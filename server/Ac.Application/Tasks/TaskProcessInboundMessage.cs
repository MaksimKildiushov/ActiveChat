using Ac.Application.Contracts.Interfaces;
using Ac.Application.Contracts.Models;
using Ac.Application.Models;
using Ac.Application.Services;
using Ac.Data;
using Ac.Data.Tenant;
using Ac.Domain.Entities;
using Ac.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ac.Application.Tasks;

/// <summary>
/// Hangfire-задача: выполняет шаги 6–9 пайплайна входящих сообщений после записи в БД и создания UserMessageEvent.
/// Вызывается из <see cref="Events.UserMessageEvent"/> по payload (TenantId, ConversationId, MessageId, UserId).
/// </summary>
public class TaskProcessInboundMessage(
    ApiDb apiDb,
    TenantDb tenantDb,
    CurrentTenantContext tenantContext,
    InboundParserRegistry parserRegistry,
    IAiDecisionService aiDecisionService,
    StepDispatcher stepDispatcher,
    IntentDispatcher intentDispatcher,
    ConversationService conversationService,
    ILogger<TaskProcessInboundMessage> logger)
{
    /// <summary>
    /// Выполняет шаги: 6) AI decision, 7) step handler → ReplyIntent, 8) deliver, 9) SaveInteraction.
    /// </summary>
    public async Task Execute(int tenantId, int conversationId, int messageId, int userId)
    {
        await ExecuteCore(tenantId, conversationId, messageId, userId, CancellationToken.None);
    }

    private async Task ExecuteCore(int tenantId, int conversationId, int messageId, int userId, CancellationToken ct)
    {
        logger.LogDebug(
            "TaskProcessInboundMessageAfterSave: TenantId={TenantId}, ConversationId={ConversationId}, MessageId={MessageId}, UserId={UserId}",
            tenantId, conversationId, messageId, userId);

        // Tenant + schema для контекста
        var tenant = await apiDb.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
        {
            logger.LogWarning("Tenant {TenantId} not found, skipping job", tenantId);
            return;
        }

        tenantContext.Set(tenantId, tenant.SchemaName);

        // Conversation с Channel и Client, Message
        var conversation = await tenantDb.Conversations
            .Include(c => c.Channel)
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
        {
            logger.LogWarning("Conversation {ConversationId} not found in tenant schema, skipping job", conversationId);
            return;
        }

        var message = await tenantDb.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, ct);

        if (message is null)
        {
            logger.LogWarning("Message {MessageId} not found in conversation {ConversationId}, skipping job", messageId, conversationId);
            return;
        }

        var channel = conversation.Channel;
        if (channel is null)
        {
            logger.LogWarning("Channel not loaded for conversation {ConversationId}, skipping job", conversationId);
            return;
        }

        var channelCtx = new ChannelContext(
            channel.Id,
            channel.TenantId,
            tenant.SchemaName,
            channel.ChannelType,
            channel.SettingsJson);

        // Восстанавливаем UnifiedInboundMessage из сохранённого сообщения
        var inbound = RestoreInboundMessage(conversation, message, channel.ChannelType);
        if (inbound is null)
        {
            logger.LogWarning("Could not restore UnifiedInboundMessage for MessageId={MessageId}, skipping job", messageId);
            return;
        }

        // 6. AI decision
        var decision = await aiDecisionService.DecideAsync(
            new DecisionContext(conversation, inbound, channelCtx), ct);

        logger.LogDebug("Decision: {StepKind} (confidence={Confidence:F2})",
            decision.StepKind, decision.Confidence);

        // 7. Step handler -> ReplyIntent
        var intent = await stepDispatcher.DispatchAsync(
            new StepContext(conversation, inbound, decision, channelCtx), ct);

        // 8. Deliver via channel adapter
        await intentDispatcher.DeliverAsync(
            new OutboundMessage(inbound.ChatId ?? string.Empty, intent, channelCtx), ct);

        // 9. Persist message log + audit
        await conversationService.SaveInteractionAsync(conversation, inbound, decision, ct);

        logger.LogInformation(
            "TaskProcessInboundMessageAfterSave completed for ConversationId={ConversationId}, Step={Step}",
            conversationId, decision.StepKind);
    }

    private UnifiedInboundMessage? RestoreInboundMessage(ConversationEntity conversation, MessageEntity message, Ac.Domain.Enums.ChannelType channelType)
    {
        if (!string.IsNullOrEmpty(message.RawJson))
        {
            try
            {
                return parserRegistry.GetParser(channelType).Parse(message.RawJson);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse RawJson for MessageId={MessageId}, using fallback", message.Id);
            }
        }

        var client = conversation.Client;
        var externalUserId = client?.ChannelUserId ?? string.Empty;
        return new UnifiedInboundMessage(
            ExternalUserId: externalUserId,
            ChatId: conversation.ChatId,
            Text: message.Content ?? string.Empty,
            Attachments: [],
            Timestamp: default,
            RawJson: message.RawJson ?? "{}");
    }
}

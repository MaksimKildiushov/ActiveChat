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
public class ProcessInboundMessageTask(
    ApiDb apiDb,
    TenantDb tenantDb,
    CurrentTenantContext tenantContext,
    InboundParserRegistry parserRegistry,
    IAiDecisionService aiDecisionService,
    StepDispatcher stepDispatcher,
    IntentDispatcher intentDispatcher,
    ConversationService conversationService,
    ILogger<ProcessInboundMessageTask> logger)
    : HangfireTaskBase<ProcessInboundMessageTask.Args, ProcessInboundMessageTask.Context>
{
    /// <summary>
    /// Выполняет шаги: 6) AI decision, 7) step handler → ReplyIntent, 8) deliver, 9) SaveInteraction.
    /// </summary>
    public async Task Execute(int tenantId, int conversationId, int messageId, int userId)
    {
        logger.LogDebug(
            "TaskProcessInboundMessage: TenantId={TenantId}, ConversationId={ConversationId}, MessageId={MessageId}, UserId={UserId}",
            tenantId, conversationId, messageId, userId);
        await RunAsync(new Args(tenantId, conversationId, messageId, userId), CancellationToken.None);
    }

    protected override async Task ExecuteAsync(Context ctx, CancellationToken ct)
    {
        var decision = await aiDecisionService.DecideAsync(
            new DecisionContext(ctx.Conversation, ctx.Inbound, ctx.ChannelCtx), ct);

        logger.LogDebug("Decision: {StepKind} (confidence={Confidence:F2})",
            decision.StepKind, decision.Confidence);

        var intent = await stepDispatcher.DispatchAsync(
            new StepContext(ctx.Conversation, ctx.Inbound, decision, ctx.ChannelCtx), ct);

        await intentDispatcher.DeliverAsync(
            new OutboundMessage(ctx.Inbound.ChatId ?? string.Empty, intent, ctx.ChannelCtx), ct);

        await conversationService.SaveInteractionAsync(ctx.Conversation, ctx.Inbound, decision, intent, ct);

        logger.LogInformation(
            "TaskProcessInboundMessage completed for ConversationId={ConversationId}, Step={Step}",
            ctx.Conversation.Id, decision.StepKind);
    }

    protected override async Task<Context> ValidateAsync(Args args, CancellationToken ct)
    {
        var tenant = EnsureNotNull(
            await apiDb.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == args.TenantId, ct),
            $"Tenant {args.TenantId} not found.");

        tenantContext.Set(args.TenantId, tenant.SchemaName);

        var conversation = EnsureNotNull(
            await tenantDb.Conversations
                .Include(c => c.Channel)
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == args.ConversationId, ct),
            $"Conversation {args.ConversationId} not found in tenant schema (TenantId={args.TenantId}).");

        var message = EnsureNotNull(
            await tenantDb.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == args.MessageId && m.ConversationId == args.ConversationId, ct),
            $"Message {args.MessageId} not found in conversation {args.ConversationId} (TenantId={args.TenantId}).");

        var channel = EnsureNotNull(
            conversation.Channel,
            $"Channel not loaded for conversation {args.ConversationId} (TenantId={args.TenantId}).");

        var channelCtx = new ChannelContext(
            channel.Id,
            channel.TenantId,
            tenant.SchemaName,
            channel.ChannelType,
            channel.SettingsJson);

        var inbound = EnsureNotNull(
            RestoreInboundMessage(conversation, message, channel.ChannelType, args.TenantId),
            $"Could not restore UnifiedInboundMessage for MessageId={args.MessageId} (ConversationId={args.ConversationId}, TenantId={args.TenantId}).");

        return new Context(conversation, message, channelCtx, inbound);
    }

    private T EnsureNotNull<T>(T? value, string message) where T : class
    {
        if (value is null)
        {
            logger.LogError("TaskProcessInboundMessage failed: {Message}", message);
            throw new InvalidOperationException(message);
        }
        return value;
    }

    private UnifiedInboundMessage? RestoreInboundMessage(ConversationEntity conversation, MessageEntity message, Domain.Enums.ChannelType channelType, int tenantId)
    {
        if (!string.IsNullOrEmpty(message.RawJson))
        {
            try
            {
                return parserRegistry.GetParser(channelType).Parse(message.RawJson);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to parse RawJson for MessageId={message.Id} (ConversationId={conversation.Id}, TenantId={tenantId}).";
                logger.LogError(ex, "TaskProcessInboundMessage failed: {Message}", msg);
                throw new InvalidOperationException(msg, ex);
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

    public record Args(int TenantId, int ConversationId, int MessageId, int UserId);

    public record Context(ConversationEntity Conversation, MessageEntity Message, ChannelContext ChannelCtx, UnifiedInboundMessage Inbound);
}

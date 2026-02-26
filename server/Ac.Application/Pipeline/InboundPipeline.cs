using Ac.Application.Events;
using Ac.Application.Contracts.Models;
using Ac.Application.Services;
using Ac.Data;
using Ac.Data.Repositories;
using Ac.Data.Tenant;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ac.Application.Pipeline;

public class InboundPipeline(
    ApiDb apiDb,
    ChannelTokenResolver tokenResolver,
    CurrentTenantContext tenantContext,
    InboundParserRegistry parserRegistry,
    ClientService clientService,
    ConversationService conversationService,
    ClientRepository clientRepository,
    ConversationRepository conversationRepository,
    ILogger<InboundPipeline> logger)
{
    public async Task ProcessAsync(ChannelToken channelToken, string rawJson, CancellationToken ct = default)
    {
        // 1. Resolve token -> ChannelContext
        var channelCtx = await tokenResolver.ResolveAsync(channelToken, ct);
        if (channelCtx is null)
        {
            logger.LogWarning("Unknown or inactive channel token: {Token}", channelToken);
            return;
        }

        tenantContext.Set(channelCtx.TenantId, channelCtx.SchemaName);

        logger.LogDebug("Channel resolved: {ChannelId} [{Type}] tenant={TenantId} schema={Schema}",
            channelCtx.ChannelId, channelCtx.ChannelType, channelCtx.TenantId, channelCtx.SchemaName);

        // 2. Parse raw body -> InboundParseResult
        var parseResult = parserRegistry.GetParser(channelCtx.ChannelType).Parse(rawJson);

        // 3a. Служебный кейс: чат закрыт каналом — закрываем диалог и выходим.
        if (parseResult.Status == InboundParseStatus.ChatClosed)
        {
            var closed = parseResult.Message;
            if (closed is null || string.IsNullOrWhiteSpace(closed.ExternalUserId))
                return;

            var closedClient = await clientRepository.FindByChannelUserIdAsync(closed.ExternalUserId, ct);
            if (closedClient is null)
                return;

            var closedConversation = await conversationRepository.FindAsync(channelCtx.ChannelId, closedClient.Id, ct);
            if (closedConversation is null)
                return;

            closedConversation.Status = ChatStatus.Closed;
            await conversationRepository.SaveChangesAsync(ct);
            return;
        }

        // 3b. Обычный кейс: входящее сообщение пользователя.
        var inbound = parseResult.Message
                     ?? throw new InvalidOperationException("Parser returned Message status with null UnifiedInboundMessage.");

        // 4. Find or create Client (priority: OverrideUserId → ChannelUserId → Email → Phone)
        var client = await clientService.GetOrCreateAsync(channelCtx, inbound.ExternalUserId, overrideUserId: null, email: null, phone: null, ct: ct);

        // 4. Find or create Conversation по клиенту (ChatId — куда отправлять ответы); при передаче inbound сохраняется Message в БД
        var (conversation, createdMessage) = await conversationService.GetOrCreateAsync(channelCtx, client, inbound.ChatId, inbound, ct);
        if (createdMessage is null)
        {
            logger.LogWarning("GetOrCreateAsync did not return message for conversation");
            return;
        }

        // 5. Добавление евента о новом сообщении. Евент будет обработан асинхронной джобой (HangFire),
        UserMessageEvent.Create(apiDb, channelCtx.TenantId, conversation.Id, createdMessage.Id, client.Id);

        await apiDb.SaveChangesAsync(ct);
        logger.LogDebug("Inbound pipeline saved (steps 1–5) for token={Token} user={UserId}; steps 6–9 will run in background.",
            channelToken, inbound.ExternalUserId);
    }
}

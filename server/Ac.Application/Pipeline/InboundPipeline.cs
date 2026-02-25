using Ac.Application.Events;
using Ac.Application.Services;
using Ac.Data;
using Ac.Data.Repositories;
using Ac.Data.Tenant;
using Ac.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ac.Application.Pipeline;

public class InboundPipeline(
    ApiDb db,
    ChannelTokenResolver tokenResolver,
    CurrentTenantContext tenantContext,
    InboundParserRegistry parserRegistry,
    ClientService clientService,
    ConversationService conversationService,
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

        // 2. Parse raw body -> UnifiedInboundMessage
        var inbound = parserRegistry.GetParser(channelCtx.ChannelType).Parse(rawJson);

        // 3. Find or create Client (priority: OverrideUserId → ChannelUserId → Email → Phone)
        var client = await clientService.GetOrCreateAsync(channelCtx, inbound.ExternalUserId, overrideUserId: null, email: null, phone: null, ct: ct);

        // 4. Find or create Conversation по клиенту (ChatId — куда отправлять ответы); при передаче inbound сохраняется Message в БД
        var (conversation, createdMessage) = await conversationService.GetOrCreateAsync(channelCtx, client, inbound.ChatId, inbound, ct);
        if (createdMessage is null)
        {
            logger.LogWarning("GetOrCreateAsync did not return message for conversation");
            return;
        }

        // 5. Добавление евента о новом сообщении. Евент будет обработан асинхронной джобой (HangFire),
        //    которая выполнит шаги 6–9: AI decision → step handler → deliver → SaveInteraction.
        UserMessageEvent.Create(db, channelCtx.TenantId, conversation.Id, createdMessage.Id, client.Id);
        db.SaveChanges();

        logger.LogInformation("Inbound pipeline saved (steps 1–5) for token={Token} user={UserId}; steps 6–9 will run in background.",
            channelToken, inbound.ExternalUserId);
    }
}

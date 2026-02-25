using Ac.Application.Contracts.Interfaces;
using Ac.Application.Contracts.Models;
using Ac.Application.Events;
using Ac.Application.Models;
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
    IAiDecisionService aiDecisionService,
    StepDispatcher stepDispatcher,
    IntentDispatcher intentDispatcher,
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

        // 5. Добавление евента о новом сообщении. Евент будет обработан асинхронной джобой (HangFire).
        if (createdMessage is not null)
        {
            UserMessageEvent.Create(db, conversation.Id, createdMessage.Id, Guid.Empty);
            db.SaveChanges();
        }

        // 6. AI decision
        var decision = await aiDecisionService.DecideAsync(
            new DecisionContext(conversation, inbound, channelCtx), ct);

        logger.LogDebug("Decision: {StepKind} (confidence={Confidence:F2})",
            decision.StepKind, decision.Confidence);

        // 7. Step handler -> ReplyIntent
        var intent = await stepDispatcher.DispatchAsync(
            new StepContext(conversation, inbound, decision, channelCtx), ct);

        // 8. Deliver via channel adapter (адрес доставки — ChatId, иначе ExternalUserId)
        await intentDispatcher.DeliverAsync(
            new OutboundMessage(inbound.ChatId, intent, channelCtx), ct);

        // 9. Persist message log + audit (обновляем LastMessage, MessagesCount, Status в диалоге)
        await conversationService.SaveInteractionAsync(conversation, inbound, decision, ct);

        logger.LogInformation("Pipeline completed for token={Token} user={UserId} step={Step}",
            channelToken, inbound.ExternalUserId, decision.StepKind);
    }
}

using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Application.Services;
using Ac.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ac.Application.Pipeline;

public class InboundPipeline(
    IChannelTokenResolver tokenResolver,
    ICurrentTenantContext tenantContext,
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

        // 4. Find or create Conversation по клиенту (ChatId — куда отправлять ответы)
        var chatId = inbound.ChatId ?? inbound.ExternalUserId;
        var conversation = await conversationService.GetOrCreateAsync(channelCtx, client, chatId, ct);

        // 5. AI decision
        var decision = await aiDecisionService.DecideAsync(
            new DecisionContext(conversation, inbound, channelCtx), ct);

        logger.LogDebug("Decision: {StepKind} (confidence={Confidence:F2})",
            decision.StepKind, decision.Confidence);

        // 6. Step handler -> ReplyIntent
        var intent = await stepDispatcher.DispatchAsync(
            new StepContext(conversation, inbound, decision, channelCtx), ct);

        // 7. Deliver via channel adapter (адрес доставки — ChatId, иначе ExternalUserId)
        await intentDispatcher.DeliverAsync(
            new OutboundMessage(chatId, intent, channelCtx), ct);

        // 8. Persist message log + audit
        await conversationService.SaveInteractionAsync(conversation.Id, inbound, decision, ct);

        logger.LogInformation("Pipeline completed for token={Token} user={UserId} step={Step}",
            channelToken, inbound.ExternalUserId, decision.StepKind);
    }
}

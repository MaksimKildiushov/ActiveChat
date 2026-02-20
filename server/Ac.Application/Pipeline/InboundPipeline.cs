using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Application.Services;
using Microsoft.Extensions.Logging;

namespace Ac.Application.Pipeline;

public class InboundPipeline(
    IChannelTokenResolver tokenResolver,
    InboundParserRegistry parserRegistry,
    ConversationService conversationService,
    IAiDecisionService aiDecisionService,
    StepDispatcher stepDispatcher,
    IntentDispatcher intentDispatcher,
    ILogger<InboundPipeline> logger)
{
    public async Task ProcessAsync(string channelToken, string rawJson, CancellationToken ct = default)
    {
        // 1. Resolve token -> ChannelContext
        var channelCtx = await tokenResolver.ResolveAsync(channelToken, ct);
        if (channelCtx is null)
        {
            logger.LogWarning("Unknown or inactive channel token: {Token}", channelToken);
            return;
        }

        logger.LogDebug("Channel resolved: {ChannelId} [{Type}] tenant={TenantId}",
            channelCtx.ChannelId, channelCtx.ChannelType, channelCtx.TenantId);

        // 2. Parse raw body -> UnifiedInboundMessage
        var inbound = parserRegistry.GetParser(channelCtx.ChannelType).Parse(rawJson);

        // 3. Find or create Conversation
        var conversation = await conversationService.GetOrCreateAsync(channelCtx, inbound.ExternalUserId, ct);

        // 4. AI decision
        var decision = await aiDecisionService.DecideAsync(
            new DecisionContext(conversation, inbound, channelCtx), ct);

        logger.LogDebug("Decision: {StepKind} (confidence={Confidence:F2})",
            decision.StepKind, decision.Confidence);

        // 5. Step handler -> ReplyIntent
        var intent = await stepDispatcher.DispatchAsync(
            new StepContext(conversation, inbound, decision, channelCtx), ct);

        // 6. Deliver via channel adapter
        await intentDispatcher.DeliverAsync(
            new OutboundMessage(inbound.ExternalUserId, intent, channelCtx), ct);

        // 7. Persist message log + audit
        await conversationService.SaveInteractionAsync(conversation.Id, inbound, decision, ct);

        logger.LogInformation("Pipeline completed for token={Token} user={UserId} step={Step}",
            channelToken, inbound.ExternalUserId, decision.StepKind);
    }
}

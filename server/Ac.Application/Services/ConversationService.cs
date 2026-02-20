using System.Text.Json;
using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Domain.Entities;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Services;

public class ConversationService(
    IConversationRepository conversations,
    IMessageRepository messages,
    IDecisionAuditRepository audits,
    IUnitOfWork unitOfWork)
{
    public async Task<ConversationEntity> GetOrCreateAsync(
        ChannelContext channelCtx,
        string externalUserId,
        CancellationToken ct = default)
    {
        var existing = await conversations.FindAsync(
            channelCtx.TenantId, channelCtx.ChannelId, externalUserId, ct);

        if (existing is not null)
            return existing;

        ConversationEntity conversation = new()
        {
            Id = Guid.NewGuid(),
            TenantId = channelCtx.TenantId,
            ChannelId = channelCtx.ChannelId,
            ExternalUserId = externalUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await conversations.CreateAsync(conversation, ct);
    }

    public async Task SaveInteractionAsync(
        Guid conversationId,
        UnifiedInboundMessage inbound,
        DecisionResult decision,
        CancellationToken ct = default)
    {
        await messages.AddAsync(new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Direction = MessageDirection.Inbound,
            Text = inbound.Text,
            RawJson = inbound.RawJson,
            CreatedAt = inbound.Timestamp
        }, ct);

        await audits.AddAsync(new DecisionAuditEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            StepKind = decision.StepKind,
            Confidence = decision.Confidence,
            SlotsJson = decision.Slots.Count > 0
                ? JsonSerializer.Serialize(decision.Slots)
                : null,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }
}

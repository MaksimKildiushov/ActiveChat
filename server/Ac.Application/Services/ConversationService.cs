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
        ClientEntity client,
        string? chatId,
        CancellationToken ct = default)
    {
        var existing = await conversations.FindAsync(
            channelCtx.ChannelId, client.Id, ct);

        if (existing is not null)
        {
            if (!string.IsNullOrEmpty(chatId) && existing.ChatId != chatId)
            {
                existing.ChatId = chatId;
                await unitOfWork.SaveChangesAsync(ct);
            }
            return existing;
        }

        ConversationEntity conversation = new()
        {
            ChannelId = channelCtx.ChannelId,
            ClientId = client.Id,
            ChatId = chatId,
        };

        return await conversations.CreateAsync(conversation, ct);
    }

    public async Task SaveInteractionAsync(
        int conversationId,
        UnifiedInboundMessage inbound,
        DecisionResult decision,
        CancellationToken ct = default)
    {
        await messages.AddAsync(new MessageEntity
        {
            ConversationId = conversationId,
            Direction = MessageDirection.Inbound,
            Text = inbound.Text,
            RawJson = inbound.RawJson,
            CreatedAt = inbound.Timestamp
        }, ct);

        await audits.AddAsync(new DecisionAuditEntity
        {
            ConversationId = conversationId,
            StepKind = decision.StepKind,
            Confidence = decision.Confidence,
            SlotsJson = decision.Slots.Count > 0
                ? JsonSerializer.Serialize(decision.Slots)
                : null,
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }
}

using Ac.Application.Contracts.Models;
using Ac.Data.Repositories;
using Ac.Domain.Entities;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;
using System.Text.Json;

namespace Ac.Application.Services;

public class ConversationService(
    ConversationRepository conversations,
    MessageRepository messages,
    DecisionAuditRepository audits,
    UnitOfWork unitOfWork)
{
    /// <summary>Возвращает диалог и созданное входящее сообщение (если inbound передан).</summary>
    public async Task<(ConversationEntity conversation, MessageEntity? createdMessage)> GetOrCreateAsync(
        ChannelContext channelCtx,
        ClientEntity client,
        string? chatId,
        UnifiedInboundMessage inbound,
        CancellationToken ct = default)
    {
        var existing = await conversations.FindAsync(
            channelCtx.ChannelId, client.Id, ct);

        if (existing is not null)
        {
            var hasChanges = false;
            if (!string.IsNullOrEmpty(chatId) && existing.ChatId != chatId)
            {
                existing.ChatId = chatId;
                hasChanges = true;
            }

            MessageEntity? addedMessage = null;
            existing.LastMessage = inbound.Text;
            existing.MessagesCount += 1;
            existing.Status = ChatStatus.Active;
            var message = new MessageEntity
            {
                ConversationId = existing.Id,
                Direction = MessageDirection.Inbound,
                Content = inbound.Text,
                RawJson = inbound.RawJson
            };
            await messages.AddAsync(message, ct);
            addedMessage = message;
            hasChanges = true;

            if (hasChanges)
                await unitOfWork.SaveChangesAsync(ct);
            return (existing, addedMessage);
        }

        ConversationEntity conversation = new()
        {
            ChannelId = channelCtx.ChannelId,
            ClientId = client.Id,
            ChatId = chatId,
        };

        conversation = await conversations.CreateAsync(conversation, ct);

        conversation.LastMessage = inbound.Text;
        conversation.MessagesCount = 1;
        conversation.Status = ChatStatus.Active;
        var newMessage = new MessageEntity
        {
            ConversationId = conversation.Id,
            Direction = MessageDirection.Inbound,
            Content = inbound.Text,
            RawJson = inbound.RawJson
        };
        await messages.AddAsync(newMessage, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (conversation, newMessage);
    }

    public async Task SaveInteractionAsync(
        ConversationEntity conversation,
        UnifiedInboundMessage inbound,
        DecisionResult decision,
        CancellationToken ct = default)
    {
        conversation.LastMessage = inbound.Text;
        conversation.MessagesCount += 1;
        conversation.Status = ChatStatus.Active;

        await audits.AddAsync(new DecisionAuditEntity
        {
            ConversationId = conversation.Id,
            StepKind = decision.StepKind,
            Confidence = decision.Confidence,
            SlotsJson = decision.Slots.Count > 0
                ? JsonSerializer.Serialize(decision.Slots)
                : null,
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }
}

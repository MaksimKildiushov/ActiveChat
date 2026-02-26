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
    public async Task<(ConversationEntity conversation, MessageEntity createdMessage)> GetOrCreateAsync(
        ChannelContext channelCtx,
        ClientEntity client,
        string? chatId,
        UnifiedInboundMessage inbound,
        CancellationToken ct = default)
    {
        var existing = await conversations.FindAsync(channelCtx.ChannelId, client.Id, ct);

        if (existing is not null)
        {
            var hasChanges = false;
            if (!string.IsNullOrEmpty(chatId) && existing.ChatId != chatId)
            {
                existing.ChatId = chatId;
                hasChanges = true;
            }
            ApplyInboundToConversation(existing, inbound, isNew: false);
            var addedMessage = await AddInboundMessageAsync(existing, inbound, ct);
            hasChanges = true;

            if (hasChanges)
                await unitOfWork.SaveChangesAsync(ct);
            return (existing, addedMessage);
        }

        var conversation = new ConversationEntity
        {
            ChannelId = channelCtx.ChannelId,
            ClientId = client.Id,
            ChatId = chatId,
        };
        conversation = await conversations.CreateAsync(conversation, ct);

        ApplyInboundToConversation(conversation, inbound, isNew: true);
        var newMessage = await AddInboundMessageAsync(conversation, inbound, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (conversation, newMessage);

        void ApplyInboundToConversation(ConversationEntity c, UnifiedInboundMessage inb, bool isNew)
        {
            c.LastMessage = inb.Text;
            c.Status = ChatStatus.Active;
            c.MessagesCount = isNew ? 1 : c.MessagesCount + 1;
        }

        async Task<MessageEntity> AddInboundMessageAsync(ConversationEntity conv, UnifiedInboundMessage inb, CancellationToken token)
        {
            var msg = new MessageEntity
            {
                ConversationId = conv.Id,
                Direction = MessageDirection.Inbound,
                Content = inb.Text,
                RawJson = inb.RawJson
            };
            await messages.AddAsync(msg, token);
            return msg;
        }
    }

    public async Task SaveInteractionAsync(
        ConversationEntity conversation,
        DecisionResult decision,
        ReplyIntent replyIntent,
        CancellationToken ct = default)
    {

        var replyText = GetReplyText(replyIntent);

        conversation.LastMessage = replyText;
        conversation.MessagesCount += 1;
        conversation.Status = ChatStatus.Active;

        if (!string.IsNullOrEmpty(replyText))
        {
            await messages.AddAsync(new MessageEntity
            {
                ConversationId = conversation.Id,
                Direction = MessageDirection.Outbound,
                Content = replyText,
                RawJson = null
            }, ct);
        }

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

        static string GetReplyText(ReplyIntent intent) =>
            intent switch
            {
                TextIntent t => t.Text,
                ButtonsIntent b => b.Text,
                HandoffIntent h => h.Message ?? string.Empty,
                _ => string.Empty
            };
    }
}

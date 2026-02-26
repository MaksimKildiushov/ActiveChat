using Hangfire;
using Ac.Application.Tasks;
using Ac.Data;
using Ac.Domain.Enums;
using Ac.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Ac.Application.Events.UserMessageEvent;

namespace Ac.Application.Events;

/// <summary>
/// Обработчик события UserMessage - отправка уведомления пользователю о новом сообщении
/// </summary>
public class UserMessageEvent(
    ApiDb db,
    EventEntity eventEntity,
    ILogger<object> logger) : MessageEventClass<UserMessagePayload>(db, eventEntity, logger)
{
    /// <summary>
    /// Создает и добавляет событие UserMessage в контекст БД
    /// </summary>
    public static EventEntity Create(
        ApiDb db,
        int tenantId,
        int chatId,
        int messageId,
        int userId)
    {
        var payload = new UserMessagePayload
        {
            TenantId = tenantId,
            ConversationId = chatId,
            MessageId = messageId,
            UserId = userId
        };

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var eventEntity = new EventEntity
        {
            EventType = EventType.UserMessage,
            Payload = payloadJson
        };

        db.Events.Add(eventEntity);
        return eventEntity;
    }

    protected override string EventName => "UserMessage";

    protected override bool ValidatePayload(UserMessagePayload payload)=>
        payload.TenantId != 0 && payload.ConversationId != 0 && payload.MessageId != 0 && payload.UserId != 0;

    protected override void EnqueueTask(UserMessagePayload payload)
    {
        BackgroundJob.Enqueue<ProcessInboundMessageTask>(x => x.Execute(
            payload.TenantId,
            payload.ConversationId,
            payload.MessageId,
            payload.UserId));
    }

    protected override void LogPayloadInfo(UserMessagePayload payload)
    {
        Logger.LogDebug(
            "Processing UserMessage: ChatId={ChatId}, MessageId={MessageId}, UserId={UserId}",
            payload.ConversationId, payload.MessageId, payload.UserId);
    }

    protected override void LogInvalidPayloadWarning(UserMessagePayload payload)
    {
        Logger.LogWarning(
            "UserMessage event {EventId} has invalid payload: ChatId={ChatId}, MessageId={MessageId}, UserId={UserId}",
            DbModel.Id, payload.ConversationId, payload.MessageId, payload.UserId);
    }

    /// <summary>
    /// Модель payload для события UserMessage
    /// </summary>
    public class UserMessagePayload
    {
        public int TenantId { get; set; }

        public int ConversationId { get; set; }

        public int MessageId { get; set; }

        public int UserId { get; set; }
    }
}

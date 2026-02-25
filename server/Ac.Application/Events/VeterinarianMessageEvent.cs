using Hangfire;
using Ac.Abstractions.Helpers;
using Ac.Application.Tasks;
using Ac.Data;
using Ac.Domain.Enums;
using Ac.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Ac.Application.Events.OperatorMessageEvent;

namespace Ac.Application.Events;

/// <summary>
/// Обработчик события OperatorMessage - отправка уведомления ветеринару о новом сообщении
/// </summary>
public class OperatorMessageEvent(
    ApiDb db,
    EventEntity eventEntity,
    ILogger<object> logger) : MessageEventClass<OperatorMessagePayload>(db, eventEntity, logger)
{
    /// <summary>
    /// Создает и добавляет событие OperatorMessage в контекст БД
    /// </summary>
    public static EventEntity Create(
        ApiDb db,
        int chatId,
        int messageId,
        Guid OperatorId,
        Guid? authorId = null)
    {
        var payload = new OperatorMessagePayload
        {
            ChatId = chatId,
            MessageId = messageId,
            OperatorId = OperatorId
        };

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var eventEntity = new EventEntity
        {
            EventType = EventType.OperatorMessage,
            Payload = payloadJson,
            Status = EventStatus.Pending,
            RetryCount = 0,
            MaxRetries = 2,
            AuthorId = authorId ?? SysAccountsHlp.Api.Id,
            Created = DateTime.UtcNow
        };

        db.Events.Add(eventEntity);
        return eventEntity;
    }

    protected override string EventName => "OperatorMessage";

    protected override bool ValidatePayload(OperatorMessagePayload payload)
    {
        return payload.ChatId != 0 && payload.MessageId != 0 && payload.OperatorId != Guid.Empty;
    }

    protected override void EnqueueTask(OperatorMessagePayload payload)
    {
        BackgroundJob.Enqueue<TaskCloneChatMessageToTg>(x => x.Execute(
            payload.ChatId,
            payload.MessageId,
            payload.OperatorId));
    }

    protected override void LogPayloadInfo(OperatorMessagePayload payload)
    {
        Logger.LogDebug(
            "Processing OperatorMessage: ChatId={ChatId}, MessageId={MessageId}, OperatorId={OperatorId}",
            payload.ChatId, payload.MessageId, payload.OperatorId);
    }

    protected override void LogInvalidPayloadWarning(OperatorMessagePayload payload)
    {
        Logger.LogWarning(
            "OperatorMessage event {EventId} has invalid payload: ChatId={ChatId}, MessageId={MessageId}, OperatorId={OperatorId}",
            DbModel.Id, payload.ChatId, payload.MessageId, payload.OperatorId);
    }

    /// <summary>
    /// Модель payload для события OperatorMessage
    /// </summary>
    public class OperatorMessagePayload
    {
        public int ChatId { get; set; }

        public int MessageId { get; set; }

        public Guid OperatorId { get; set; }
    }
}

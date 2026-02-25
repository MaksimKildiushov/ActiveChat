using Ac.Data;
using Ac.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Ac.Application.Events;

/// <summary>
/// Абстрактный базовый класс для обработки событий сообщений
/// Содержит общую логику парсинга payload и запуска задач
/// </summary>
public abstract class MessageEventClass<TPayload> : EventClass
    where TPayload : class
{
    protected MessageEventClass(
        ApiDb db,
        EventEntity eventEntity,
        ILogger<object> logger) : base(db, eventEntity, logger)
    {
    }

    /// <summary>
    /// Название события для логирования
    /// </summary>
    protected abstract string EventName { get; }

    /// <summary>
    /// Валидация payload после десериализации
    /// </summary>
    protected abstract bool ValidatePayload(TPayload payload);

    /// <summary>
    /// Запуск Hangfire задачи для отправки уведомления
    /// </summary>
    protected abstract void EnqueueTask(TPayload payload);

    /// <summary>
    /// Логирование информации о payload
    /// </summary>
    protected abstract void LogPayloadInfo(TPayload payload);

    /// <summary>
    /// Логирование предупреждения о невалидном payload
    /// </summary>
    protected abstract void LogInvalidPayloadWarning(TPayload payload);

    public override Task ProcessEventCore()
    {
        try
        {
            // Парсим Payload (JSON) с теми же опциями, что использовались при сериализации
            var payload = JsonSerializer.Deserialize<TPayload>(
                DbModel.Payload,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            if (payload == null)
            {
                Logger.LogError("Failed to parse {EventName} payload for event {EventId}", EventName, DbModel.Id);
                throw new InvalidOperationException($"Invalid {EventName} payload");
            }

            // Проверяем наличие обязательных полей
            if (!ValidatePayload(payload))
            {
                LogInvalidPayloadWarning(payload);
                return Task.CompletedTask; // Пропускаем событие, если данные некорректны
            }

            LogPayloadInfo(payload);

            // Запускаем задачу отправки уведомления через Hangfire
            EnqueueTask(payload);
            
            return Task.CompletedTask;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to deserialize {EventName} payload for event {EventId}", EventName, DbModel.Id);
            throw;
        }
    }
}

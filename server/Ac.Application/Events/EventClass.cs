using Ac.Abstractions.Helpers;
using Ac.Data;
using Ac.Domain.Enums;
using Ac.Domain.Entities;
using Microsoft.Extensions.Logging;
namespace Ac.Application.Events;

/// <summary>
/// Абстрактный базовый класс для обработки событий
/// Содержит всю логику работы с БД (блокировки, статусы, retry)
/// </summary>
public class EventClass(ApiDb db, EventEntity dbModel, ILogger<object> logger)
{
    private readonly ApiDb _db = db;
    protected readonly EventEntity DbModel = dbModel;
    protected readonly ILogger<object> Logger = logger;

    /// <summary>
    /// Маппинг типов событий на классы обработчиков
    /// </summary>
    private static readonly Dictionary<EventType, Type> EventTypeMap = new()
    {
        { EventType.OperatorMessage, typeof(OperatorMessageEvent) },
        { EventType.UserMessage, typeof(UserMessageEvent) },
    };

    public async Task ProcessAsync()
    {
        try
        {
            // Шаг 1: Блокируем событие для обработки
            await Lock();

            // Шаг 2: Распаковываем обработчик события
            var eventHandler = Unpack();

            // Шаг 3: Вызываем специфичную обработку события
            await eventHandler.ProcessEventCore();

            // Шаг 4: Завершаем обработку события
            await Finish();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing event {EventId}", DbModel.Id);

            // Обновляем информацию об ошибке
            DbModel.RetryCount++;
            DbModel.ErrorMessage = ex.Message;
            DbModel.ErrorStackTrace = ex.StackTrace;

            if (DbModel.RetryCount >= DbModel.MaxRetries)
            {
                DbModel.Status = EventStatus.Failed;
                Logger.LogError("Event {EventId} failed after {RetryCount} retries", DbModel.Id, DbModel.RetryCount);
            }
            else
            {
                // Экспоненциальная задержка перед повтором
                DbModel.Status = EventStatus.Pending;
                DbModel.NextRetryAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, DbModel.RetryCount) * 60);
                DbModel.ProcessingId = null;
                DbModel.ProcessingStartedAt = null;
            }

            await SaveChangesAsync();
        }
    }

    public virtual async Task ProcessEventCore()
    {
        throw new Exception("ProcessEventCore must be implemented in derived classes (error 1245301125).");
    }

    /// <summary>
    /// Обрабатывает событие по его ID (вызывается Hangfire)
    /// </summary>
    private async Task Lock()
    {
        Logger.LogInformation("Locking event {EventId}", this.DbModel.Id);

        // Проверка на дубликат - если уже обработано, выходим
        if (DbModel.Status == EventStatus.Completed)
        {
            Logger.LogInformation("Event {EventId} already completed", DbModel.Id);
            return;
        }

        // Если уже обрабатывается другим job, проверяем таймаут (5 минут)
        if (DbModel.Status == EventStatus.Processing)
        {
            if (DbModel.ProcessingStartedAt.HasValue &&
                DateTime.UtcNow - DbModel.ProcessingStartedAt.Value > TimeSpan.FromMinutes(5))
            {
                Logger.LogWarning("Event {EventId} stuck in Processing, resetting", DbModel.Id);
                DbModel.Status = EventStatus.Pending;
                DbModel.ProcessingStartedAt = null;
                DbModel.ProcessingId = null;
            }
            else
            {
                Logger.LogInformation("Event {EventId} already processing", DbModel.Id);
                return;
            }
        }

        // Атомарно меняем статус на Processing
        var processingId = Guid.NewGuid().ToString();
        DbModel.Status = EventStatus.Processing;
        DbModel.ProcessingStartedAt = DateTime.UtcNow;
        DbModel.ProcessingId = processingId;
        await SaveChangesAsync();
    }

    private EventClass Unpack()
    {
        // Получаем тип класса обработчика для данного типа события
        if (!EventTypeMap.TryGetValue(DbModel.EventType, out var eventClassType))
        {
            Logger.LogWarning("Unknown event type: {EventType}", DbModel.EventType);
            throw new NotSupportedException($"Unknown event type: {DbModel.EventType}");
        }

        // Создаем экземпляр обработчика через Activator с использованием IServiceProvider
        EventClass eventHandler;
        try
        {
            // Создаем экземпляр через Activator
            eventHandler = (EventClass)Activator.CreateInstance(eventClassType,_db, DbModel, Logger)!;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create event handler instance for type {EventType}", eventClassType.Name);
            throw new InvalidOperationException($"Failed to create event handler for type {DbModel.EventType}", ex);
        }

        // Запускаем обработку события (вся логика БД теперь в EventClass)
        return eventHandler;
    }

    /// <summary>
    /// Специфичная обработка события (переопределяется в наследниках)
    /// </summary>
    /// <param name="eventEntity">Сущность события из БД</param>
    private async Task Finish()
    {
        // Успешное завершение
        DbModel.Status = EventStatus.Completed;
        DbModel.ProcessedAt = DateTime.UtcNow;
        DbModel.ProcessingId = null;
        DbModel.ProcessingStartedAt = null;
        await SaveChangesAsync();

        Logger.LogDebug("Event {EventId} processed successfully", DbModel.Id);
    }

    private async Task SaveChangesAsync()
    {
        DbModel.ModifierId = SysAccountsHlp.Api.Id;
        DbModel.Modified = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

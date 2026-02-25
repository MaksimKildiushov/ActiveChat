using Ac.Domain.Enums;
using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;

namespace Ac.Domain.Entities;

/// <summary>
/// Событие для асинхронной обработки через Hangfire
/// </summary>
public class EventEntity : IntEntity
{
    /// <summary>
    /// Тип события
    /// </summary>
    public EventType EventType { get; set; }

    /// <summary>
    /// JSON payload с данными события
    /// </summary>
    [Required]
    public string Payload { get; set; } = null!;

    /// <summary>
    /// Статус обработки события
    /// </summary>
    public EventStatus Status { get; set; } = EventStatus.Pending;

    /// <summary>
    /// Количество попыток обработки
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Максимальное количество попыток
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Дата и время обработки
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Дата и время следующей попытки обработки (для retry)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Сообщение об ошибке (если обработка провалилась)
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace ошибки (если обработка провалилась)
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Уникальный идентификатор текущей обработки (для защиты от дубликатов)
    /// </summary>
    [StringLength(50)]
    public string? ProcessingId { get; set; }

    /// <summary>
    /// Дата и время начала обработки
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; }
}

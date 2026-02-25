namespace Ac.Domain.Enums;

/// <summary>
/// Статус обработки события
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Ожидает обработки
    /// </summary>
    Pending = 0,

    /// <summary>
    /// В процессе обработки (job запущен)
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Успешно обработано
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Окончательно провалено (превышен MaxRetries)
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Отправлено в Dead Letter Queue
    /// </summary>
    DeadLetter = 4
}

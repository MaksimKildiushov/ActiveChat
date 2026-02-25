namespace Ac.Domain.Enums;

/// <summary>
/// Тип события для обработки через Hangfire
/// </summary>
public enum EventType
{
    /// <summary>
    /// Ветеринар отправил сообщение пользователю.
    /// </summary>
    OperatorMessage = 0,

    /// <summary>
    /// Платеж получен.
    /// </summary>
    PaymentReceived = 1,

    /// <summary>
    /// Пользователь отправил сообщение в чат.
    /// </summary>
    UserMessage = 2,
}

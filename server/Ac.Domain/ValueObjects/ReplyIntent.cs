namespace Ac.Domain.ValueObjects;

/// <summary>
/// Базовый тип намерения-ответа, который pipeline возвращает после обработки шага.
/// Каждый конкретный intent описывает, что именно нужно отправить пользователю.
/// Доставкой занимается IChannelDeliveryAdapter — он преобразует intent в формат конкретного канала.
/// </summary>
public abstract record ReplyIntent;

/// <summary>Обычный текстовый ответ.</summary>
public record TextIntent(string Text) : ReplyIntent;

/// <summary>Текст + набор кнопок быстрого ответа (Telegram InlineKeyboard, WhatsApp buttons и т.д.).</summary>
public record ButtonsIntent(string Text, IReadOnlyList<string> Buttons) : ReplyIntent;

/// <summary>Передача диалога живому оператору. Message — необязательное сообщение пользователю о переключении.</summary>
public record HandoffIntent(string? Message = null) : ReplyIntent;

/// <summary>Вызов внешнего API клиента (CRM, ERP). Adapter решает, делать HTTP-запрос или ставить задачу в очередь.</summary>
public record CallClientApiIntent(string Endpoint, IReadOnlyDictionary<string, string> Params) : ReplyIntent;

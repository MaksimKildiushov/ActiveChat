namespace Ac.Domain.ValueObjects;

/// <summary>
/// Единая модель входящего сообщения, в которую каждый IInboundParser
/// преобразует сырой payload своего канала.
/// После парсинга pipeline работает только с этой моделью — каналоспецифичные детали скрыты.
/// </summary>
public record UnifiedInboundMessage(
    /// <summary>ID пользователя в рамках канала (chat_id, userId, clientId и т.д.).</summary>
    string ExternalUserId,
    /// <summary>Текстовое содержимое. Пустая строка, если сообщение нетекстовое.</summary>
    string Text,
    /// <summary>Список URL/ID вложений (фото, документы). Пустой список, если вложений нет.</summary>
    IReadOnlyList<string> Attachments,
    /// <summary>Время отправки сообщения по данным канала.</summary>
    DateTimeOffset Timestamp,
    /// <summary>Исходный JSON для аудита и отладки. Сохраняется в Messages.RawJson.</summary>
    string RawJson);

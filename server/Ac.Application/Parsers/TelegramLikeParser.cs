using Ac.Application.Contracts.Interfaces;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;
using System.Text.Json;

namespace Ac.Application.Parsers;

/// <summary>
/// Ожидает Telegram-like JSON:
/// { "update_id": 123, "message": { "from": { "id": 456 }, "text": "Hello", "date": 1700000000 } }
/// </summary>
public class TelegramLikeParser : IInboundParser
{
    public ChannelType ChannelType => ChannelType.Telegram;

    public UnifiedInboundMessage Parse(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var message = root.GetProperty("message");
        var from = message.GetProperty("from");

        var externalUserId = from.GetProperty("id").GetRawText().Trim('"');
        var text = message.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? "" : "";
        var timestamp = message.TryGetProperty("date", out var dateProp)
            ? DateTimeOffset.FromUnixTimeSeconds(dateProp.GetInt64())
            : DateTimeOffset.UtcNow;

        return new UnifiedInboundMessage(
            ExternalUserId: externalUserId,
            Text: text,
            Attachments: [],
            Timestamp: timestamp,
            RawJson: rawJson);
    }
}

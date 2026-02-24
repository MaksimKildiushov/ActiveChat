using Ac.Application.Contracts.Interfaces;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;
using System.Text.Json;

namespace Ac.Application.Parsers;

/// <summary>
/// Ожидает generic webhook JSON:
/// { "userId": "user123", "text": "Hello", "timestamp": "2024-01-01T00:00:00Z" }
/// </summary>
public class WebhookParser : IInboundParser
{
    public ChannelType ChannelType => ChannelType.Webhook;

    public UnifiedInboundMessage Parse(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var userId = root.TryGetProperty("userId", out var userProp)
            ? userProp.GetString() ?? "unknown"
            : "unknown";
        var chatId = root.TryGetProperty("chatId", out var chatProp) ? chatProp.GetString() : null;

        var text = root.TryGetProperty("text", out var textProp)
            ? textProp.GetString() ?? ""
            : "";

        var timestamp = root.TryGetProperty("timestamp", out var tsProp)
                        && DateTimeOffset.TryParse(tsProp.GetString(), out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

        return new UnifiedInboundMessage(
            ExternalUserId: userId,
            ChatId: chatId,
            Text: text,
            Attachments: [],
            Timestamp: timestamp,
            RawJson: rawJson);
    }
}

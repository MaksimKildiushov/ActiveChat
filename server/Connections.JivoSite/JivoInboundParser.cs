using Ac.Application.Contracts.Interfaces;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;
using System.Text.Json;

namespace Connections.JivoSite;

public sealed class JivoInboundParser : IInboundParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ChannelType ChannelType => ChannelType.JivoSite;

    public UnifiedInboundMessage Parse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            throw new ArgumentException("rawJson is empty", nameof(rawJson));

        using var doc = JsonDocument.Parse(rawJson);
        JsonElement payload = ExtractPayload(doc.RootElement);

        var evt = payload.Deserialize<JivoClientMessageEvent>(JsonOptions)
                  ?? throw new InvalidOperationException("Failed to deserialize Jivo payload.");

        if (!string.Equals(evt.Event, "CLIENT_MESSAGE", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Unsupported Jivo event: '{evt.Event}'.");

        var timestamp = DateTimeOffset.FromUnixTimeSeconds(evt.Message.Timestamp);

        // TEXT -> берём text, иначе пусто по контракту UnifiedInboundMessage
        var text = string.Equals(evt.Message.Type, "TEXT", StringComparison.OrdinalIgnoreCase)
            ? (evt.Message.Text ?? string.Empty)
            : string.Empty;

        var attachments = ExtractAttachments(payload);

        return new UnifiedInboundMessage(
            ExternalUserId: evt.ClientId,
            ChatId: evt.ChatId,
            Text: text,
            Attachments: attachments,
            Timestamp: timestamp,
            RawJson: rawJson
        );
    }

    private static JsonElement ExtractPayload(JsonElement root)
    {
        // case A: пришёл массив [ { ..., body: {...} } ]
        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            var first = root.EnumerateArray().First();

            if (first.ValueKind == JsonValueKind.Object &&
                first.TryGetProperty("body", out var body) &&
                body.ValueKind == JsonValueKind.Object)
                return body;

            if (first.ValueKind == JsonValueKind.Object)
                return first;
        }

        // case B: пришёл объект { ..., body: {...} }
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("body", out var body2) &&
            body2.ValueKind == JsonValueKind.Object)
            return body2;

        // case C: пришёл чистый объект события Jivo
        if (root.ValueKind == JsonValueKind.Object)
            return root;

        throw new InvalidOperationException("Unsupported JSON shape for inbound Jivo payload.");
    }

    /// <summary>
    /// БЕЗОПАСНО: если не нашли вложений — возвращаем пустой список.
    /// Допилим, когда ты пришлёшь пример payload с file/image/etc.
    /// </summary>
    private static IReadOnlyList<string> ExtractAttachments(JsonElement payload)
    {
        // Jivo Bot API может присылать разные message.type (TEXT/FILE/PHOTO/…)
        // В доке и в реальности ключи могут отличаться, поэтому пока делаем "не ломающее" извлечение.
        // Если найдём явные URL/ID — вернём, иначе пусто.

        if (!payload.TryGetProperty("message", out var msg) || msg.ValueKind != JsonValueKind.Object)
            return Array.Empty<string>();

        // пример “универсального” поиска полей, которые часто встречаются:
        // url, file_url, link, src, id, file_id и т.п.
        // (не навязываем — просто вытаскиваем строки)
        var candidates = new List<string>();

        void AddIfStringProp(string propName)
        {
            if (msg.TryGetProperty(propName, out var p) && p.ValueKind == JsonValueKind.String)
            {
                var s = p.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    candidates.Add(s!);
            }
        }

        AddIfStringProp("url");
        AddIfStringProp("file_url");
        AddIfStringProp("link");
        AddIfStringProp("src");
        AddIfStringProp("file_id");
        AddIfStringProp("id");

        // бывает, что вложения лежат массивом
        if (msg.TryGetProperty("attachments", out var att) && att.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in att.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        candidates.Add(s!);
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    if (item.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String)
                    {
                        var s = u.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            candidates.Add(s!);
                    }
                    if (item.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String)
                    {
                        var s = i.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            candidates.Add(s!);
                    }
                }
            }
        }

        return candidates.Count == 0 ? Array.Empty<string>() : candidates;
    }
}

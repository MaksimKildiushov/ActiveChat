using System.Text.Json.Serialization;

namespace Connections.JivoSite;

public sealed class JivoClientMessageEvent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("site_id")]
    public string SiteId { get; init; } = default!;

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = default!;

    [JsonPropertyName("chat_id")]
    public string ChatId { get; init; } = default!;

    [JsonPropertyName("agents_online")]
    public bool AgentsOnline { get; init; }

    [JsonPropertyName("sender")]
    public JivoSender Sender { get; init; } = default!;

    [JsonPropertyName("message")]
    public JivoMessage Message { get; init; } = default!;

    [JsonPropertyName("channel")]
    public JivoChannel Channel { get; init; } = default!;

    [JsonPropertyName("event")]
    public string Event { get; init; } = default!;
}

public sealed class JivoSender
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("user_token")]
    public string? UserToken { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("url")]
    public string Url { get; init; } = default!;

    [JsonPropertyName("has_contacts")]
    public bool HasContacts { get; init; }
}

public sealed class JivoMessage
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = default!; // TEXT, etc.

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; } // unix seconds
}

public sealed class JivoChannel
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; init; } = default!; // widget, etc.
}

using System.Text.Json.Serialization;

namespace Connections.JivoSite;

public sealed class JivoOutboundMessageDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = default!; // TEXT

    [JsonPropertyName("text")]
    public string Text { get; init; } = default!;
}

// ===== DTO под Jivo BOT_MESSAGE =====
public sealed class JivoBotMessagePayload
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = default!;

    [JsonPropertyName("chat_id")]
    public string ChatId { get; init; } = default!;

    [JsonPropertyName("message")]
    public JivoOutboundMessageDto Message { get; init; } = default!;

    [JsonPropertyName("event")]
    public string Event { get; init; } = default!;
}

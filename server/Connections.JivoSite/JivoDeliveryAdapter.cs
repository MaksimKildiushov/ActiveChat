using Ac.Application.Contracts.Interfaces;
using Ac.Application.Contracts.Models;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Connections.JivoSite;

public sealed class JivoDeliveryAdapter(HttpClient http) : IChannelDeliveryAdapter
{
    public ChannelType ChannelType => ChannelType.JivoSite;

    public async Task DeliverAsync(OutboundMessage message, CancellationToken ct = default)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (message.ChannelContext.ChannelType != ChannelType.JivoSite)
            throw new InvalidOperationException($"Wrong adapter for channel type {message.ChannelContext.ChannelType}");

        var settings = JivoSettings.FromJson(message.ChannelContext.SettingsJson);

        // Jivo требует и chat_id, и client_id
        var chatId = message.ChatId;
        var clientId = settings.ClientId;

        var payloads = BuildPayloads(message.Intent, clientId, chatId);

        // Некоторые intent’ы могут ничего не отправлять (HandoffIntent без Message)
        foreach (var payload in payloads)
        {
            using var resp = await http.PostAsJsonAsync(settings.WebhookUrl, payload, JivoSettings.Options, ct);
            resp.EnsureSuccessStatusCode();
        }
    }

    private static IEnumerable<JivoBotMessagePayload> BuildPayloads(ReplyIntent intent, string clientId, string chatId)
    {
        // Игнорировать VIDEO/AUDIO/VOICE/LOCATION — у тебя таких intent’ов нет, поэтому просто не реализуем.

        return intent switch
        {
            TextIntent t => new[] { BotText(clientId, chatId, t.Text) },

            ButtonsIntent b => new[] { BotText(clientId, chatId, RenderButtons(b.Text, b.Buttons)) },

            HandoffIntent h => string.IsNullOrWhiteSpace(h.Message)
                ? Array.Empty<JivoBotMessagePayload>()
                : new[] { BotText(clientId, chatId, h.Message!) },

            // Это не “доставка в канал”, это действие системы — пусть выполняется другим компонентом pipeline
            CallClientApiIntent => throw new NotSupportedException("CallClientApiIntent must be handled by pipeline executor, not channel adapter."),

            _ => throw new NotSupportedException($"Unknown intent: {intent.GetType().Name}")
        };
    }

    private static string RenderButtons(string text, IReadOnlyList<string> buttons)
    {
        if (buttons is null || buttons.Count == 0) return text ?? string.Empty;

        // максимально нейтрально: список вариантов
        // 1) текст
        // 2) варианты в виде строк (потом пользователь пишет руками)
        return (text ?? string.Empty)
               + "\n\nВарианты:\n"
               + string.Join("\n", buttons.Select(b => $"• {b}"));
    }

    private static JivoBotMessagePayload BotText(string clientId, string chatId, string text) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        ClientId = clientId,
        ChatId = chatId,
        Event = "BOT_MESSAGE",
        Message = new JivoOutboundMessageDto
        {
            Type = "TEXT",
            Text = text ?? string.Empty
        }
    };

    // ===== Settings =====
    private sealed class JivoSettings
    {
        [JsonPropertyName("webhookUrl")]
        public string WebhookUrl { get; init; } = default!;

        [JsonPropertyName("clientId")]
        public string ClientId { get; init; } = default!;

        public static JivoSettings FromJson(string? settingsJson)
        {
            if (string.IsNullOrWhiteSpace(settingsJson))
                throw new InvalidOperationException("ChannelContext.SettingsJson is empty. Expected Jivo settings with webhookUrl and clientId.");

            var settings = JsonSerializer.Deserialize<JivoSettings>(settingsJson, Options);
            if (settings is null ||
                string.IsNullOrWhiteSpace(settings.WebhookUrl) ||
                string.IsNullOrWhiteSpace(settings.ClientId))
                throw new InvalidOperationException("Invalid Jivo settings. Expected JSON: {\"webhookUrl\":\"...\",\"clientId\":\"...\"}");

            return settings;
        }

        public static readonly JsonSerializerOptions Options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }
}

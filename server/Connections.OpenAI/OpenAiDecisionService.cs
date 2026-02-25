using System.Net.Http.Json;
using System.Text.Json;
using Ac.Application.Contracts.Interfaces;
using Ac.Application.Contracts.Models;
using Ac.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connections.OpenAi;

public sealed class OpenAiDecisionService : IAiDecisionService
{
    private readonly OpenAiOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiDecisionService> _logger;

    public OpenAiDecisionService(
        IOptions<OpenAiOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAiDecisionService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DecisionResult> DecideAsync(DecisionContext context, CancellationToken ct = default)
    {
        var (apiKey, model) = ResolveApiKeyAndModel(context.ChannelContext.SchemaName);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAi ApiKey не задан (ни в OpenAi, ни в OpenAi:Clients:{Schema}). Fallback в Handoff.",
                context.ChannelContext.SchemaName);
            return new DecisionResult(
                StepKind: StepKind.Handoff,
                Confidence: 0,
                Slots: new Dictionary<string, string>(),
                ClarificationQuestion: null);
        }

        var userText = context.InboundMessage.Text?.Trim() ?? string.Empty;
        var lastMessage = context.Conversation.LastMessage;

        var systemPrompt = """
            Ты — бот поддержки в чате. На каждое сообщение пользователя нужно вернуть решение в формате JSON (только JSON, без markdown и пояснений):
            {
              "step_kind": "Answer" | "AskClarification" | "Handoff",
              "confidence": число от 0 до 1,
              "proposed_text": "текст ответа пользователю" (обязателен для Answer),
              "clarification_question": "вопрос на уточнение" (обязателен для AskClarification),
              "slots": {}
            }
            Правила:
            - Answer — если можешь дать полезный ответ по существу.
            - AskClarification — если запрос непонятен или не хватает данных.
            - Handoff — если пользователь просит оператора/человека, или запрос вне твоей компетенции.
            Отвечай кратко и по-русски.
            """;

        var userContent = string.IsNullOrEmpty(lastMessage)
            ? userText
            : $"Предыдущее сообщение: {lastMessage}\n\nТекущее: {userText}";

        if (string.IsNullOrWhiteSpace(userContent))
        {
            return new DecisionResult(
                StepKind: StepKind.AskClarification,
                Confidence: 0.9,
                Slots: new Dictionary<string, string>(),
                ClarificationQuestion: "Не получил сообщение. Что вы хотели сказать?");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("OpenAi");
            var request = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userContent }
                },
                response_format = new { type = "json_object" }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            httpRequest.Headers.Add("Authorization", "Bearer " + apiKey);
            httpRequest.Content = JsonContent.Create(request);

            var response = await httpClient.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement?>(cancellationToken: ct);
            if (!json.HasValue || json.Value.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("OpenAi вернул пустой ответ.");
                return FallbackResult(userText);
            }
            var choices = json.Value.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                _logger.LogWarning("OpenAi вернул пустой список choices.");
                return FallbackResult(userText);
            }
            var content = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("OpenAi вернул пустой content.");
                return FallbackResult(userText);
            }

            return ParseResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Запрос к OpenAi завершился с ошибкой.");
            return FallbackResult(userText);
        }
    }

    /// <summary>
    /// Берёт ApiKey и Model из OpenAi:Clients:{schemaName}, при отсутствии — из OpenAi (глобальные).
    /// </summary>
    private (string ApiKey, string Model) ResolveApiKeyAndModel(string schemaName)
    {
        if (!string.IsNullOrEmpty(schemaName) &&
            _options.Clients != null &&
            _options.Clients.TryGetValue(schemaName, out var client) &&
            !string.IsNullOrWhiteSpace(client.ApiKey))
        {
            return (client.ApiKey, client.Model ?? _options.Model);
        }
        return (_options.ApiKey, _options.Model);
    }

    private static DecisionResult ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var stepKindStr = root.TryGetProperty("step_kind", out var sk)
            ? sk.GetString() ?? "Answer"
            : "Answer";
        var stepKind = stepKindStr switch
        {
            "AskClarification" => StepKind.AskClarification,
            "Handoff" => StepKind.Handoff,
            _ => StepKind.Answer
        };

        var confidence = root.TryGetProperty("confidence", out var conf)
            ? Math.Clamp(conf.GetDouble(), 0, 1)
            : 0.8;

        var slots = new Dictionary<string, string>();
        if (root.TryGetProperty("slots", out var slotsEl) && slotsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in slotsEl.EnumerateObject())
                if (prop.Value.ValueKind == JsonValueKind.String)
                    slots[prop.Name] = prop.Value.GetString() ?? string.Empty;
        }

        var proposedText = root.TryGetProperty("proposed_text", out var pt) ? pt.GetString() : null;
        var clarificationQuestion = root.TryGetProperty("clarification_question", out var cq) ? cq.GetString() : null;

        return new DecisionResult(
            StepKind: stepKind,
            Confidence: confidence,
            Slots: slots,
            ProposedText: proposedText,
            ClarificationQuestion: clarificationQuestion);
    }

    private static DecisionResult FallbackResult(string userText)
    {
        return new DecisionResult(
            StepKind: StepKind.Answer,
            Confidence: 0.5,
            Slots: new Dictionary<string, string>(),
            ProposedText: string.IsNullOrWhiteSpace(userText) ? "Понял вас." : $"Вы написали: {userText}");
    }
}

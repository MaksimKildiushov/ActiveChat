using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Domain.Enums;

namespace Ac.Application.Services;

/// <summary>
/// Заглушка AI: реальную реализацию подключить через IAiDecisionService.
/// </summary>
public class StubAiDecisionService : IAiDecisionService
{
    public Task<DecisionResult> DecideAsync(DecisionContext context, CancellationToken ct = default)
    {
        var text = context.InboundMessage.Text.Trim();

        DecisionResult result = text switch
        {
            { Length: 0 } => new(
                StepKind: StepKind.AskClarification,
                Confidence: 0.9,
                Slots: new Dictionary<string, string>(),
                ClarificationQuestion: "Не получил сообщение. Что вы хотели сказать?"),

            var t when t.Contains("оператор", StringComparison.OrdinalIgnoreCase) => new(
                StepKind: StepKind.Handoff,
                Confidence: 0.95,
                Slots: new Dictionary<string, string>()),

            var t => new(
                StepKind: StepKind.Answer,
                Confidence: 0.8,
                Slots: new Dictionary<string, string>(),
                ProposedText: $"Вы сказали: {t}")
        };

        return Task.FromResult(result);
    }
}

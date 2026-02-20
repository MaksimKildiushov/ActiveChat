using Ac.Domain.Enums;

namespace Ac.Application.Models;

public record DecisionResult(
    StepKind StepKind,
    double Confidence,
    IReadOnlyDictionary<string, string> Slots,
    string? ProposedText = null,
    string? ClarificationQuestion = null);

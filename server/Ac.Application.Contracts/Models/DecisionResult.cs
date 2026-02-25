using Ac.Domain.Enums;

namespace Ac.Application.Contracts.Models;

public record DecisionResult(
    StepKind StepKind,
    double Confidence,
    IReadOnlyDictionary<string, string> Slots,
    string? ProposedText = null,
    string? ClarificationQuestion = null);

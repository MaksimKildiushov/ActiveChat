using Ac.Application.Contracts.Models;
using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Handlers;

public class AnswerStepHandler : IStepHandler
{
    public StepKind StepKind => StepKind.Answer;

    public Task<ReplyIntent> HandleAsync(StepContext context, CancellationToken ct = default)
    {
        var text = context.DecisionResult.ProposedText ?? "Понял вас.";
        return Task.FromResult<ReplyIntent>(new TextIntent(text));
    }
}

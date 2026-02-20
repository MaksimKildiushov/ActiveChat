using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Handlers;

public class AskClarificationStepHandler : IStepHandler
{
    public StepKind StepKind => StepKind.AskClarification;

    public Task<ReplyIntent> HandleAsync(StepContext context, CancellationToken ct = default)
    {
        var question = context.DecisionResult.ClarificationQuestion ?? "Уточните, пожалуйста, запрос.";
        return Task.FromResult<ReplyIntent>(new TextIntent(question));
    }
}

using Ac.Application.Contracts.Models;
using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Services;

public class StepDispatcher(IEnumerable<IStepHandler> handlers)
{
    private readonly Dictionary<StepKind, IStepHandler> _handlers = handlers.ToDictionary(h => h.StepKind);

    public Task<ReplyIntent> DispatchAsync(StepContext context, CancellationToken ct = default)
        => _handlers.TryGetValue(context.DecisionResult.StepKind, out var handler)
            ? handler.HandleAsync(context, ct)
            : throw new InvalidOperationException(
                $"No handler registered for StepKind '{context.DecisionResult.StepKind}'. Register IStepHandler implementation.");
}

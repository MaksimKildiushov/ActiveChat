using Ac.Application.Models;

namespace Ac.Application.Interfaces;

public interface IAiDecisionService
{
    Task<DecisionResult> DecideAsync(DecisionContext context, CancellationToken ct = default);
}

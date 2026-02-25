using Ac.Application.Contracts.Models;

namespace Ac.Application.Contracts.Interfaces;

public interface IAiDecisionService
{
    Task<DecisionResult> DecideAsync(DecisionContext context, CancellationToken ct = default);
}

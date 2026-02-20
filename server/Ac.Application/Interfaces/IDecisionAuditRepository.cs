using Ac.Domain.Entities;

namespace Ac.Application.Interfaces;

public interface IDecisionAuditRepository
{
    Task AddAsync(DecisionAuditEntity audit, CancellationToken ct = default);
}

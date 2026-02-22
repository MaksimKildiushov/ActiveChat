using Ac.Application.Interfaces;
using Ac.Domain.Entities;

namespace Ac.Data.Repositories;

public class DecisionAuditRepository(ApiDb db) : IDecisionAuditRepository
{
    public async Task AddAsync(DecisionAuditEntity audit, CancellationToken ct = default)
        => await db.DecisionAudits.AddAsync(audit, ct);
}

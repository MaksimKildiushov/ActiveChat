using Ac.Application.Interfaces;

namespace Ac.Data.Repositories;

public class UnitOfWork(TenantDb db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}

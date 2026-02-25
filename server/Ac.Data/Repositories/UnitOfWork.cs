namespace Ac.Data.Repositories;

public class UnitOfWork(TenantDb db)
{
    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}

using Ac.Data.Accessors;
using Libraries.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
namespace Ac.Data;

public sealed class AuditingInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider clock) : SaveChangesInterceptor
{
    private void ApplyAudit(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<IBaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // Не трогаем, если уже выставлено (например, миграции/импорт).
                if (entry.Property(e => e.Created).CurrentValue == default)
                    entry.Property(e => e.Created).CurrentValue = clock.UtcNow;

                if (entry.Property(e => e.AuthorId).CurrentValue == default)
                    entry.Property(e => e.AuthorId).CurrentValue = currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Защитим от случайной модификации Created.
                entry.Property(e => e.Created).IsModified = false;
                entry.Property(e => e.AuthorId).IsModified = false;

                entry.Property(e => e.Modified).CurrentValue = clock.UtcNow;
                entry.Property(e => e.ModifierId).CurrentValue = currentUser.UserId;
            }
        }
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

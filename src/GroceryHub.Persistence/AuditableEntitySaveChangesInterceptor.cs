using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GroceryHub.Persistence;

public sealed class AuditableEntitySaveChangesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditAndSoftDeleteRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditAndSoftDeleteRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is ISoftDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = utcNow;

                if (entry.Entity is IAuditableEntity)
                {
                    entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = utcNow;
                }

                continue;
            }

            if (entry.Entity is IAuditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = utcNow;
                    entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = utcNow;
                }
            }
        }
    }
}

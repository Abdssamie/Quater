using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quater.Shared.Interfaces;

namespace Quater.Backend.Data.Interceptors;

/// <summary>
/// EF Core interceptor that implements soft delete functionality.
/// Prevents hard deletion of entities with IsDeleted property by converting DELETE operations to UPDATE operations.
/// </summary>
/// <remarks>
/// This interceptor automatically:
/// - Intercepts DELETE commands and converts them to UPDATE commands that set IsDeleted = true
/// - Applies to all entities that have an IsDeleted boolean property
/// - Maintains referential integrity while preserving data for sync and audit purposes
/// 
/// Usage in DbContext:
/// <code>
/// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
/// {
///     optionsBuilder.AddInterceptors(new SoftDeleteInterceptor());
/// }
/// </code>
/// </remarks>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Intercepts SaveChanges operations to implement soft delete logic.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null)
        {
            return base.SavingChanges(eventData, result);
        }

        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Intercepts async SaveChanges operations to implement soft delete logic.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Applies soft delete logic to all entities marked for deletion.
    /// </summary>
    /// <param name="context">The DbContext containing the entities.</param>
    private static void ApplySoftDelete(DbContext context)
    {
        // Find all entities marked for deletion
        var deletedEntries = context.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Deleted)
            .Where(e => e.Entity is ISoftDelete)
            .ToList(); // Materialize to avoid modification during iteration

        foreach (var entry in deletedEntries)
        {
            // Check if entity has IsDeleted property
            var isDeletedProperty = entry.Entity.GetType().GetProperty("IsDeleted");
            if (isDeletedProperty is null || isDeletedProperty.PropertyType != typeof(bool))
            {
                throw new InvalidOperationException("Unexpected Error. IAuditable models must " +
                                                    "have IsDeleted property and it must be bool");
            }

            // Convert DELETE to UPDATE by setting IsDeleted = true
            entry.State = EntityState.Modified;
            isDeletedProperty.SetValue(entry.Entity, true);

            // Also set DeletedAt if the entity supports it
            var deletedAtProperty = entry.Entity.GetType().GetProperty("DeletedAt");
            if (deletedAtProperty != null && deletedAtProperty.PropertyType == typeof(DateTime?))
            {
                deletedAtProperty.SetValue(entry.Entity, DateTime.UtcNow);
            }
            else
            {
                throw new InvalidOperationException("Unexpected Error. IAuditable models must " +
                                                    "have DeletedAt property and it must be DateTime");
            }

            // Ensure owned entities are properly included
            // When switching from Deleted to Modified, owned entities need to be marked as well
            foreach (var navigation in entry.Navigations)
            {
                if (
                    !navigation.Metadata.TargetEntityType.IsOwned() || navigation.CurrentValue == null
                    ) continue;

                var ownedEntry = context.Entry(navigation.CurrentValue);
                if (ownedEntry.State is EntityState.Detached or EntityState.Deleted)
                {
                    ownedEntry.State = EntityState.Unchanged;
                }
            }
        }
    }
}

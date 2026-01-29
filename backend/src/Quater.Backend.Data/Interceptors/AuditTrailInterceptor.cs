using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quater.Shared.Models;
using System.Text.Json;

namespace Quater.Backend.Data.Interceptors;

/// <summary>
/// EF Core interceptor that automatically creates audit log entries for entity changes.
/// Tracks INSERT, UPDATE, and DELETE operations with before/after values.
/// </summary>
/// <remarks>
/// This interceptor automatically:
/// - Creates AuditLog entries for all entity changes (except AuditLog itself)
/// - Captures old and new values as JSON for UPDATE operations
/// - Records the user who made the change (from ICurrentUserService)
/// - Timestamps all changes with UTC time
/// - Captures IP address if available
/// 
/// Usage in DbContext:
/// <code>
/// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
/// {
///     optionsBuilder.AddInterceptors(new AuditTrailInterceptor(currentUserService, httpContextAccessor));
/// }
/// </code>
/// </remarks>
public class AuditTrailInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService? _currentUserService;
    private readonly string? _ipAddress;

    /// <summary>
    /// Initializes a new instance of the AuditTrailInterceptor.
    /// </summary>
    /// <param name="currentUserService">Service to get current user information (optional).</param>
    /// <param name="ipAddress">IP address of the client making the change (optional).</param>
    public AuditTrailInterceptor(ICurrentUserService? currentUserService = null, string? ipAddress = null)
    {
        _currentUserService = currentUserService;
        _ipAddress = ipAddress;
    }

    /// <summary>
    /// Intercepts SaveChanges operations to create audit log entries.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null)
        {
            return base.SavingChanges(eventData, result);
        }

        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Intercepts async SaveChanges operations to create audit log entries.
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

        CreateAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Creates audit log entries for all tracked entity changes.
    /// </summary>
    /// <param name="context">The DbContext containing the entities.</param>
    private void CreateAuditLogs(DbContext context)
    {
        var userId = _currentUserService?.GetCurrentUserId() ?? "System";
        var timestamp = DateTime.UtcNow;

        // Get all modified, added, or deleted entities (except AuditLog itself)
        var auditableEntries = context.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Added || 
                       e.State == EntityState.Modified || 
                       e.State == EntityState.Deleted)
            .Where(e => e.Entity is not AuditLog && e.Entity is not AuditLogArchive)
            .ToList();

        foreach (var entry in auditableEntries)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityIdProperty = entry.Entity.GetType().GetProperty("Id");
            
            if (entityIdProperty == null)
            {
                continue; // Skip entities without Id property
            }

            var entityId = entityIdProperty.GetValue(entry.Entity) as Guid?;
            if (entityId == null)
            {
                continue; // Skip if Id is not a Guid
            }

            string action = entry.State switch
            {
                EntityState.Added => "INSERT",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "UNKNOWN"
            };

            string? oldValue = null;
            string? newValue = null;

            if (entry.State == EntityState.Modified)
            {
                // Capture old and new values for modified entities
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    if (property.IsModified)
                    {
                        oldValues[property.Metadata.Name] = property.OriginalValue;
                        newValues[property.Metadata.Name] = property.CurrentValue;
                    }
                }

                if (oldValues.Any())
                {
                    oldValue = JsonSerializer.Serialize(oldValues);
                    newValue = JsonSerializer.Serialize(newValues);
                }
            }
            else if (entry.State == EntityState.Added)
            {
                // Capture new values for added entities
                var values = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    values[property.Metadata.Name] = property.CurrentValue;
                }
                newValue = JsonSerializer.Serialize(values);
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Capture old values for deleted entities
                var values = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    values[property.Metadata.Name] = property.CurrentValue;
                }
                oldValue = JsonSerializer.Serialize(values);
            }

            // Create audit log entry
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EntityType = entityType,
                EntityId = entityId.Value,
                Action = action,
                OldValue = oldValue?.Length > 4000 ? oldValue.Substring(0, 4000) : oldValue,
                NewValue = newValue?.Length > 4000 ? newValue.Substring(0, 4000) : newValue,
                Timestamp = timestamp,
                IpAddress = _ipAddress,
                IsArchived = false
            };

            context.Set<AuditLog>().Add(auditLog);
        }
    }
}

/// <summary>
/// Interface for getting current user information.
/// Implement this in your application to provide user context to the audit interceptor.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    /// <returns>The user ID, or "System" if no user is authenticated.</returns>
    string GetCurrentUserId();
}

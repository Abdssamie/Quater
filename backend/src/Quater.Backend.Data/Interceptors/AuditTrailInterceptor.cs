using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;
using Quater.Shared.Models;
using System.Text.Json;

namespace Quater.Backend.Data.Interceptors;

/// <summary>
/// EF Core interceptor that automatically creates audit log entries for entities implementing IAuditable.
/// Tracks INSERT, UPDATE, and DELETE operations with before/after values.
/// </summary>
/// <remarks>
/// This interceptor automatically:
/// - Creates AuditLog entries ONLY for entities implementing IAuditable
/// - Captures only properties that actually changed (for UPDATE operations)
/// - Always includes IAuditable properties (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
/// - Truncates individual property values exceeding 50 characters (keeps JSON valid)
/// - Records the user who made the change (from ICurrentUserService, defaults to "System")
/// - Timestamps all changes with UTC time
/// - Captures IP address if available
/// - Throws exception if entity implements IAuditable but has no EntityType enum value
/// 
/// Audited entities: Lab, Sample, TestResult, Parameter
/// 
/// Usage in DbContext:
/// <code>
/// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
/// {
///     optionsBuilder.AddInterceptors(new AuditTrailInterceptor(currentUserService, ipAddress, logger));
/// }
/// </code>
/// </remarks>
public class AuditTrailInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditTrailInterceptor>? _logger;
    private readonly string? _ipAddress;
    private readonly AsyncLocal<bool> _isSavingAuditLogs = new();
    private readonly AsyncLocal<List<AuditLogData>> _pendingAuditLogs = new();

    /// <summary>
    /// Initializes a new instance of the AuditTrailInterceptor.
    /// </summary>
    /// <param name="currentUserService">Service to get current user information.</param>
    /// <param name="ipAddress">IP address of the client making the change (optional).</param>
    /// <param name="logger">Logger for diagnostic information (optional).</param>
    public AuditTrailInterceptor(
        ICurrentUserService currentUserService,
        string? ipAddress = null,
        ILogger<AuditTrailInterceptor>? logger = null)
    {
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _ipAddress = ipAddress;
        _logger = logger;
    }

    /// <summary>
    /// Intercepts SaveChanges operations to capture audit data and add audit logs before save.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null || _isSavingAuditLogs.Value)
        {
            return base.SavingChanges(eventData, result);
        }

        CaptureAuditData(eventData.Context);

        // Add audit logs to the context BEFORE SaveChanges completes (same transaction)
        AddAuditLogsToContext(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Intercepts async SaveChanges operations to capture audit data and add audit logs before save.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null || _isSavingAuditLogs.Value)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        CaptureAuditData(eventData.Context);

        // Add audit logs to the context BEFORE SaveChanges completes (same transaction)
        AddAuditLogsToContext(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Adds captured audit logs to the context (to be saved in the same transaction).
    /// </summary>
    private void AddAuditLogsToContext(DbContext context)
    {
        var pendingLogs = _pendingAuditLogs.Value;
        if (pendingLogs == null || pendingLogs.Count == 0)
            return;

        _isSavingAuditLogs.Value = true;

        try
        {
            foreach (var auditLog in pendingLogs.Select(
                         auditData => new AuditLog
                         {
                             Id = Guid.NewGuid(),
                             UserId = auditData.UserId,
                             EntityType = auditData.EntityType,
                             EntityId = auditData.EntityId,
                             Action = auditData.Action,
                             OldValue = auditData.OldValue,
                             NewValue = auditData.NewValue,
                             Timestamp = auditData.Timestamp,
                             IpAddress = auditData.IpAddress,
                             IsArchived = false,
                             IsTruncated = auditData.IsTruncated
                         })
                     )
            {
                context.Set<AuditLog>().Add(auditLog);
            }

            pendingLogs.Clear();
        }
        finally
        {
            _isSavingAuditLogs.Value = false;
        }
    }

    /// <summary>
    /// Intercepts async SavedChanges to persist audit logs after the main save completes.
    /// </summary>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // No longer needed - audit logs are added in SavingChangesAsync
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Captures audit data from tracked entity changes before SaveChanges completes.
    /// </summary>
    /// <param name="context">The DbContext containing the entities.</param>
    private void CaptureAuditData(DbContext context)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger?.LogDebug("Capturing audit data for user: {UserId}", userId);

        var timestamp = DateTime.UtcNow;

        // Get all modified, added, or deleted entities that implement IAuditable
        var auditableEntries = context.ChangeTracker
            .Entries()
            .Where(e => e.State
                is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
            .Where(e => e.Entity is IAuditable)
            .ToList();

        _logger?.LogDebug("Found {Count} auditable entities to process", auditableEntries.Count);

        // Initialize or clear pending audit logs for this save operation
        if (_pendingAuditLogs.Value == null)
        {
            _pendingAuditLogs.Value = [];
        }
        else
        {
            _pendingAuditLogs.Value.Clear();
        }

        foreach (var entry in auditableEntries)
        {
            var entityTypeName = entry.Entity.GetType().Name;
            var entityIdProperty = entry.Entity.GetType().GetProperty("Id");

            if (entityIdProperty == null || entityIdProperty.GetValue(entry.Entity) is not Guid entityId)
            {
                throw new InvalidOperationException("Unexpected error: " +
                                                    "IAuditable models must have id property " +
                                                    "and it must be Guid");
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Modified => AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => AuditAction.Update
            };

            string? oldValue = null;
            string? newValue = null;
            bool isTruncated = false;

            switch (entry.State)
            {
                case EntityState.Modified:
                    {
                        // Capture old and new values for modified entities (only changed properties)
                        var oldValues = new Dictionary<string, object?>();
                        var newValues = new Dictionary<string, object?>();

                        foreach (var property in entry.Properties)
                        {
                            if (!property.IsModified) continue; // Only changed properties
                            oldValues[property.Metadata.Name] = property.OriginalValue;
                            newValues[property.Metadata.Name] = property.CurrentValue;
                        }

                        if (oldValues.Count != 0)
                        {
                            // Truncate individual property values, not the entire JSON
                            var (truncatedOld, oldTruncated) = TruncatePropertyValues(oldValues);
                            var (truncatedNew, newTruncated) = TruncatePropertyValues(newValues);

                            oldValue = JsonSerializer.Serialize(truncatedOld);
                            newValue = JsonSerializer.Serialize(truncatedNew);
                            isTruncated = oldTruncated || newTruncated;

                            if (isTruncated)
                            {
                                _logger?.LogWarning(
                                    "Property values truncated for {EntityType} {EntityId}",
                                    entityTypeName, entityId);
                            }
                        }

                        break;
                    }
                case EntityState.Added:
                    {
                        // Capture new values for added entities
                        var values = new Dictionary<string, object?>();
                        foreach (var property in entry.Properties)
                        {
                            values[property.Metadata.Name] = property.CurrentValue;
                        }

                        var (truncatedValues, wasTruncated) = TruncatePropertyValues(values);
                        newValue = JsonSerializer.Serialize(truncatedValues);
                        isTruncated = wasTruncated;

                        if (isTruncated)
                        {
                            _logger?.LogWarning(
                                "Property values truncated for new {EntityType} {EntityId}",
                                entityTypeName, entityId);
                        }

                        break;
                    }
                case EntityState.Deleted:
                    {
                        // Capture old values for deleted entities
                        var values = new Dictionary<string, object?>();
                        foreach (var property in entry.Properties)
                        {
                            values[property.Metadata.Name] = property.OriginalValue; // Use OriginalValue
                        }

                        var (truncatedValues, wasTruncated) = TruncatePropertyValues(values);
                        oldValue = JsonSerializer.Serialize(truncatedValues);
                        isTruncated = wasTruncated;

                        if (isTruncated)
                        {
                            _logger?.LogWarning(
                                "Property values truncated for deleted {EntityType} {EntityId}",
                                entityTypeName, entityId);
                        }

                        break;
                    }
                case EntityState.Detached:
                case EntityState.Unchanged:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Convert entity type name to EntityType enum
            if (!Enum.TryParse<EntityType>(entityTypeName, out var entityTypeEnum))
            {
                _logger?.LogError(
                    "Entity {EntityType} implements IAuditable but has no matching EntityType enum value",
                    entityTypeName);
                throw new InvalidOperationException(
                    $"Entity '{entityTypeName}' implements IAuditable but has no corresponding EntityType enum value. " +
                    $"Add '{entityTypeName}' to the EntityType enum.");
            }

            // Store audit data for later persistence
            _pendingAuditLogs.Value.Add(new AuditLogData
            {
                UserId = userId,
                EntityType = entityTypeEnum,
                EntityId = entityId,
                Action = action,
                OldValue = oldValue,
                NewValue = newValue,
                Timestamp = timestamp,
                IpAddress = _ipAddress,
                IsTruncated = isTruncated
            });
        }
    }

    /// <summary>
    /// Truncates individual property values that exceed maxLength while keeping JSON structure valid.
    /// </summary>
    private static (Dictionary<string, object?> values, bool wasTruncated) TruncatePropertyValues(
        Dictionary<string, object?> values,
        int maxLength = 50)
    {
        var wasTruncated = false;
        var result = new Dictionary<string, object?>();

        foreach (var kvp in values)
        {
            if (kvp.Value is string strValue && strValue.Length > maxLength)
            {
                result[kvp.Key] = strValue[..(maxLength - 15)] + "...[TRUNCATED]";
                wasTruncated = true;
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return (result, wasTruncated);
    }

    /// <summary>
    /// Internal data structure to hold audit log information between SavingChanges and SavedChanges events.
    /// </summary>
    private class AuditLogData
    {
        public required Guid UserId { get; init; }
        public required EntityType EntityType { get; init; }
        public required Guid EntityId { get; init; }
        public required AuditAction Action { get; init; }
        public string? OldValue { get; init; }
        public string? NewValue { get; init; }
        public required DateTime Timestamp { get; init; }
        public string? IpAddress { get; init; }
        public bool IsTruncated { get; init; }
    }
}


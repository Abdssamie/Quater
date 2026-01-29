using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Service for creating and managing conflict backups
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Create a backup of conflicting versions
    /// </summary>
    Task<ConflictBackup> CreateBackupAsync(
        Guid entityId,
        string entityType,
        string serverVersion,
        string clientVersion,
        ConflictResolutionStrategy strategy,
        string deviceId,
        Guid labId,
        CancellationToken ct = default);

    /// <summary>
    /// Mark a conflict as resolved
    /// </summary>
    Task MarkAsResolvedAsync(
        Guid backupId,
        string resolvedBy,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get unresolved conflicts for a lab
    /// </summary>
    Task<List<ConflictBackup>> GetUnresolvedConflictsAsync(
        Guid labId,
        CancellationToken ct = default);

    /// <summary>
    /// Get conflict backup by entity
    /// </summary>
    Task<ConflictBackup?> GetBackupByEntityAsync(
        Guid entityId,
        string entityType,
        CancellationToken ct = default);
}

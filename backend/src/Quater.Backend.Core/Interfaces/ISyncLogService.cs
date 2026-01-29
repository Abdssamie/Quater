using Quater.Shared.Models;

namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Service for tracking synchronization operations
/// </summary>
public interface ISyncLogService
{
    /// <summary>
    /// Create a new sync log entry
    /// </summary>
    Task<SyncLog> CreateSyncLogAsync(
        string deviceId,
        string userId,
        string status,
        CancellationToken ct = default);

    /// <summary>
    /// Update sync log with results
    /// </summary>
    Task UpdateSyncLogAsync(
        Guid syncLogId,
        string status,
        int recordsSynced,
        int conflictsDetected,
        int conflictsResolved,
        string? errorMessage = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get last successful sync for a device
    /// </summary>
    Task<SyncLog?> GetLastSuccessfulSyncAsync(
        string deviceId,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Get sync history for a device
    /// </summary>
    Task<List<SyncLog>> GetSyncHistoryAsync(
        string deviceId,
        string userId,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Get sync statistics for a device
    /// </summary>
    Task<(int total, int failed)> GetSyncStatsAsync(
        string deviceId,
        string userId,
        CancellationToken ct = default);
}

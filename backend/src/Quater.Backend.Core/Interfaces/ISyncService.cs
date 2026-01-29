using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Service for orchestrating bidirectional synchronization
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Push changes from client to server
    /// </summary>
    Task<SyncResponse> PushAsync(
        SyncPushRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Pull changes from server to client
    /// </summary>
    Task<SyncResponse> PullAsync(
        SyncPullRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get sync status for a device
    /// </summary>
    Task<SyncStatusResponse> GetStatusAsync(
        string deviceId,
        string userId,
        CancellationToken ct = default);
}

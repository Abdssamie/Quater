namespace Quater.Shared.Enums;

/// <summary>
/// Synchronization status of an entity between desktop and backend.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Entity has not been synchronized yet (newly created locally)
    /// </summary>
    Pending,

    /// <summary>
    /// Entity is currently being synchronized
    /// </summary>
    InProgress,

    /// <summary>
    /// Entity has been successfully synchronized
    /// </summary>
    Synced,

    /// <summary>
    /// Synchronization failed - requires retry or manual intervention
    /// </summary>
    Failed,

    /// <summary>
    /// Conflict detected during synchronization - requires resolution
    /// </summary>
    Conflict
}

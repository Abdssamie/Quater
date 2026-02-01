namespace Quater.Shared.Interfaces;

/// <summary>
/// Interface for entities that support offline synchronization.
/// Tracks synchronization state for offline-first scenarios.
/// </summary>
public interface ISyncable
{
    /// <summary>
    /// Gets or sets the date and time when the entity was last synchronized.
    /// </summary>
    DateTime LastSyncedAt { get; init; }

    /// <summary>
    /// Gets or sets the synchronization version identifier.
    /// Used to track and resolve conflicts during synchronization.
    /// </summary>
    string? SyncVersion { get; set; }
}

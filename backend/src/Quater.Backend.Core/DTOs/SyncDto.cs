using Quater.Shared.Enums;

namespace Quater.Backend.Core.DTOs;

/// <summary>
/// DTO for sync push request from client
/// </summary>
public record SyncPushRequest
{
    public string DeviceId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime LastSyncTimestamp { get; init; }
    public List<SyncEntityData> Entities { get; init; } = new();
}

/// <summary>
/// DTO for sync pull request from client
/// </summary>
public record SyncPullRequest
{
    public string DeviceId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime LastSyncTimestamp { get; init; }
}

/// <summary>
/// DTO for sync response
/// </summary>
public record SyncResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime ServerTimestamp { get; init; }
    public int RecordsSynced { get; init; }
    public int ConflictsDetected { get; init; }
    public int ConflictsResolved { get; init; }
    public List<SyncEntityData> Entities { get; init; } = new();
    public List<ConflictInfo> Conflicts { get; init; } = new();
}

/// <summary>
/// DTO for sync status
/// </summary>
public record SyncStatusResponse
{
    public string DeviceId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime LastSyncTimestamp { get; init; }
    public SyncStatus Status { get; init; } = SyncStatus.Pending;
    public int TotalSyncs { get; init; }
    public int FailedSyncs { get; init; }
    public int PendingConflicts { get; init; }
}

/// <summary>
/// DTO for entity data in sync operations
/// </summary>
public record SyncEntityData
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty; // JSON serialized entity
    public DateTime LastModified { get; init; }
    public bool IsDeleted { get; init; }
}

/// <summary>
/// DTO for conflict information
/// </summary>
public record ConflictInfo
{
    public Guid EntityId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public ConflictResolutionStrategy Strategy { get; init; }
    public string ServerVersion { get; init; } = string.Empty;
    public string ClientVersion { get; init; } = string.Empty;
    public DateTime ConflictDetectedAt { get; init; }
}

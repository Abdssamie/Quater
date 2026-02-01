using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Tracks synchronization between clients and server.
/// </summary>
public class SyncLog : IEntity, ISyncable
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Unique device identifier
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>
    /// Foreign key to User
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string UserId { get; init; }

    /// <summary>
    /// UTC timestamp of last successful sync
    /// </summary>
    [Required]
    public DateTime LastSyncTimestamp { get; set; }

    /// <summary>
    /// Sync status
    /// </summary>
    [Required]
    public SyncStatus Status { get; set; } = SyncStatus.Pending;

    /// <summary>
    /// Error details if sync failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of records synced
    /// </summary>
    [Required]
    public int RecordsSynced { get; set; }

    /// <summary>
    /// Number of conflicts detected
    /// </summary>
    [Required]
    public int ConflictsDetected { get; set; }

    /// <summary>
    /// Number of conflicts resolved
    /// </summary>
    [Required]
    public int ConflictsResolved { get; set; }

    /// <summary>
    /// UTC timestamp of sync attempt
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; }

    // ISyncable interface properties
    public DateTime LastSyncedAt { get; init; }
    public string? SyncVersion { get; set; }

    // Navigation properties
    public User User { get; init; } = null!;
}

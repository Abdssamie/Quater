using System.ComponentModel.DataAnnotations;

namespace Quater.Desktop.Data.Models;

/// <summary>
/// Tracks synchronization between clients and server.
/// Desktop/offline version with same schema as backend SyncLog entity.
///
/// ⚠️ IMPORTANT: MODEL SYNCHRONIZATION REQUIRED ⚠️
/// This model is duplicated in 3 locations:
/// 1. Backend: backend/src/Quater.Backend.Core/Models/SyncLog.cs (master)
/// 2. Desktop: desktop/src/Quater.Desktop.Data/Models/SyncLog.cs (THIS FILE)
/// 3. Mobile: mobile/src/models/SyncLog.ts (TypeScript - to be generated from API)
///
/// When backend SyncLog.cs changes:
/// - Copy schema changes to this file
/// - Keep all fields identical (names, types, attributes)
/// - Update QuaterLocalContext.cs entity configuration if needed
/// - Run migration: dotnet ef migrations add [MigrationName] --project desktop/src/Quater.Desktop.Data
///
/// TODO (Phase 3): Refactor to use shared models project to eliminate duplication
/// </summary>
public class SyncLog
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
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to User
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of last successful sync
    /// </summary>
    [Required]
    public DateTime LastSyncTimestamp { get; set; }

    /// <summary>
    /// Sync status: "success", "failed", "in_progress"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error details if sync failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of records synced
    /// </summary>
    [Required]
    public int RecordsSynced { get; set; } = 0;

    /// <summary>
    /// Number of conflicts detected
    /// </summary>
    [Required]
    public int ConflictsDetected { get; set; } = 0;

    /// <summary>
    /// Number of conflicts resolved
    /// </summary>
    [Required]
    public int ConflictsResolved { get; set; } = 0;

    /// <summary>
    /// UTC timestamp of sync attempt
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; }
}

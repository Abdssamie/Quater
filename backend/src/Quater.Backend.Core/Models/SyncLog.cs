using System.ComponentModel.DataAnnotations;

namespace Quater.Backend.Core.Models;

/// <summary>
/// Tracks synchronization between clients and server.
/// 
/// ⚠️ IMPORTANT: MODEL SYNCHRONIZATION REQUIRED ⚠️
/// This model is duplicated in 3 locations:
/// 1. Backend: backend/src/Quater.Backend.Core/Models/SyncLog.cs (THIS FILE)
/// 2. Desktop: desktop/src/Quater.Desktop.Data/Models/SyncLog.cs (same schema)
/// 3. Mobile: mobile/src/models/SyncLog.ts (TypeScript - to be generated from API)
/// 
/// When modifying this model:
/// - Update desktop/src/Quater.Desktop.Data/Models/SyncLog.cs with same schema
/// - Regenerate mobile TypeScript types from OpenAPI/Swagger after backend changes
/// - Update QuaterDbContext.cs entity configuration if relationships change
/// - Update QuaterLocalContext.cs entity configuration for desktop
/// - Run migrations: dotnet ef migrations add [MigrationName] for both backend and desktop
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

    // Navigation properties
    public User User { get; set; } = null!;
}

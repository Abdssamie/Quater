using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Tracks all data modifications for compliance and conflict resolution.
/// </summary>
public class AuditLog : IEntity
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to User
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity modified (e.g., "Sample", "TestResult")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of modified entity
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Action performed on the entity
    /// </summary>
    [Required]
    public AuditAction Action { get; set; }

    /// <summary>
    /// JSON serialized old value (for updates). If data exceeds 4000 chars, stores truncation marker.
    /// </summary>
    [MaxLength(4000)]
    public string? OldValue { get; set; }

    /// <summary>
    /// JSON serialized new value. If data exceeds 4000 chars, stores truncation marker.
    /// </summary>
    [MaxLength(4000)]
    public string? NewValue { get; set; }

    /// <summary>
    /// Comma-separated list of field names that were changed (for quick filtering)
    /// </summary>
    [MaxLength(500)]
    public string? ChangedFields { get; set; }

    /// <summary>
    /// Flag indicating if OldValue/NewValue were truncated due to size limits
    /// </summary>
    [Required]
    public bool IsTruncated { get; set; } = false;

    /// <summary>
    /// Full data storage path (e.g., blob storage URL) if values were too large for database
    /// </summary>
    [MaxLength(500)]
    public string? OverflowStoragePath { get; set; }

    /// <summary>
    /// Foreign key to ConflictBackup (if this audit entry relates to a conflict resolution)
    /// </summary>
    public Guid? ConflictBackupId { get; set; }

    /// <summary>
    /// Notes when user resolves sync conflict
    /// </summary>
    [MaxLength(1000)]
    public string? ConflictResolutionNotes { get; set; }

    /// <summary>
    /// UTC timestamp of modification
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// IP address of client (IPv4/IPv6)
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Flag indicating if record is archived (for 90-day archival strategy)
    /// </summary>
    [Required]
    public bool IsArchived { get; set; } = false;

    // Navigation properties
    public User User { get; set; } = null!;
    public ConflictBackup? ConflictBackup { get; set; }
}

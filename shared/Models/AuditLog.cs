using System.ComponentModel.DataAnnotations;

namespace Quater.Shared.Models;

/// <summary>
/// Tracks all data modifications for compliance and conflict resolution.
/// </summary>
public class AuditLog
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
    /// Action performed: "create", "update", "delete"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized old value (for updates)
    /// </summary>
    [MaxLength(4000)]
    public string? OldValue { get; set; }

    /// <summary>
    /// JSON serialized new value
    /// </summary>
    [MaxLength(4000)]
    public string? NewValue { get; set; }

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
}

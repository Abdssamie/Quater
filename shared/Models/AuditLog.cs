using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Tracks all data modifications for compliance and conflict resolution.
/// </summary>
public sealed class AuditLog : IEntity
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
    public Guid UserId { get; init; } 

    /// <summary>
    /// Type of entity modified
    /// </summary>
    [Required]
    public EntityType EntityType { get; init; }

    /// <summary>
    /// ID of modified entity
    /// </summary>
    [Required]
    public Guid EntityId { get; init; }

    /// <summary>
    /// Action performed on the entity
    /// </summary>
    [Required]
    public AuditAction Action { get; init; }

    /// <summary>
    /// JSON serialized old value (for updates). Individual property values exceeding 50 chars are truncated.
    /// </summary>
    [MaxLength(4000)]
    public string? OldValue { get; init; }

    /// <summary>
    /// JSON serialized new value. Individual property values exceeding 50 chars are truncated.
    /// </summary>
    [MaxLength(4000)]
    public string? NewValue { get; init; }

    /// <summary>
    /// Flag indicating if any property values were truncated due to size limits (>50 chars)
    /// </summary>
    [Required]
    public bool IsTruncated { get; init; } 

    /// <summary>
    /// UTC timestamp of modification
    /// </summary>
    [Required]
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// IP address of client (IPv4/IPv6)
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; init; }

    /// <summary>
    /// Flag indicating if record is archived (for 90-day archival strategy)
    /// </summary>
    [Required]
    public bool IsArchived { get; set; } 

    /// <summary>
    /// UTC timestamp when this record becomes eligible for archival (Timestamp + 90 days)
    /// </summary>
    public DateTime ArchiveEligibleAt => Timestamp.AddDays(90);

    /// <summary>
    /// Whether this record is eligible for archival (older than 90 days and not yet archived)
    /// </summary>
    public bool IsEligibleForArchival => !IsArchived && DateTime.UtcNow >= ArchiveEligibleAt;

    // Navigation properties
    public User User { get; init; } = null!;
}

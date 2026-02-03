using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Archived audit logs older than 90 days (cold storage).
/// </summary>
public sealed class AuditLogArchive : IEntity
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
    /// UTC timestamp when record was archived
    /// </summary>
    [Required]
    public DateTime ArchivedDate { get; init; }

    // Navigation properties
    public User User { get; init; } = null!;
}

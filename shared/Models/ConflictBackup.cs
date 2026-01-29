using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Stores backup copies of conflicting records during synchronization.
/// Allows recovery and manual conflict resolution.
/// </summary>
public class ConflictBackup : IEntity, IAuditable
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the conflicting entity
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Type of entity (e.g., "Sample", "TestResult", "Parameter")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized backup of the server version
    /// </summary>
    [Required]
    public string ServerVersion { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized backup of the client version
    /// </summary>
    [Required]
    public string ClientVersion { get; set; } = string.Empty;

    /// <summary>
    /// Strategy used to resolve the conflict
    /// </summary>
    [Required]
    public ConflictResolutionStrategy ResolutionStrategy { get; set; }

    /// <summary>
    /// UTC timestamp when conflict was detected
    /// </summary>
    [Required]
    public DateTime ConflictDetectedAt { get; set; }

    /// <summary>
    /// UTC timestamp when conflict was resolved (null if unresolved)
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// User ID who resolved the conflict (for manual resolution)
    /// </summary>
    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Additional notes about the conflict resolution
    /// </summary>
    [MaxLength(1000)]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Device ID where conflict originated
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to Lab
    /// </summary>
    [Required]
    public Guid LabId { get; set; }

    /// <summary>
    /// UTC timestamp of creation
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; }

    // IAuditable interface properties
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public Lab Lab { get; set; } = null!;
}

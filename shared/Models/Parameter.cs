using System.ComponentModel.DataAnnotations;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a water quality parameter with compliance thresholds.
/// </summary>
public sealed class Parameter : IEntity, IAuditable, ISoftDelete, IConcurrent
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Parameter name (e.g., "pH", "turbidity")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measurement
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// WHO drinking water standard threshold
    /// </summary>
    public double? WhoThreshold { get; set; }

    /// <summary>
    /// Moroccan standard threshold (Phase 2)
    /// </summary>
    public double? MoroccanThreshold { get; set; }

    /// <summary>
    /// Minimum valid value
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// Maximum valid value
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>
    /// Parameter description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether parameter is currently used
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // IAuditable interface properties
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDelete interface properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ISyncable interface properties
    public DateTime LastSyncedAt { get; init; }
    public string? SyncVersion { get; set; }

    // IConcurrent interface properties
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a water quality parameter with compliance thresholds.
/// </summary>
public class Parameter
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

    /// <summary>
    /// UTC timestamp of creation
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// UTC timestamp of last modification
    /// </summary>
    [Required]
    public DateTime LastModified { get; set; }
}

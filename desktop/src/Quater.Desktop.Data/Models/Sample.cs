using System.ComponentModel.DataAnnotations;

namespace Quater.Desktop.Data.Models;

/// <summary>
/// Represents a water sample collected from a specific location at a specific time.
/// Desktop/offline version with same schema as backend Sample entity.
/// </summary>
public class Sample
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Sample type: "DrinkingWater", "Wastewater", "SurfaceWater", "Groundwater", "IndustrialWater"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// GPS latitude coordinate
    /// </summary>
    [Required]
    public double LocationLatitude { get; set; }

    /// <summary>
    /// GPS longitude coordinate
    /// </summary>
    [Required]
    public double LocationLongitude { get; set; }

    /// <summary>
    /// Human-readable location (e.g., "Municipal Well #3")
    /// </summary>
    [MaxLength(200)]
    public string? LocationDescription { get; set; }

    /// <summary>
    /// Hierarchical location path for reporting
    /// </summary>
    [MaxLength(500)]
    public string? LocationHierarchy { get; set; }

    /// <summary>
    /// UTC timestamp of sample collection
    /// </summary>
    [Required]
    public DateTime CollectionDate { get; set; }

    /// <summary>
    /// Name of technician who collected sample
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CollectorName { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes about sample
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Current status: "Pending", "Completed", "Archived"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Optimistic locking version number
    /// </summary>
    [Required]
    [ConcurrencyCheck]
    public int Version { get; set; }

    /// <summary>
    /// UTC timestamp of last modification
    /// </summary>
    [Required]
    [ConcurrencyCheck]
    public DateTime LastModified { get; set; }

    /// <summary>
    /// User ID who last modified
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// Soft delete flag for sync
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Sync status flag
    /// </summary>
    [Required]
    public bool IsSynced { get; set; } = false;

    /// <summary>
    /// Foreign key to Lab
    /// </summary>
    [Required]
    public Guid LabId { get; set; }

    /// <summary>
    /// User ID who created sample
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of creation
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}

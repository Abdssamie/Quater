using System.ComponentModel.DataAnnotations;
using Quater.Backend.Core.Enums;

namespace Quater.Backend.Core.Models;

/// <summary>
/// Represents a single water quality test performed on a sample.
/// </summary>
public class TestResult
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to Sample
    /// </summary>
    [Required]
    public Guid SampleId { get; set; }

    /// <summary>
    /// Water quality parameter (e.g., "pH", "turbidity")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// Measured value
    /// </summary>
    [Required]
    public double Value { get; set; }

    /// <summary>
    /// Unit of measurement (e.g., "mg/L", "NTU")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of test
    /// </summary>
    [Required]
    public DateTime TestDate { get; set; }

    /// <summary>
    /// Name of technician who performed test
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TechnicianName { get; set; } = string.Empty;

    /// <summary>
    /// Method used for testing
    /// </summary>
    [Required]
    public TestMethod TestMethod { get; set; }

    /// <summary>
    /// Compliance result
    /// </summary>
    [Required]
    public ComplianceStatus ComplianceStatus { get; set; }

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
    /// User ID who created result
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
    public Sample Sample { get; set; } = null!;
}

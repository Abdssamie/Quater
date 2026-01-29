using System.ComponentModel.DataAnnotations;

namespace Quater.Desktop.Data.Models;

/// <summary>
/// Represents a single water quality test performed on a sample.
/// Desktop/offline version with same schema as backend TestResult entity.
///
/// ⚠️ IMPORTANT: MODEL SYNCHRONIZATION REQUIRED ⚠️
/// This model is duplicated in 3 locations:
/// 1. Backend: backend/src/Quater.Backend.Core/Models/TestResult.cs (master - uses C# enums)
/// 2. Desktop: desktop/src/Quater.Desktop.Data/Models/TestResult.cs (THIS FILE - uses string enums)
/// 3. Mobile: mobile/src/models/TestResult.ts (TypeScript - to be generated from API)
///
/// When backend TestResult.cs changes:
/// - Copy schema changes to this file
/// - Convert C# enums (TestMethod, ComplianceStatus) to string properties
/// - Keep all other fields identical (names, types, attributes)
/// - Update QuaterLocalContext.cs entity configuration if needed
/// - Run migration: dotnet ef migrations add [MigrationName] --project desktop/src/Quater.Desktop.Data
///
/// Enum Mapping:
/// - Backend TestMethod enum → Desktop string: "Titration", "Spectrophotometry", etc.
/// - Backend ComplianceStatus enum → Desktop string: "pass", "fail", "warning"
///
/// TODO (Phase 3): Refactor to use shared models project to eliminate duplication
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
    /// Method used for testing: "Titration", "Spectrophotometry", "Chromatography", "Microscopy", "Electrode", "Culture", "Other"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TestMethod { get; set; } = string.Empty;

    /// <summary>
    /// Compliance result: "Pass", "Fail", "Warning"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ComplianceStatus { get; set; } = string.Empty;

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

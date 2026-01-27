using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Quater.Backend.Core.Enums;

namespace Quater.Backend.Core.Models;

/// <summary>
/// Represents a water sample collected from a specific location at a specific time.
/// 
/// ⚠️ IMPORTANT: MODEL SYNCHRONIZATION REQUIRED ⚠️
/// This model is duplicated in 3 locations:
/// 1. Backend: backend/src/Quater.Backend.Core/Models/Sample.cs (THIS FILE - uses C# enums)
/// 2. Desktop: desktop/src/Quater.Desktop.Data/Models/Sample.cs (uses string enums for SQLite)
/// 3. Mobile: mobile/src/models/Sample.ts (TypeScript - to be generated from API)
/// 
/// When modifying this model:
/// - Update desktop/src/Quater.Desktop.Data/Models/Sample.cs with same schema
/// - Convert C# enums to string properties for desktop (SQLite compatibility)
/// - Regenerate mobile TypeScript types from OpenAPI/Swagger after backend changes
/// - Update QuaterDbContext.cs entity configuration if relationships change
/// - Update QuaterLocalContext.cs entity configuration for desktop
/// - Run migrations: dotnet ef migrations add [MigrationName] for both backend and desktop
/// 
/// TODO (Phase 3): Refactor to use shared models project to eliminate duplication
/// </summary>
public class Sample
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Sample type
    /// </summary>
    [Required]
    public SampleType Type { get; set; }

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
    /// Current status
    /// </summary>
    [Required]
    public SampleStatus Status { get; set; }

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
    public Lab Lab { get; set; } = null!;
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}

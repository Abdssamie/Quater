using System.ComponentModel.DataAnnotations;

namespace Quater.Backend.Core.Models;

/// <summary>
/// Represents a water quality parameter with compliance thresholds.
///
/// ⚠️ IMPORTANT: MODEL SYNCHRONIZATION REQUIRED ⚠️
/// This model is duplicated in 3 locations:
/// 1. Backend: backend/src/Quater.Backend.Core/Models/Parameter.cs (THIS FILE)
/// 2. Desktop: desktop/src/Quater.Desktop.Data/Models/Parameter.cs (same schema)
/// 3. Mobile: mobile/src/models/Parameter.ts (TypeScript - to be generated from API)
///
/// When modifying this model:
/// - Update desktop/src/Quater.Desktop.Data/Models/Parameter.cs with same schema
/// - Regenerate mobile TypeScript types from OpenAPI/Swagger after backend changes
/// - Update QuaterDbContext.cs entity configuration if relationships change
/// - Update QuaterLocalContext.cs entity configuration for desktop
/// - Run migrations: dotnet ef migrations add [MigrationName] for both backend and desktop
///
/// TODO (Phase 3): Refactor to use shared models project to eliminate duplication
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

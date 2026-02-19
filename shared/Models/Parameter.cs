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
   public double? Threshold { get; set; }

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

   // IAuditable interface properties - Managed by AuditInterceptor
   public DateTime CreatedAt { get; private set; }
   public Guid CreatedBy { get; private set; }
   public DateTime? UpdatedAt { get; private set; }
   public Guid? UpdatedBy { get; private set; }

   // ISoftDelete interface properties
   public bool IsDeleted { get; private set; }
   public DateTime? DeletedAt { get; set; }
   public string? DeletedBy { get; set; }

   // IConcurrent interface properties
   [Timestamp]
   public byte[] RowVersion { get; set; } = null!;
}

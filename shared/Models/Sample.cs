using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;
using Quater.Shared.ValueObjects;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a water sample collected from a specific location at a specific time.
/// </summary>
public sealed class Sample : IEntity, IAuditable, ISoftDelete, IConcurrent
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
    /// Geographic location where sample was collected
    /// </summary>
    [Required]
    public Location Location { get; set; } = null!;

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
    /// Soft delete flag for sync
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Sync status flag
    /// </summary>
    [Required]
    public bool IsSynced { get; set; }

    /// <summary>
    /// Foreign key to Lab
    /// </summary>
    [Required]
    public Guid LabId { get; set; }

    // IAuditable interface properties
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDelete interface properties
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ISyncable interface properties
    public DateTime LastSyncedAt { get; init; }
    public string? SyncVersion { get; set; }

    // IConcurrent interface properties
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation properties
    public Lab Lab { get; set; } = null!;
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}

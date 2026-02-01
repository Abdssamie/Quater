using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;
using Quater.Shared.ValueObjects;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a single water quality test performed on a sample.
/// </summary>
public sealed class TestResult : IEntity, IAuditable, ISoftDelete, ISyncable, IConcurrent
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
    public Guid SampleId { get; init; }

    /// <summary>
    /// Measurement data (parameter, value, unit)
    /// </summary>
    [Required]
    public Measurement Measurement { get; set; } = null!;

    /// <summary>
    /// Status of the test result (Draft, Submitted, Voided)
    /// </summary>
    [Required]
    public TestResultStatus Status { get; set; } = TestResultStatus.Draft;

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
    /// If voided, reference to the TestResult that voided this one
    /// </summary>
    public Guid? VoidedTestResultId { get; set; }

    /// <summary>
    /// If voided, reference to the replacement TestResult
    /// </summary>
    public Guid? ReplacedByTestResultId { get; set; }

    /// <summary>
    /// Whether this result has been voided
    /// </summary>
    public bool IsVoided { get; set; } = false;

    /// <summary>
    /// Reason for voiding this result
    /// </summary>
    [MaxLength(500)]
    public string? VoidReason { get; set; }

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
    public Sample Sample { get; init; } = null!;
}

using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;

namespace Quater.Backend.Core.DTOs;

/// <summary>
/// Data Transfer Object for Sample entity
/// </summary>
public class SampleDto
{
    public Guid Id { get; set; }
    public SampleType Type { get; set; }
    public double LocationLatitude { get; set; }
    public double LocationLongitude { get; set; }
    public string? LocationDescription { get; set; }
    public string? LocationHierarchy { get; set; }
    public DateTime CollectionDate { get; set; }
    public string CollectorName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public SampleStatus Status { get; set; }
    public int Version { get; set; }
    public DateTime LastModified { get; set; }
    public Guid LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
    public Guid LabId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// DTO for creating a new sample
/// </summary>
public class CreateSampleDto
{
    [Required(ErrorMessage = "Sample type is required")]
    public SampleType Type { get; set; }

    [Required(ErrorMessage = "Location latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double LocationLatitude { get; set; }

    [Required(ErrorMessage = "Location longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double LocationLongitude { get; set; }

    [MaxLength(200, ErrorMessage = "Location description cannot exceed 200 characters")]
    public string? LocationDescription { get; set; }

    [MaxLength(500, ErrorMessage = "Location hierarchy cannot exceed 500 characters")]
    public string? LocationHierarchy { get; set; }

    [Required(ErrorMessage = "Collection date is required")]
    public DateTime CollectionDate { get; set; }

    [Required(ErrorMessage = "Collector name is required")]
    [MaxLength(100, ErrorMessage = "Collector name cannot exceed 100 characters")]
    public string CollectorName { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Lab ID is required")]
    public Guid LabId { get; set; }
}

/// <summary>
/// DTO for updating an existing sample
/// </summary>
public class UpdateSampleDto
{
    [Required(ErrorMessage = "Sample type is required")]
    public SampleType Type { get; set; }

    [Required(ErrorMessage = "Location latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double LocationLatitude { get; set; }

    [Required(ErrorMessage = "Location longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double LocationLongitude { get; set; }

    [MaxLength(200, ErrorMessage = "Location description cannot exceed 200 characters")]
    public string? LocationDescription { get; set; }

    [MaxLength(500, ErrorMessage = "Location hierarchy cannot exceed 500 characters")]
    public string? LocationHierarchy { get; set; }

    [Required(ErrorMessage = "Collection date is required")]
    public DateTime CollectionDate { get; set; }

    [Required(ErrorMessage = "Collector name is required")]
    [MaxLength(100, ErrorMessage = "Collector name cannot exceed 100 characters")]
    public string CollectorName { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Sample status is required")]
    public SampleStatus Status { get; set; }

    [Required(ErrorMessage = "Version is required")]
    public int Version { get; set; }
}

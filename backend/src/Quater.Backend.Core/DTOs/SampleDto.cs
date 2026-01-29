using Quater.Backend.Core.Enums;

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
    public string LastModifiedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
    public Guid LabId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// DTO for creating a new sample
/// </summary>
public class CreateSampleDto
{
    public SampleType Type { get; set; }
    public double LocationLatitude { get; set; }
    public double LocationLongitude { get; set; }
    public string? LocationDescription { get; set; }
    public string? LocationHierarchy { get; set; }
    public DateTime CollectionDate { get; set; }
    public string CollectorName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid LabId { get; set; }
}

/// <summary>
/// DTO for updating an existing sample
/// </summary>
public class UpdateSampleDto
{
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
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

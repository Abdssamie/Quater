using Quater.Backend.Core.DTOs;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for mapping between Sample entity and DTOs
/// </summary>
public static class SampleMappingExtensions
{
    /// <summary>
    /// Converts Sample entity to SampleDto
    /// </summary>
    public static SampleDto ToDto(this Sample sample)
    {
        return new SampleDto
        {
            Id = sample.Id,
            Type = sample.Type,
            LocationLatitude = sample.LocationLatitude,
            LocationLongitude = sample.LocationLongitude,
            LocationDescription = sample.LocationDescription,
            LocationHierarchy = sample.LocationHierarchy,
            CollectionDate = sample.CollectionDate,
            CollectorName = sample.CollectorName,
            Notes = sample.Notes,
            Status = sample.Status,
            Version = sample.Version,
            LastModified = sample.LastModified,
            LastModifiedBy = sample.LastModifiedBy,
            IsDeleted = sample.IsDeleted,
            IsSynced = sample.IsSynced,
            LabId = sample.LabId,
            CreatedBy = sample.CreatedBy,
            CreatedDate = sample.CreatedDate
        };
    }

    /// <summary>
    /// Converts CreateSampleDto to Sample entity
    /// </summary>
    public static Sample ToEntity(this CreateSampleDto dto, string createdBy)
    {
        var now = DateTime.UtcNow;
        return new Sample
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            LocationLatitude = dto.LocationLatitude,
            LocationLongitude = dto.LocationLongitude,
            LocationDescription = dto.LocationDescription,
            LocationHierarchy = dto.LocationHierarchy,
            CollectionDate = dto.CollectionDate,
            CollectorName = dto.CollectorName,
            Notes = dto.Notes,
            Status = SampleStatus.Pending,
            Version = 1,
            LastModified = now,
            LastModifiedBy = createdBy,
            IsDeleted = false,
            IsSynced = false,
            LabId = dto.LabId,
            CreatedBy = createdBy,
            CreatedDate = now,
            CreatedAt = now,
            LastSyncedAt = DateTime.MinValue
        };
    }

    /// <summary>
    /// Updates Sample entity from UpdateSampleDto
    /// </summary>
    public static void UpdateFromDto(this Sample sample, UpdateSampleDto dto, string updatedBy)
    {
        sample.Type = dto.Type;
        sample.LocationLatitude = dto.LocationLatitude;
        sample.LocationLongitude = dto.LocationLongitude;
        sample.LocationDescription = dto.LocationDescription;
        sample.LocationHierarchy = dto.LocationHierarchy;
        sample.CollectionDate = dto.CollectionDate;
        sample.CollectorName = dto.CollectorName;
        sample.Notes = dto.Notes;
        sample.Status = dto.Status;
        sample.Version = dto.Version;
        sample.LastModified = DateTime.UtcNow;
        sample.LastModifiedBy = updatedBy;
        sample.UpdatedAt = DateTime.UtcNow;
        sample.UpdatedBy = updatedBy;
        sample.IsSynced = false;
    }

    /// <summary>
    /// Converts collection of Sample entities to DTOs
    /// </summary>
    public static IEnumerable<SampleDto> ToDtos(this IEnumerable<Sample> samples)
    {
        return samples.Select(sample => sample.ToDto());
    }
}

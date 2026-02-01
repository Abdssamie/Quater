using Quater.Backend.Core.DTOs;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Quater.Shared.ValueObjects;

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
            LocationLatitude = sample.Location.Latitude,
            LocationLongitude = sample.Location.Longitude,
            LocationDescription = sample.Location.Description,
            LocationHierarchy = sample.Location.Hierarchy,
            CollectionDate = sample.CollectionDate,
            CollectorName = sample.CollectorName,
            Notes = sample.Notes,
            Status = sample.Status,
            Version = 1, // Version removed from model, using constant for backward compatibility
            LastModified = sample.UpdatedAt ?? sample.CreatedAt,
            LastModifiedBy = sample.UpdatedBy ?? sample.CreatedBy,
            IsDeleted = sample.IsDeleted,
            IsSynced = sample.IsSynced,
            LabId = sample.LabId,
            CreatedBy = sample.CreatedBy,
            CreatedDate = sample.CreatedAt
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
            Location = new Location(dto.LocationLatitude, dto.LocationLongitude, dto.LocationDescription, dto.LocationHierarchy),
            CollectionDate = dto.CollectionDate,
            CollectorName = dto.CollectorName,
            Notes = dto.Notes,
            Status = SampleStatus.Pending,
            IsDeleted = false,
            IsSynced = false,
            LabId = dto.LabId,
            CreatedBy = createdBy,
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
        sample.Location = new Location(dto.LocationLatitude, dto.LocationLongitude, dto.LocationDescription, dto.LocationHierarchy);
        sample.CollectionDate = dto.CollectionDate;
        sample.CollectorName = dto.CollectorName;
        sample.Notes = dto.Notes;
        sample.Status = dto.Status;
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

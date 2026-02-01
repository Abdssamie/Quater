using Quater.Backend.Core.DTOs;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for mapping between Lab entity and DTOs
/// </summary>
public static class LabMappingExtensions
{
    /// <summary>
    /// Converts Lab entity to LabDto
    /// </summary>
    public static LabDto ToDto(this Lab lab)
    {
        return new LabDto
        {
            Id = lab.Id,
            Name = lab.Name,
            Location = lab.Location,
            ContactInfo = lab.ContactInfo,
            CreatedDate = lab.CreatedAt,
            IsActive = lab.IsActive,
            CreatedAt = lab.CreatedAt,
            CreatedBy = lab.CreatedBy,
            UpdatedAt = lab.UpdatedAt,
            UpdatedBy = lab.UpdatedBy
        };
    }

    /// <summary>
    /// Converts CreateLabDto to Lab entity
    /// </summary>
    public static Lab ToEntity(this CreateLabDto dto, string createdBy)
    {
        var now = DateTime.UtcNow;
        return new Lab
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Location = dto.Location,
            ContactInfo = dto.ContactInfo,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = createdBy,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Updates Lab entity from UpdateLabDto
    /// </summary>
    public static void UpdateFromDto(this Lab lab, UpdateLabDto dto, string updatedBy)
    {
        lab.Name = dto.Name;
        lab.Location = dto.Location;
        lab.ContactInfo = dto.ContactInfo;
        lab.IsActive = dto.IsActive;
        lab.UpdatedAt = DateTime.UtcNow;
        lab.UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Converts collection of Lab entities to DTOs
    /// </summary>
    public static IEnumerable<LabDto> ToDtos(this IEnumerable<Lab> labs)
    {
        return labs.Select(lab => lab.ToDto());
    }
}

using Quater.Backend.Core.DTOs;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for mapping between Parameter entity and DTOs
/// </summary>
public static class ParameterMappingExtensions
{
    /// <summary>
    /// Converts Parameter entity to ParameterDto
    /// </summary>
    public static ParameterDto ToDto(this Parameter parameter)
    {
        return new ParameterDto
        {
            Id = parameter.Id,
            Name = parameter.Name,
            Unit = parameter.Unit,
            WhoThreshold = parameter.WhoThreshold,
            MoroccanThreshold = parameter.MoroccanThreshold,
            MinValue = parameter.MinValue,
            MaxValue = parameter.MaxValue,
            Description = parameter.Description,
            IsActive = parameter.IsActive,
            CreatedDate = parameter.CreatedDate,
            LastModified = parameter.LastModified
        };
    }

    /// <summary>
    /// Converts CreateParameterDto to Parameter entity
    /// </summary>
    public static Parameter ToEntity(this CreateParameterDto dto, string createdBy)
    {
        var now = DateTime.UtcNow;
        return new Parameter
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Unit = dto.Unit,
            WhoThreshold = dto.WhoThreshold,
            MoroccanThreshold = dto.MoroccanThreshold,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            Description = dto.Description,
            IsActive = true,
            CreatedDate = now,
            LastModified = now,
            CreatedAt = now,
            CreatedBy = createdBy,
            IsDeleted = false,
            LastSyncedAt = DateTime.MinValue
        };
    }

    /// <summary>
    /// Updates Parameter entity from UpdateParameterDto
    /// </summary>
    public static void UpdateFromDto(this Parameter parameter, UpdateParameterDto dto, string updatedBy)
    {
        parameter.Name = dto.Name;
        parameter.Unit = dto.Unit;
        parameter.WhoThreshold = dto.WhoThreshold;
        parameter.MoroccanThreshold = dto.MoroccanThreshold;
        parameter.MinValue = dto.MinValue;
        parameter.MaxValue = dto.MaxValue;
        parameter.Description = dto.Description;
        parameter.IsActive = dto.IsActive;
        parameter.LastModified = DateTime.UtcNow;
        parameter.UpdatedAt = DateTime.UtcNow;
        parameter.UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Converts collection of Parameter entities to DTOs
    /// </summary>
    public static IEnumerable<ParameterDto> ToDtos(this IEnumerable<Parameter> parameters)
    {
        return parameters.Select(parameter => parameter.ToDto());
    }
}

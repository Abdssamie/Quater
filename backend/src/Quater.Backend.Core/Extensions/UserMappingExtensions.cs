using Quater.Backend.Core.DTOs;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for mapping between User entity and DTOs
/// </summary>
public static class UserMappingExtensions
{
    /// <summary>
    /// Converts User entity to UserDto
    /// </summary>
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Role = user.Role,
            LabId = user.LabId,
            LabName = user.Lab?.Name,
            CreatedDate = user.CreatedAt,
            LastLogin = user.LastLogin,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            UpdatedAt = user.UpdatedAt,
            UpdatedBy = user.UpdatedBy
        };
    }

    /// <summary>
    /// Converts CreateUserDto to User entity
    /// </summary>
    public static User ToEntity(this CreateUserDto dto, string createdBy)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = dto.UserName,
            Email = dto.Email,
            Role = dto.Role,
            LabId = dto.LabId,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Updates User entity from UpdateUserDto
    /// </summary>
    public static void UpdateFromDto(this User user, UpdateUserDto dto, string updatedBy)
    {
        if (dto.UserName != null)
            user.UserName = dto.UserName;

        if (dto.Email != null)
            user.Email = dto.Email;

        if (dto.Role.HasValue)
            user.Role = dto.Role.Value;

        if (dto.LabId.HasValue)
            user.LabId = dto.LabId.Value;

        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Converts collection of User entities to DTOs
    /// </summary>
    public static IEnumerable<UserDto> ToDtos(this IEnumerable<User> users)
    {
        return users.Select(user => user.ToDto());
    }
}

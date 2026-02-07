using Quater.Backend.Core.DTOs;
using Quater.Shared.Models;
using Quater.Shared.Enums;

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
        // COMPATIBILITY: Map to the first lab found for legacy DTO support
        // TODO: Update UserDto to support multiple labs
        var primaryLab = user.UserLabs?.FirstOrDefault();

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Role = primaryLab?.Role ?? UserRole.Viewer, // Fallback if no lab assigned
            LabId = primaryLab?.LabId ?? Guid.Empty,    // Fallback if no lab assigned
            LabName = primaryLab?.Lab?.Name,            // Requires .Include(u => u.UserLabs).ThenInclude(ul => ul.Lab)
            LastLogin = user.LastLogin,
            IsActive = user.IsActive,
        };
    }

    /// <summary>
    /// Converts CreateUserDto to User entity
    /// </summary>
    public static User ToEntity(this CreateUserDto dto, Guid createdBy)
    {
        // Note: Role and LabId from DTO must be handled separately by creating a UserLab entity
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = dto.UserName,
            Email = dto.Email,
            IsActive = true,
        };
    }

    /// <summary>
    /// Updates User entity from UpdateUserDto
    /// </summary>
    public static void UpdateFromDto(this User user, UpdateUserDto dto, Guid updatedBy)
    {
        if (dto.UserName != null)
            user.UserName = dto.UserName;

        if (dto.Email != null)
            user.Email = dto.Email;

        // Note: Role and LabId updates must be handled via UserLab management
        
        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;
    }

    /// <summary>
    /// Converts collection of User entities to DTOs
    /// </summary>
    public static IEnumerable<UserDto> ToDtos(this IEnumerable<User> users)
    {
        return users.Select(user => user.ToDto());
    }
}

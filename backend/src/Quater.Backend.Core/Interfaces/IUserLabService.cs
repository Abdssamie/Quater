using Quater.Backend.Core.DTOs;
using Quater.Shared.Enums;

namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Service for managing user-lab memberships and roles.
/// </summary>
public interface IUserLabService
{
    /// <summary>
    /// Adds a user to a lab with the specified role.
    /// </summary>
    Task<UserLabDto> AddUserToLabAsync(Guid userId, Guid labId, UserRole role, CancellationToken ct = default);
    
    /// <summary>
    /// Removes a user from a lab.
    /// </summary>
    Task RemoveUserFromLabAsync(Guid userId, Guid labId, CancellationToken ct = default);
    
    /// <summary>
    /// Updates a user's role in a specific lab.
    /// </summary>
    Task<UserLabDto> UpdateUserRoleInLabAsync(Guid userId, Guid labId, UserRole newRole, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all users in a specific lab.
    /// </summary>
    Task<IEnumerable<UserDto>> GetUsersByLabAsync(Guid labId, CancellationToken ct = default);
}

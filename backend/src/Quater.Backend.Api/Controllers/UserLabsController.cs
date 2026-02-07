using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Enums;

namespace Quater.Backend.Api.Controllers;

// TODO: Implement user invitation feature
// - Generate secure invitation tokens with pre-assigned roles
// - Send invitation emails with registration links
// - Validate tokens during user registration
// - Expire tokens after use or timeout
// - Track invitation status (pending, accepted, expired)
// This will replace the removed self-registration endpoint with a secure invite-only system.

[ApiController]
[Route("api/users/{userId}/labs")]
[Authorize(Policy = Policies.AdminOnly)]
public class UserLabsController(IUserLabService userLabService) : ControllerBase
{
    /// <summary>
    /// Adds a user to a lab with the specified role.
    /// </summary>
    [HttpPost("{labId}")]
    [ProducesResponseType(typeof(UserLabDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddUserToLab(
        Guid userId,
        Guid labId,
        [FromBody] AddUserToLabRequest request)
    {
        var result = await userLabService.AddUserToLabAsync(userId, labId, request.Role, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetUserLabs), new { userId }, result);
    }

    /// <summary>
    /// Removes a user from a lab.
    /// </summary>
    [HttpDelete("{labId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserFromLab(Guid userId, Guid labId)
    {
        await userLabService.RemoveUserFromLabAsync(userId, labId, HttpContext.RequestAborted);
        return NoContent();
    }

    /// <summary>
    /// Updates a user's role in a specific lab.
    /// </summary>
    [HttpPut("{labId}/role")]
    [ProducesResponseType(typeof(UserLabDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoleInLab(
        Guid userId,
        Guid labId,
        [FromBody] UpdateUserRoleRequest request)
    {
        var result = await userLabService.UpdateUserRoleInLabAsync(userId, labId, request.Role, HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>
    /// Gets all labs a user belongs to (placeholder for consistency).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserLabDto>), StatusCodes.Status200OK)]
    public IActionResult GetUserLabs(Guid userId)
    {
        // This is handled by GET /api/users/{id} which returns user with all labs
        return Ok(new { message = "Use GET /api/users/{id} to get user with all labs" });
    }
}

/// <summary>
/// Request to add a user to a lab.
/// </summary>
public class AddUserToLabRequest
{
    public UserRole Role { get; set; }
}

/// <summary>
/// Request to update a user's role in a lab.
/// </summary>
public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}

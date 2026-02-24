using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Extensions;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Controller for managing users (administrative operations)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewerOrAbove)] // All endpoints require at least Viewer role
public class UsersController(IUserService userService, ILogger<UsersController> logger) : ControllerBase
{
    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await userService.GetAllAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get active users only (non-paginated, useful for dropdowns)
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetActive(CancellationToken ct = default)
    {
        var result = await userService.GetActiveAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Get users by lab ID with pagination
    /// </summary>
    [HttpGet("by_lab/{labId}")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetByLabId(
        Guid labId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await userService.GetByLabIdAsync(labId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get current authenticated user's profile
    /// </summary>
    /// <remarks>
    /// This endpoint only requires authentication (not lab membership) because:
    /// 1. Users need to fetch their profile before selecting a lab
    /// 2. The profile contains the list of labs the user has access to
    /// </remarks>
    [HttpGet("me")]
    [Authorize] // Override class-level policy - only authentication required, not lab membership
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken ct = default)
    {
        Console.WriteLine("[UsersController.GetCurrentUser] Request received");
        logger.LogInformation("[UsersController.GetCurrentUser] Request received");
        
        var userId = User.GetUserIdOrThrow();
        Console.WriteLine($"[UsersController.GetCurrentUser] Extracted userId: {userId}");
        logger.LogInformation("[UsersController.GetCurrentUser] Extracted userId: {UserId}", userId);
        
        try
        {
            var user = await userService.GetByIdAsync(userId, ct);
            logger.LogInformation("[UsersController.GetCurrentUser] User found: {UserName}", user.UserName);
            return Ok(user);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning("[UsersController.GetCurrentUser] User not found for userId: {UserId}. Message: {Message}", userId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[UsersController.GetCurrentUser] Error fetching user for userId: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var user = await userService.GetByIdAsync(id, ct);
        return Ok(user);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can create users
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();

            var created = await userService.CreateAsync(dto, userId, ct);
            var primaryLab = created.Labs.FirstOrDefault();
            logger.LogInformation(
                "User created successfully with ID {UserId}, Username: {UserName}, Role: {Role}, LabId: {LabId} by user {CreatedBy}",
                created.Id, created.UserName, primaryLab?.Role, primaryLab?.LabId, userId);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BadRequestException ex)
        {
            logger.LogWarning(ex, "Bad request when creating user {UserName}", dto.UserName);
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Lab not found when creating user {UserName} for lab {LabId}", dto.UserName, dto.LabId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can update users
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        [FromBody] UpdateUserDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();

            var updated = await userService.UpdateAsync(id, dto, userId, ct);
            logger.LogInformation("User {UserId} updated successfully by user {UpdatedBy}", id, userId);
            return Ok(updated);
        }
        catch (BadRequestException ex)
        {
            logger.LogWarning(ex, "Bad request when updating user {UserId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found when updating user {UserId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict when updating user {UserId}", id);
            return Conflict(new { message = "The user was modified by another user. Please refresh and try again." });
        }
    }

    /// <summary>
    /// Delete a user (soft delete - marks as inactive)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can delete users
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await userService.DeleteAsync(id, ct);
        logger.LogInformation("User {UserId} deactivated successfully", id);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Api.Attributes;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Controller for managing user invitations.
/// </summary>
[ApiController]
[Route("api/invitations")]
public sealed class UserInvitationsController(
    IUserInvitationService invitationService,
    ILogger<UserInvitationsController> logger) : ControllerBase
{
    /// <summary>
    /// Create a new user invitation.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(UserInvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserInvitationDto>> Create([FromBody] CreateUserInvitationDto dto)
    {
        var created = await invitationService.InviteUserAsync(dto, HttpContext.RequestAborted);
        logger.LogInformation("Invitation created for {Email}", created.Email);
        return CreatedAtAction(nameof(GetPendingInvitations), new { page = 1, pageSize = 20 }, created);
    }

    /// <summary>
    /// Get an invitation by token.
    /// </summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    [EndpointRateLimit(10, 60, RateLimitTrackBy.IpAddress)]
    [ProducesResponseType(typeof(UserInvitationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInvitationDto>> GetByToken(string token)
    {
        var invitation = await invitationService.GetByTokenAsync(token, HttpContext.RequestAborted);
        return Ok(invitation);
    }

    /// <summary>
    /// Accept an invitation and activate the user.
    /// </summary>
    [HttpPost("accept")]
    [AllowAnonymous]
    [EndpointRateLimit(5, 60, RateLimitTrackBy.IpAddress)]
    [ProducesResponseType(typeof(UserInvitationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInvitationDto>> Accept([FromBody] AcceptInvitationDto dto)
    {
        var invitation = await invitationService.AcceptInvitationAsync(dto, HttpContext.RequestAborted);
        return Ok(invitation);
    }

    /// <summary>
    /// Revoke a pending invitation.
    /// </summary>
    [HttpDelete("{invitationId}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid invitationId)
    {
        await invitationService.RevokeInvitationAsync(invitationId, HttpContext.RequestAborted);
        logger.LogInformation("Invitation {InvitationId} revoked", invitationId);
        return NoContent();
    }

    /// <summary>
    /// Get pending invitations with pagination.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(PagedResult<UserInvitationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserInvitationDto>>> GetPendingInvitations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await invitationService.GetPendingInvitationsAsync(page, pageSize, HttpContext.RequestAborted);
        return Ok(result);
    }
}

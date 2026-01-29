using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Controller for bidirectional synchronization between clients and server
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;

    public SyncController(ISyncService syncService)
    {
        _syncService = syncService;
    }

    /// <summary>
    /// Push changes from client to server
    /// </summary>
    [HttpPost("push")]
    [ProducesResponseType(typeof(SyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncResponse>> Push(
        [FromBody] SyncPushRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.DeviceId) || string.IsNullOrEmpty(request.UserId))
            return BadRequest(new { message = "DeviceId and UserId are required" });

        var response = await _syncService.PushAsync(request, ct);
        
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    /// Pull changes from server to client
    /// </summary>
    [HttpPost("pull")]
    [ProducesResponseType(typeof(SyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncResponse>> Pull(
        [FromBody] SyncPullRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.DeviceId) || string.IsNullOrEmpty(request.UserId))
            return BadRequest(new { message = "DeviceId and UserId are required" });

        var response = await _syncService.PullAsync(request, ct);
        
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    /// Get sync status for a device
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SyncStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncStatusResponse>> GetStatus(
        [FromQuery] string deviceId,
        [FromQuery] string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(userId))
            return BadRequest(new { message = "DeviceId and UserId are required" });

        var status = await _syncService.GetStatusAsync(deviceId, userId, ct);
        return Ok(status);
    }
}

using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Controller for bidirectional synchronization between clients and server
/// NOTE: This controller is disabled until ISyncService is implemented
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger until ISyncService is implemented
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
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

        _logger.LogInformation("Sync push initiated by device {DeviceId} for user {UserId}", request.DeviceId, request.UserId);
        var response = await _syncService.PushAsync(request, ct);
        
        if (!response.Success)
        {
            _logger.LogWarning("Sync push failed for device {DeviceId}, user {UserId}", 
                request.DeviceId, request.UserId);
            return BadRequest(response);
        }

        _logger.LogInformation("Sync push completed successfully for device {DeviceId}, user {UserId}", 
            request.DeviceId, request.UserId);
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

        _logger.LogInformation("Sync pull initiated by device {DeviceId} for user {UserId}", request.DeviceId, request.UserId);
        var response = await _syncService.PullAsync(request, ct);
        
        if (!response.Success)
        {
            _logger.LogWarning("Sync pull failed for device {DeviceId}, user {UserId}", 
                request.DeviceId, request.UserId);
            return BadRequest(response);
        }

        _logger.LogInformation("Sync pull completed successfully for device {DeviceId}, user {UserId}", 
            request.DeviceId, request.UserId);
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

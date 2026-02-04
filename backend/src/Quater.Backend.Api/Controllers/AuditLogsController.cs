using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Enums;

namespace Quater.Backend.Api.Controllers;

/**
 * @id: audit-log-controller
 * @priority: high
 * @progress: 100
 * @deps: ["audit-log-service", "audit-log-dto"]
 * @spec: Read-only controller for viewing audit logs. Endpoints: GET /api/auditlogs (paginated), GET /api/auditlogs/entity/{entityId}, GET /api/auditlogs/user/{userId}, GET /api/auditlogs/filter. Admin-only access.
 * @skills: ["csharp", "dotnet", "aspnetcore"]
 */

/// <summary>
/// Controller for viewing audit logs (read-only, Admin only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.AdminOnly)] // All audit log endpoints require Admin role
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IAuditLogService auditLogService, ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all audit logs with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await _auditLogService.GetAllAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get audit log by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AuditLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var auditLog = await _auditLogService.GetByIdAsync(id, ct);
        if (auditLog == null)
            return NotFound(new { message = $"Audit log with ID {id} not found" });

        return Ok(auditLog);
    }

    /// <summary>
    /// Get audit logs by entity ID
    /// </summary>
    [HttpGet("by-entity/{entityId}")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetByEntity(
        Guid entityId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await _auditLogService.GetByEntityAsync(entityId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get audit logs by entity ID (DEPRECATED - use /by-entity/{entityId} instead)
    /// </summary>
    [HttpGet("entity/{entityId}")]
    [Obsolete("This endpoint is deprecated. Use GET /api/auditlogs/by-entity/{entityId} instead.")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetByEntityLegacy(
        Guid entityId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Redirect to new endpoint implementation
        return await GetByEntity(entityId, pageNumber, pageSize, ct);
    }

    /// <summary>
    /// Get audit logs by user ID
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetByUser(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await _auditLogService.GetByUserAsync(userId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get audit logs by user ID (DEPRECATED - use /by-user/{userId} instead)
    /// </summary>
    [HttpGet("user/{userId}")]
    [Obsolete("This endpoint is deprecated. Use GET /api/auditlogs/by-user/{userId} instead.")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetByUserLegacy(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Redirect to new endpoint implementation
        return await GetByUser(userId, pageNumber, pageSize, ct);
    }

    /// <summary>
    /// Get audit logs with advanced filtering
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetByFilter(
        [FromBody] AuditLogFilterDto filter,
        CancellationToken ct = default)
    {
        if (filter.PageNumber < 1 || filter.PageSize < 1 || filter.PageSize > 100)
            return BadRequest("Invalid pagination parameters");

        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate > filter.EndDate)
            return BadRequest("Start date must be before or equal to end date");

        var result = await _auditLogService.GetByFilterAsync(filter, ct);
        _logger.LogInformation("Audit logs filtered: {Count} results returned", result.TotalCount);
        return Ok(result);
    }
}

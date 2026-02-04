using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Extensions;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewerOrAbove)] // All endpoints require at least Viewer role
public class LabsController(ILabService labService, ILogger<LabsController> logger) : ControllerBase
{
    /// <summary>
    /// Get all labs with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LabDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LabDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await labService.GetAllAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get active labs only
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<LabDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LabDto>>> GetActive(CancellationToken ct = default)
    {
        var result = await labService.GetActiveAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Get lab by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LabDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var lab = await labService.GetByIdAsync(id, ct);
        if (lab == null)
            return NotFound(new { message = $"Lab with ID {id} not found" });

        return Ok(lab);
    }

    /// <summary>
    /// Create a new lab
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can create labs
    [ProducesResponseType(typeof(LabDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LabDto>> Create(
        [FromBody] CreateLabDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();

            var created = await labService.CreateAsync(dto, userId, ct);
            logger.LogInformation("Lab created successfully with ID {LabId}, Name: {LabName} by user {UserId}",
                created.Id, created.Name, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation when creating lab {LabName}", dto.Name);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing lab
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can update labs
    [ProducesResponseType(typeof(LabDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabDto>> Update(
        Guid id,
        [FromBody] UpdateLabDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();

            var updated = await labService.UpdateAsync(id, dto, userId, ct);
            if (updated == null)
            {
                logger.LogWarning("Attempt to update non-existent lab {LabId}", id);
                return NotFound(new { message = $"Lab with ID {id} not found" });
            }

            logger.LogInformation("Lab {LabId} updated successfully by user {UserId}", id, userId);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation when updating lab {LabId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a lab (soft delete - marks as inactive)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can delete labs
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await labService.DeleteAsync(id, ct);
        if (!deleted)
        {
            logger.LogWarning("Attempt to delete non-existent lab {LabId}", id);
            return NotFound(new { message = $"Lab with ID {id} not found" });
        }

        logger.LogInformation("Lab {LabId} deleted successfully", id);
        return NoContent();
    }
}

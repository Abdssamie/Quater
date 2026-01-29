using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabsController(ILabService labService) : ControllerBase
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
    [ProducesResponseType(typeof(LabDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LabDto>> Create(
        [FromBody] CreateLabDto dto,
        CancellationToken ct = default)
    {
        try
        {
            // TODO: Get actual user ID from authentication context
            var userId = "system"; // Placeholder until auth is implemented
            
            var created = await labService.CreateAsync(dto, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing lab
    /// </summary>
    [HttpPut("{id}")]
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
            // TODO: Get actual user ID from authentication context
            var userId = "system"; // Placeholder until auth is implemented
            
            var updated = await labService.UpdateAsync(id, dto, userId, ct);
            if (updated == null)
                return NotFound(new { message = $"Lab with ID {id} not found" });

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a lab (soft delete - marks as inactive)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await labService.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound(new { message = $"Lab with ID {id} not found" });

        return NoContent();
    }
}

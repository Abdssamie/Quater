using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Extensions;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewerOrAbove)] // All endpoints require at least Viewer role
public class SamplesController(ISampleService sampleService, ILogger<SamplesController> logger) : ControllerBase
{
    /// <summary>
    /// Get all samples with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SampleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SampleDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await sampleService.GetAllAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get samples by lab ID with pagination
    /// </summary>
    [HttpGet("by-lab/{labId}")]
    [ProducesResponseType(typeof(PagedResult<SampleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<SampleDto>>> GetByLabId(
        Guid labId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await sampleService.GetByLabIdAsync(labId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get samples by lab ID with pagination (DEPRECATED - use /by-lab/{labId} instead)
    /// </summary>
    [HttpGet("lab/{labId}")]
    [Obsolete("This endpoint is deprecated. Use GET /api/samples/by-lab/{labId} instead.")]
    [ProducesResponseType(typeof(PagedResult<SampleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<SampleDto>>> GetByLabIdLegacy(
        Guid labId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Redirect to new endpoint implementation
        return await GetByLabId(labId, pageNumber, pageSize, ct);
    }

    /// <summary>
    /// Get sample by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SampleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SampleDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var sample = await sampleService.GetByIdAsync(id, ct);
        if (sample == null)
            return NotFound(new { message = $"Sample with ID {id} not found" });

        return Ok(sample);
    }

    /// <summary>
    /// Create a new sample
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.TechnicianOrAbove)] // Only Technician and Admin can create
    [ProducesResponseType(typeof(SampleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SampleDto>> Create(
        [FromBody] CreateSampleDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();

            var created = await sampleService.CreateAsync(dto, userId, ct);
            logger.LogInformation("Sample created successfully with ID {SampleId} by user {UserId}", created.Id, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed when creating sample for lab {LabId}", dto.LabId);
            return BadRequest(new { message = "Validation failed", errors = ex.Errors });
        }
    }

    /// <summary>
    /// Update an existing sample
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.TechnicianOrAbove)] // Only Technician and Admin can update
    [ProducesResponseType(typeof(SampleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SampleDto>> Update(
        Guid id,
        [FromBody] UpdateSampleDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();

            var updated = await sampleService.UpdateAsync(id, dto, userId, ct);
            if (updated == null)
            {
                logger.LogWarning("Attempt to update non-existent sample {SampleId}", id);
                return NotFound(new { message = $"Sample with ID {id} not found" });
            }

            logger.LogInformation("Sample {SampleId} updated successfully by user {UserId}", id, userId);
            return Ok(updated);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed when updating sample {SampleId}", id);
            return BadRequest(new { message = "Validation failed", errors = ex.Errors });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict when updating sample {SampleId}", id);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a sample (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can delete
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await sampleService.DeleteAsync(id, ct);
        if (!deleted)
        {
            logger.LogWarning("Attempt to delete non-existent sample {SampleId}", id);
            return NotFound(new { message = $"Sample with ID {id} not found" });
        }

        logger.LogInformation("Sample {SampleId} deleted successfully", id);
        return NoContent();
    }
}

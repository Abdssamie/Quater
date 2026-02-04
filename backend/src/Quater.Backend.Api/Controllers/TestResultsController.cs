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
public class TestResultsController(ITestResultService testResultService, ILogger<TestResultsController> logger) : ControllerBase
{
    /// <summary>
    /// Get all test results with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TestResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TestResultDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await testResultService.GetAllAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get test results by sample ID with pagination
    /// </summary>
    [HttpGet("by-sample/{sampleId}")]
    [ProducesResponseType(typeof(PagedResult<TestResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TestResultDto>>> GetBySampleId(
        Guid sampleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await testResultService.GetBySampleIdAsync(sampleId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get test results by sample ID with pagination (DEPRECATED - use /by-sample/{sampleId} instead)
    /// </summary>
    [HttpGet("sample/{sampleId}")]
    [Obsolete("This endpoint is deprecated. Use GET /api/testresults/by-sample/{sampleId} instead.")]
    [ProducesResponseType(typeof(PagedResult<TestResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TestResultDto>>> GetBySampleIdLegacy(
        Guid sampleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Redirect to new endpoint implementation
        return await GetBySampleId(sampleId, pageNumber, pageSize, ct);
    }

    /// <summary>
    /// Get test result by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestResultDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var testResult = await testResultService.GetByIdAsync(id, ct);
        if (testResult == null)
            return NotFound(new { message = $"Test result with ID {id} not found" });
        
        return Ok(testResult);
    }

    /// <summary>
    /// Create a new test result
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.TechnicianOrAbove)] // Only Technician and Admin can create
    [ProducesResponseType(typeof(TestResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestResultDto>> Create(
        [FromBody] CreateTestResultDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            
            var created = await testResultService.CreateAsync(dto, userId, ct);
            logger.LogInformation("Test result created successfully with ID {TestResultId} for sample {SampleId} by user {UserId}", 
                created.Id, dto.SampleId, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed when creating test result for sample {SampleId}", dto.SampleId);
            return BadRequest(new { message = "Validation failed", errors = ex.Errors });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation when creating test result for sample {SampleId}", dto.SampleId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing test result
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.TechnicianOrAbove)] // Only Technician and Admin can update
    [ProducesResponseType(typeof(TestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TestResultDto>> Update(
        Guid id,
        [FromBody] UpdateTestResultDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.GetUserIdOrThrow();
            
            var updated = await testResultService.UpdateAsync(id, dto, userId, ct);
            if (updated == null)
            {
                logger.LogWarning("Attempt to update non-existent test result {TestResultId}", id);
                return NotFound(new { message = $"Test result with ID {id} not found" });
            }

            logger.LogInformation("Test result {TestResultId} updated successfully by user {UserId}", id, userId);
            return Ok(updated);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed when updating test result {TestResultId}", id);
            return BadRequest(new { message = "Validation failed", errors = ex.Errors });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict when updating test result {TestResultId}", id);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a test result (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can delete
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await testResultService.DeleteAsync(id, ct);
        if (!deleted)
        {
            logger.LogWarning("Attempt to delete non-existent test result {TestResultId}", id);
            return NotFound(new { message = $"Test result with ID {id} not found" });
        }

        logger.LogInformation("Test result {TestResultId} deleted successfully", id);
        return NoContent();
    }
}

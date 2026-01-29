using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestResultsController(ITestResultService testResultService) : ControllerBase
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
    [HttpGet("sample/{sampleId}")]
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
    [ProducesResponseType(typeof(TestResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestResultDto>> Create(
        [FromBody] CreateTestResultDto dto,
        CancellationToken ct = default)
    {
        try
        {
            // TODO: Get actual user ID from authentication context
            var userId = "system";
            
            var created = await testResultService.CreateAsync(dto, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = "Validation failed", errors = ex.Errors });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing test result
    /// </summary>
    [HttpPut("{id}")]
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
            // TODO: Get actual user ID from authentication context
            var userId = "system";
            
            var updated = await testResultService.UpdateAsync(id, dto, userId, ct);
            if (updated == null)
                return NotFound(new { message = $"Test result with ID {id} not found" });

            return Ok(updated);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = "Validation failed", errors = ex.Errors });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a test result (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await testResultService.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound(new { message = $"Test result with ID {id} not found" });

        return NoContent();
    }
}

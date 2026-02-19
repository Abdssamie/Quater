using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Controller for managing test parameters.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewerOrAbove)] // All endpoints require at least Viewer role
public partial class ParametersController(IParameterService parameterService, ILogger<ParametersController> logger) : ControllerBase
{
    /// <summary>
    /// Get all parameters with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ParameterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ParameterDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Invalid pagination parameters");

        var result = await parameterService.GetAllAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get active parameters only
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<ParameterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ParameterDto>>> GetActive(CancellationToken ct = default)
    {
        var result = await parameterService.GetActiveAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Get parameter by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ParameterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParameterDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var parameter = await parameterService.GetByIdAsync(id, ct);
        return Ok(parameter);
    }

    /// <summary>
    /// Create a new parameter
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can create parameters
    [ProducesResponseType(typeof(ParameterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ParameterDto>> Create(
        [FromBody] CreateParameterDto dto,
        CancellationToken ct = default)
    {
        var created = await parameterService.CreateAsync(dto, ct);
        logger.LogInformation("Parameter created successfully with ID {ParameterId}, Name: {ParameterName}", created.Id, created.Name);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update an existing parameter
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can update parameters
    [ProducesResponseType(typeof(ParameterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParameterDto>> Update(
        Guid id,
        [FromBody] UpdateParameterDto dto,
        CancellationToken ct = default)
    {
        var updated = await parameterService.UpdateAsync(id, dto, ct);
        logger.LogInformation("Parameter {ParameterId} updated successfully", id);
        return Ok(updated);
    }

    /// <summary>
    /// Delete a parameter (soft delete - marks as inactive)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)] // Only Admin can delete parameters
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await parameterService.DeleteAsync(id, ct);
        logger.LogInformation("Parameter {ParameterId} deleted successfully", id);
        return NoContent();
    }
}

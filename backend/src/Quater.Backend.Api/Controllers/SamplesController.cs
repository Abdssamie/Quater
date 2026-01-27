using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Core.Models;

namespace Quater.Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SamplesController(ISampleService sampleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sample>>> GetAll(CancellationToken ct)
    {
        var samples = await sampleService.GetAllAsync(ct);
        return Ok(samples);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Sample>> GetById(Guid id, CancellationToken ct)
    {
        var sample = await sampleService.GetByIdAsync(id, ct);
        if (sample == null)
            return NotFound();
        
        return Ok(sample);
    }

    [HttpPost]
    public async Task<ActionResult<Sample>> Create([FromBody] Sample sample, CancellationToken ct)
    {
        var created = await sampleService.CreateAsync(sample, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Sample>> Update(Guid id, [FromBody] Sample sample, CancellationToken ct)
    {
        if (id != sample.Id)
            return BadRequest("ID mismatch");

        var updated = await sampleService.UpdateAsync(sample, ct);
        if (updated == null)
            return NotFound();

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await sampleService.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}

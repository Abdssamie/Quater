using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Core.Models;
using Quater.Backend.Data;

namespace Quater.Backend.Services;

public class SampleService(QuaterDbContext context, TimeProvider timeProvider) : ISampleService
{
    public async Task<Sample?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Samples
            .Include(s => s.TestResults)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IEnumerable<Sample>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Samples
            .AsNoTracking()
            .OrderByDescending(s => s.CollectionDate)
            .ToListAsync(ct);
    }

    public async Task<Sample> CreateAsync(Sample sample, CancellationToken ct = default)
    {
        sample.CreatedDate = timeProvider.GetUtcNow().DateTime;
        sample.LastModified = timeProvider.GetUtcNow().DateTime;
        
        context.Samples.Add(sample);
        await context.SaveChangesAsync(ct);
        return sample;
    }

    public async Task<Sample?> UpdateAsync(Sample sample, CancellationToken ct = default)
    {
        var existing = await context.Samples.FindAsync([sample.Id], ct);
        if (existing == null) return null;

        // Map updates (simplified for now)
        context.Entry(existing).CurrentValues.SetValues(sample);
        existing.LastModified = timeProvider.GetUtcNow().DateTime;

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sample = await context.Samples.FindAsync([id], ct);
        if (sample == null) return false;

        context.Samples.Remove(sample);
        await context.SaveChangesAsync(ct);
        return true;
    }
}

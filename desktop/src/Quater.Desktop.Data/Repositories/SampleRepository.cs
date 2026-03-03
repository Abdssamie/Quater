using Microsoft.EntityFrameworkCore;
using Quater.Shared.Models;
using Quater.Shared.Enums;

namespace Quater.Desktop.Data.Repositories;

public class SampleRepository(QuaterLocalContext context) : ISampleRepository
{
    public async Task<Sample?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Samples
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IEnumerable<Sample>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Samples
            .OrderByDescending(s => s.CollectionDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Sample>> GetFilteredAsync(
        SampleStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var query = context.Samples.AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (startDate.HasValue)
            query = query.Where(s => s.CollectionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.CollectionDate <= endDate.Value);

        return await query
            .OrderByDescending(s => s.CollectionDate)
            .ToListAsync(ct);
    }

    public async Task<Sample> CreateAsync(Sample sample, CancellationToken ct = default)
    {
        sample.Id = Guid.NewGuid();

        context.Samples.Add(sample);

        // Set shadow property for sync tracking
        context.Entry(sample).Property("IsSynced").CurrentValue = false;

        await context.SaveChangesAsync(ct);

        return sample;
    }

    public async Task<Sample> UpdateAsync(Sample sample, CancellationToken ct = default)
    {
        // Set shadow property to indicate needs sync
        context.Entry(sample).Property("IsSynced").CurrentValue = false;

        context.Samples.Update(sample);
        await context.SaveChangesAsync(ct);

        return sample;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sample = await GetByIdAsync(id, ct);
        if (sample == null)
            return false;

        // Soft delete: mark as deleted instead of physically removing the row.
        // DeletedBy is null because DeleteAsync has no caller-identity parameter.
        sample.MarkDeleted(deletedBy: null);

        // Set shadow property to indicate needs sync
        context.Entry(sample).Property("IsSynced").CurrentValue = false;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await context.Samples.CountAsync(ct);
    }
}

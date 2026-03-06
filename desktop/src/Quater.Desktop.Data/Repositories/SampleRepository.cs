using Microsoft.EntityFrameworkCore;
using Quater.Shared.Models;

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

    public async Task<IReadOnlyList<Sample>> GetFilteredAsync(SampleQuery query, CancellationToken ct = default)
    {
        var samplesQuery = context.Samples.AsQueryable();

        if (query.Status.HasValue)
            samplesQuery = samplesQuery.Where(sample => sample.Status == query.Status.Value);

        if (query.StartDate.HasValue)
            samplesQuery = samplesQuery.Where(sample => sample.CollectionDate >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            samplesQuery = samplesQuery.Where(sample => sample.CollectionDate <= query.EndDate.Value);

        if (query.LabId.HasValue)
            samplesQuery = samplesQuery.Where(sample => sample.LabId == query.LabId.Value);

        var searchText = query.SearchText.Trim();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchPattern = $"%{searchText}%";
            samplesQuery = samplesQuery.Where(sample =>
                EF.Functions.Like(sample.CollectorName, searchPattern) ||
                EF.Functions.Like(sample.Location.Description ?? string.Empty, searchPattern) ||
                EF.Functions.Like(sample.Notes ?? string.Empty, searchPattern));
        }

        return await samplesQuery
            .OrderByDescending(sample => sample.CollectionDate)
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

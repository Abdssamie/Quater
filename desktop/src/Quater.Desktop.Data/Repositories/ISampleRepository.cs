using Quater.Desktop.Data.Models;

namespace Quater.Desktop.Data.Repositories;

public interface ISampleRepository
{
    Task<Sample?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Sample>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Sample>> GetFilteredAsync(string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
    Task<Sample> CreateAsync(Sample sample, CancellationToken ct = default);
    Task<Sample> UpdateAsync(Sample sample, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
}

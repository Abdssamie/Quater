using Quater.Backend.Core.Models;

namespace Quater.Backend.Core.Interfaces;

public interface ISampleService
{
    Task<Sample?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Sample>> GetAllAsync(CancellationToken ct = default);
    Task<Sample> CreateAsync(Sample sample, CancellationToken ct = default);
    Task<Sample?> UpdateAsync(Sample sample, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

public interface ISampleService
{
    Task<SampleDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<SampleDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<PagedResult<SampleDto>> GetByLabIdAsync(Guid labId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<SampleDto> CreateAsync(CreateSampleDto dto, Guid userId, CancellationToken ct = default);
    Task<SampleDto?> UpdateAsync(Guid id, UpdateSampleDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

public interface ILabService
{
    Task<LabDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<LabDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IEnumerable<LabDto>> GetActiveAsync(CancellationToken ct = default);
    Task<LabDto> CreateAsync(CreateLabDto dto, Guid userId, CancellationToken ct = default);
    Task<LabDto?> UpdateAsync(Guid id, UpdateLabDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

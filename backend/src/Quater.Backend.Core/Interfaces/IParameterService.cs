using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

public interface IParameterService
{
    Task<ParameterDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ParameterDto> GetByNameAsync(string name, CancellationToken ct = default);
    Task<PagedResult<ParameterDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IEnumerable<ParameterDto>> GetActiveAsync(CancellationToken ct = default);
    Task<ParameterDto> CreateAsync(CreateParameterDto dto, CancellationToken ct = default);
    Task<ParameterDto> UpdateAsync(Guid id, UpdateParameterDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

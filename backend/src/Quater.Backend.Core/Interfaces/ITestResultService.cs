using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

public interface ITestResultService
{
    Task<TestResultDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<TestResultDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<PagedResult<TestResultDto>> GetBySampleIdAsync(Guid sampleId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<TestResultDto> CreateAsync(CreateTestResultDto dto, Guid userId, CancellationToken ct = default);
    Task<TestResultDto?> UpdateAsync(Guid id, UpdateTestResultDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

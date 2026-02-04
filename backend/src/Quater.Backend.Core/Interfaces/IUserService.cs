using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetByLabIdAsync(Guid labId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetActiveAsync(CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserDto dto, Guid createdBy, CancellationToken ct = default);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, Guid updatedBy, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto dto, CancellationToken ct = default);
}

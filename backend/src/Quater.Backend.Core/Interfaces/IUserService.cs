using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetByLabIdAsync(Guid labId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetActiveAsync(CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserDto dto, string createdBy, CancellationToken ct = default);
    Task<UserDto?> UpdateAsync(string id, UpdateUserDto dto, string updatedBy, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(string id, ChangePasswordDto dto, CancellationToken ct = default);
}

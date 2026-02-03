using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;
using Quater.Backend.Data;

namespace Quater.Backend.Services;

public class UserService(
    QuaterDbContext context,
    UserManager<User> userManager
    ) : IUserService
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Lab)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return user == null ? null : MapToDto(user);
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.Lab)
            .OrderBy(u => u.UserName);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<UserDto>> GetByLabIdAsync(Guid labId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.Lab)
            .Where(u => u.LabId == labId)
            .OrderBy(u => u.UserName);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<UserDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var users = await context.Users
            .AsNoTracking()
            .Include(u => u.Lab)
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);

        return users.Select(MapToDto);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, Guid createdBy, CancellationToken ct = default)
    {
        // Verify lab exists
        var labExists = await context.Labs.AnyAsync(l => l.Id == dto.LabId && !l.IsDeleted, ct);
        if (!labExists)
            throw new NotFoundException(ErrorMessages.LabNotFound);

        var user = new User
        {
            UserName = dto.UserName,
            Email = dto.Email,
            Role = dto.Role,
            LabId = dto.LabId,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"{ErrorMessages.UserCreationFailed}: {errors}");
        }

        // Reload user with Lab navigation property
        var createdUser = await context.Users
            .Include(u => u.Lab)
            .FirstAsync(u => u.Id == user.Id, ct);

        return MapToDto(createdUser);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, Guid updatedBy, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return null;

        // Update fields if provided
        if (!string.IsNullOrEmpty(dto.UserName))
            user.UserName = dto.UserName;

        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;

        if (dto.Role.HasValue)
            user.Role = dto.Role.Value;

        if (dto.LabId.HasValue)
        {
            // Verify lab exists
            var labExists = await context.Labs.AnyAsync(l => l.Id == dto.LabId.Value && !l.IsDeleted, ct);
            if (!labExists)
                throw new NotFoundException(ErrorMessages.LabNotFound);

            user.LabId = dto.LabId.Value;
        }

        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"{ErrorMessages.UserUpdateFailed}: {errors}");
        }

        // Reload user with Lab navigation property
        var updatedUser = await context.Users
            .Include(u => u.Lab)
            .FirstAsync(u => u.Id == user.Id, ct);

        return MapToDto(updatedUser);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return false;

        // Soft delete by marking as inactive
        user.IsActive = false;

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return false;

        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        return result.Succeeded;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        Role = user.Role,
        LabId = user.LabId,
        LabName = user.Lab.Name,
        LastLogin = user.LastLogin,
        IsActive = user.IsActive,
    };
}

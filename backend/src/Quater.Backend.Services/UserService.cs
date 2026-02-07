using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Extensions;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;
using Quater.Backend.Data;
using Quater.Shared.Enums;

namespace Quater.Backend.Services;

public class UserService(
    QuaterDbContext context,
    UserManager<User> userManager
    ) : IUserService
{
    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null)
            throw new NotFoundException(ErrorMessages.UserNotFound);

        return user.ToDto();
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .OrderBy(u => u.UserName);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items.Select(u => u.ToDto()),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<UserDto>> GetByLabIdAsync(Guid labId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .Where(u => u.UserLabs.Any(ul => ul.LabId == labId))
            .OrderBy(u => u.UserName);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items.Select(u => u.ToDto()),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<UserDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var users = await context.Users
            .AsNoTracking()
            .Include(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserName)
            .Take(1000)
            .ToListAsync(ct);

        return users.Select(u => u.ToDto());
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
            IsActive = true,
            UserLabs = 
            [
                new UserLab 
                { 
                    LabId = dto.LabId,
                    Role = dto.Role
                }
            ]
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"{ErrorMessages.UserCreationFailed}: {errors}");
        }

        // Reload user with Lab navigation property
        var createdUser = await context.Users
            .Include(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .FirstAsync(u => u.Id == user.Id, ct);

        return createdUser.ToDto();
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto, Guid updatedBy, CancellationToken ct = default)
    {
        // Load with UserLabs to handle role/lab updates
        var user = await context.Users
            .Include(u => u.UserLabs)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null)
            throw new NotFoundException(ErrorMessages.UserNotFound);

        // Update fields if provided
        if (!string.IsNullOrEmpty(dto.UserName))
            user.UserName = dto.UserName;

        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;
        
        // Handle Role and Lab updates (Legacy single-lab support)
        var userLab = user.UserLabs.FirstOrDefault();

        // 1. If LabId is changing
        if (dto.LabId.HasValue)
        {
             // Verify lab exists
            var labExists = await context.Labs.AnyAsync(l => l.Id == dto.LabId.Value && !l.IsDeleted, ct);
            if (!labExists)
                throw new NotFoundException(ErrorMessages.LabNotFound);
                
            if (userLab != null)
            {
                // If switching labs, remove old and add new
                if (userLab.LabId != dto.LabId.Value)
                {
                    // Keep the role unless explicitly changed
                    var role = dto.Role ?? userLab.Role;
                    context.UserLabs.Remove(userLab);
                    user.UserLabs.Add(new UserLab { LabId = dto.LabId.Value, Role = role });
                }
                else if (dto.Role.HasValue)
                {
                    // Same lab, just update role
                    userLab.Role = dto.Role.Value;
                }
            }
            else
            {
                // No existing lab, add new one
                user.UserLabs.Add(new UserLab 
                { 
                    LabId = dto.LabId.Value, 
                    Role = dto.Role ?? UserRole.Viewer // Default if not provided
                });
            }
        }
        else if (dto.Role.HasValue && userLab != null)
        {
            // Only updating role on existing lab
            userLab.Role = dto.Role.Value;
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
            .Include(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .FirstAsync(u => u.Id == user.Id, ct);

        return updatedUser.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new NotFoundException(ErrorMessages.UserNotFound);

        // Soft delete by marking as inactive
        user.IsActive = false;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"{ErrorMessages.UserUpdateFailed}: {errors}");
        }
    }

    public async Task ChangePasswordAsync(Guid id, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new NotFoundException(ErrorMessages.UserNotFound);

        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"Password change failed: {errors}");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Extensions;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Services;

public class UserLabService(QuaterDbContext context) : IUserLabService
{
    public async Task<UserLabDto> AddUserToLabAsync(Guid userId, Guid labId, UserRole role, CancellationToken ct = default)
    {
        // Verify user exists
        var userExists = await context.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            throw new NotFoundException(ErrorMessages.UserNotFound);

        // Verify lab exists
        var lab = await context.Labs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == labId && !l.IsDeleted, ct);
        if (lab == null)
            throw new NotFoundException(ErrorMessages.LabNotFound);

        // Check if user is already a member
        var existingMembership = await context.UserLabs
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LabId == labId, ct);
        if (existingMembership != null)
            throw new ConflictException("User is already a member of this lab");

        // Add membership
        var userLab = new UserLab
        {
            UserId = userId,
            LabId = labId,
            Role = role,
            AssignedAt = DateTime.UtcNow
        };

        context.UserLabs.Add(userLab);
        await context.SaveChangesAsync(ct);

        return new UserLabDto
        {
            LabId = labId,
            LabName = lab.Name,
            Role = role,
            AssignedAt = userLab.AssignedAt
        };
    }

    public async Task RemoveUserFromLabAsync(Guid userId, Guid labId, CancellationToken ct = default)
    {
        var userLab = await context.UserLabs
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LabId == labId, ct);

        if (userLab == null)
            throw new NotFoundException("User is not a member of this lab");

        context.UserLabs.Remove(userLab);
        await context.SaveChangesAsync(ct);
    }

    public async Task<UserLabDto> UpdateUserRoleInLabAsync(Guid userId, Guid labId, UserRole newRole, CancellationToken ct = default)
    {
        var userLab = await context.UserLabs
            .Include(ul => ul.Lab)
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LabId == labId, ct);

        if (userLab == null)
            throw new NotFoundException("User is not a member of this lab");

        userLab.Role = newRole;
        await context.SaveChangesAsync(ct);

        return new UserLabDto
        {
            LabId = labId,
            LabName = userLab.Lab.Name,
            Role = newRole,
            AssignedAt = userLab.AssignedAt
        };
    }

    public async Task<IEnumerable<UserDto>> GetUsersByLabAsync(Guid labId, CancellationToken ct = default)
    {
        var users = await context.Users
            .AsNoTracking()
            .Include(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .Where(u => u.UserLabs.Any(ul => ul.LabId == labId))
            .ToListAsync(ct);

        return users.Select(u => u.ToDto());
    }

    public async Task<IEnumerable<UserLabDto>> AddUserToLabsAsync(
        Guid userId,
        IEnumerable<(Guid LabId, UserRole Role)> assignments,
        CancellationToken ct = default)
    {
        var assignmentList = assignments.ToList();
        if (assignmentList.Count == 0)
            return [];

        // Verify user exists
        var userExists = await context.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            throw new NotFoundException(ErrorMessages.UserNotFound);

        var labIds = assignmentList.Select(a => a.LabId).ToList();
        if (labIds.Distinct().Count() != labIds.Count)
            throw new BadRequestException("Duplicate lab assignments are not allowed");

        // Verify all labs exist
        var labs = await context.Labs
            .AsNoTracking()
            .Where(l => labIds.Contains(l.Id) && !l.IsDeleted)
            .ToDictionaryAsync(l => l.Id, ct);

        if (labs.Count != labIds.Distinct().Count())
            throw new NotFoundException(ErrorMessages.LabNotFound);

        // Check for existing memberships
        var existingLabIds = await context.UserLabs
            .Where(ul => ul.UserId == userId && labIds.Contains(ul.LabId))
            .Select(ul => ul.LabId)
            .ToListAsync(ct);

        if (existingLabIds.Count > 0)
            throw new ConflictException("User is already a member of one or more of these labs");

        var assignedAt = DateTime.UtcNow;
        var userLabs = assignmentList.Select(a => new UserLab
        {
            UserId = userId,
            LabId = a.LabId,
            Role = a.Role,
            AssignedAt = assignedAt
        }).ToList();

        context.UserLabs.AddRange(userLabs);
        await context.SaveChangesAsync(ct);

        return userLabs.Select(ul => new UserLabDto
        {
            LabId = ul.LabId,
            LabName = labs[ul.LabId].Name,
            Role = ul.Role,
            AssignedAt = ul.AssignedAt
        });
    }
}

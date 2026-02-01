using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;
using Quater.Backend.Data;

namespace Quater.Backend.Services;

public class LabService(
    QuaterDbContext context,
    TimeProvider timeProvider) : ILabService
{
    public async Task<LabDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lab = await context.Labs
            .AsNoTracking()
            .Where(l => l.Id == id && !l.IsDeleted)
            .FirstOrDefaultAsync(ct);

        return lab == null ? null : MapToDto(lab);
    }

    public async Task<PagedResult<LabDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.Labs
            .AsNoTracking()
            .Where(l => !l.IsDeleted)
            .OrderBy(l => l.Name);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<LabDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<LabDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var labs = await context.Labs
            .AsNoTracking()
            .Where(l => l.IsActive && !l.IsDeleted)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

        return labs.Select(MapToDto);
    }

    public async Task<LabDto> CreateAsync(CreateLabDto dto, string userId, CancellationToken ct = default)
    {
        // Check for duplicate name
        var exists = await context.Labs.AnyAsync(l => l.Name == dto.Name && !l.IsDeleted, ct);
        if (exists)
            throw new ConflictException(ErrorMessages.LabAlreadyExists);

        var now = timeProvider.GetUtcNow().DateTime;

        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Location = dto.Location,
            ContactInfo = dto.ContactInfo,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
            IsDeleted = false
        };

        context.Labs.Add(lab);
        await context.SaveChangesAsync(ct);

        return MapToDto(lab);
    }

    public async Task<LabDto?> UpdateAsync(Guid id, UpdateLabDto dto, string userId, CancellationToken ct = default)
    {
        var existing = await context.Labs.FindAsync([id], ct);
        if (existing == null || existing.IsDeleted)
            return null;

        // Check for duplicate name (excluding current lab)
        var duplicateExists = await context.Labs
            .AnyAsync(l => l.Name == dto.Name && l.Id != id && !l.IsDeleted, ct);
        if (duplicateExists)
            throw new ConflictException(ErrorMessages.LabAlreadyExists);

        var now = timeProvider.GetUtcNow().DateTime;

        existing.Name = dto.Name;
        existing.Location = dto.Location;
        existing.ContactInfo = dto.ContactInfo;
        existing.IsActive = dto.IsActive;
        existing.UpdatedAt = now;
        existing.UpdatedBy = userId;

        await context.SaveChangesAsync(ct);

        return MapToDto(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lab = await context.Labs.FindAsync([id], ct);
        if (lab == null || lab.IsDeleted)
            return false;

        // Soft delete
        lab.IsDeleted = true;
        lab.DeletedAt = timeProvider.GetUtcNow().DateTime;

        await context.SaveChangesAsync(ct);
        return true;
    }

    private static LabDto MapToDto(Lab lab) => new()
    {
        Id = lab.Id,
        Name = lab.Name,
        Location = lab.Location,
        ContactInfo = lab.ContactInfo,
        IsActive = lab.IsActive,
        CreatedAt = lab.CreatedAt,
        CreatedBy = lab.CreatedBy,
        UpdatedAt = lab.UpdatedAt,
        UpdatedBy = lab.UpdatedBy
    };
}

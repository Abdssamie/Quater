using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;
using Quater.Backend.Data;

namespace Quater.Backend.Services;

public class ParameterService(
    QuaterDbContext context,
    TimeProvider timeProvider) : IParameterService
{
    public async Task<ParameterDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var parameter = await context.Parameters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return parameter == null ? null : MapToDto(parameter);
    }

    public async Task<ParameterDto?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var parameter = await context.Parameters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == name, ct);

        return parameter == null ? null : MapToDto(parameter);
    }

    public async Task<PagedResult<ParameterDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.Parameters
            .AsNoTracking()
            .OrderBy(p => p.Name);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ParameterDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<ParameterDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var parameters = await context.Parameters
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return parameters.Select(MapToDto);
    }

    public async Task<ParameterDto> CreateAsync(CreateParameterDto dto, CancellationToken ct = default)
    {
        // Check for duplicate name
        var exists = await context.Parameters.AnyAsync(p => p.Name == dto.Name, ct);
        if (exists)
            throw new ConflictException(ErrorMessages.ParameterAlreadyExists);

        var now = timeProvider.GetUtcNow().DateTime;

        var parameter = new Parameter
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Unit = dto.Unit,
            WhoThreshold = dto.WhoThreshold,
            MoroccanThreshold = dto.MoroccanThreshold,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "system", // TODO: Get from current user context
            IsDeleted = false,
            LastSyncedAt = DateTime.MinValue
        };

        context.Parameters.Add(parameter);
        await context.SaveChangesAsync(ct);

        return MapToDto(parameter);
    }

    public async Task<ParameterDto?> UpdateAsync(Guid id, UpdateParameterDto dto, CancellationToken ct = default)
    {
        var existing = await context.Parameters.FindAsync([id], ct);
        if (existing == null)
            return null;

        // Check for duplicate name (excluding current parameter)
        var duplicateExists = await context.Parameters
            .AnyAsync(p => p.Name == dto.Name && p.Id != id, ct);
        if (duplicateExists)
            throw new ConflictException(ErrorMessages.ParameterAlreadyExists);

        var now = timeProvider.GetUtcNow().DateTime;

        existing.Name = dto.Name;
        existing.Unit = dto.Unit;
        existing.WhoThreshold = dto.WhoThreshold;
        existing.MoroccanThreshold = dto.MoroccanThreshold;
        existing.MinValue = dto.MinValue;
        existing.MaxValue = dto.MaxValue;
        existing.Description = dto.Description;
        existing.IsActive = dto.IsActive;
        existing.UpdatedAt = now;
        existing.UpdatedBy = "system"; // TODO: Get from current user context

        await context.SaveChangesAsync(ct);

        return MapToDto(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var parameter = await context.Parameters.FindAsync([id], ct);
        if (parameter == null)
            return false;

        // Soft delete by marking as inactive
        parameter.IsActive = false;
        parameter.UpdatedAt = timeProvider.GetUtcNow().DateTime;
        parameter.UpdatedBy = "system"; // TODO: Get from current user context

        await context.SaveChangesAsync(ct);
        return true;
    }

    private static ParameterDto MapToDto(Parameter parameter) => new()
    {
        Id = parameter.Id,
        Name = parameter.Name,
        Unit = parameter.Unit,
        WhoThreshold = parameter.WhoThreshold,
        MoroccanThreshold = parameter.MoroccanThreshold,
        MinValue = parameter.MinValue,
        MaxValue = parameter.MaxValue,
        Description = parameter.Description,
        IsActive = parameter.IsActive,
        CreatedDate = parameter.CreatedAt,
        LastModified = parameter.UpdatedAt ?? parameter.CreatedAt
    };
}

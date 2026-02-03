using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Shared.Enums;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;
using Quater.Backend.Data;
using Quater.Shared.ValueObjects;

namespace Quater.Backend.Services;

public class SampleService(
    QuaterDbContext context, 
    TimeProvider timeProvider,
    IValidator<Sample> validator) : ISampleService
{
    public async Task<SampleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sample = await context.Samples
            .AsNoTracking()
            .Where(s => s.Id == id && !s.IsDeleted)
            .FirstOrDefaultAsync(ct);

        return sample == null ? null : MapToDto(sample);
    }

    public async Task<PagedResult<SampleDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.Samples
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.CollectionDate);

        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SampleDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<SampleDto>> GetByLabIdAsync(Guid labId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.Samples
            .AsNoTracking()
            .Where(s => s.LabId == labId && !s.IsDeleted)
            .OrderByDescending(s => s.CollectionDate);

        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SampleDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<SampleDto> CreateAsync(CreateSampleDto dto, string userId, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        
        var sample = new Sample
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            Location = new Location(dto.LocationLatitude, dto.LocationLongitude, dto.LocationDescription, dto.LocationHierarchy),
            CollectionDate = dto.CollectionDate,
            CollectorName = dto.CollectorName,
            Notes = dto.Notes,
            Status = SampleStatus.Pending,
            LabId = dto.LabId,
            CreatedBy = userId,
            CreatedAt = now
        };

        // Validate
        await validator.ValidateAndThrowAsync(sample, ct);

        context.Samples.Add(sample);
        await context.SaveChangesAsync(ct);
        
        return MapToDto(sample);
    }

    public async Task<SampleDto?> UpdateAsync(Guid id, UpdateSampleDto dto, string userId, CancellationToken ct = default)
    {
        var existing = await context.Samples.FindAsync([id], ct);
        if (existing == null || existing.IsDeleted) 
            return null;

        // Note: Optimistic concurrency is now handled by RowVersion (byte[]) in the database
        // The Version field in DTO is kept for backward compatibility but not used for concurrency

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Update fields
        existing.Type = dto.Type;
        existing.Location = new Location(dto.LocationLatitude, dto.LocationLongitude, dto.LocationDescription, dto.LocationHierarchy);
        existing.CollectionDate = dto.CollectionDate;
        existing.CollectorName = dto.CollectorName;
        existing.Notes = dto.Notes;
        existing.Status = dto.Status;
        existing.UpdatedAt = now;
        existing.UpdatedBy = userId;

        // Validate
        await validator.ValidateAndThrowAsync(existing, ct);

        await context.SaveChangesAsync(ct);
        
        return MapToDto(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sample = await context.Samples.FindAsync([id], ct);
        if (sample == null || sample.IsDeleted) 
            return false;

        context.Samples.Remove(sample);

        await context.SaveChangesAsync(ct);
        return true;
    }

    private static SampleDto MapToDto(Sample sample) => new()
    {
        Id = sample.Id,
        Type = sample.Type,
        LocationLatitude = sample.Location.Latitude,
        LocationLongitude = sample.Location.Longitude,
        LocationDescription = sample.Location.Description,
        LocationHierarchy = sample.Location.Hierarchy,
        CollectionDate = sample.CollectionDate,
        CollectorName = sample.CollectorName,
        Notes = sample.Notes,
        Status = sample.Status,
        Version = 1, // Placeholder for backward compatibility - actual concurrency uses RowVersion
        LastModified = sample.UpdatedAt ?? sample.CreatedAt,
        LastModifiedBy = sample.UpdatedBy ?? sample.CreatedBy,
        IsDeleted = sample.IsDeleted,
        LabId = sample.LabId,
        CreatedBy = sample.CreatedBy,
        CreatedDate = sample.CreatedAt
    };
}

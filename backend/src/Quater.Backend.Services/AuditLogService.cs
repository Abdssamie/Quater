using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Extensions;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;

namespace Quater.Backend.Services;

/**
 * @id: audit-log-service
 * @priority: high
 * @progress: 85
 * @deps: ["audit-log-service-interface", "audit-log-dto", "audit-log-mapping-extensions"]
 * @tests: ["audit-log-service-tests"]
 * @spec: Service implementation for querying audit logs with filtering by entity type, user, date range, action type
 * @skills: ["csharp", "dotnet", "entity-framework"]
 */
/// <summary>
/// Service for querying audit logs (read-only)
/// </summary>
public class AuditLogService(QuaterDbContext context) : IAuditLogService
{
    /// <inheritdoc/>
    public async Task<PagedResult<AuditLogDto>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .AsNoTracking()
            .Where(a => !a.IsArchived);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items.Select(a => a.ToDto()),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AuditLogDto>> GetByEntityAsync(
        Guid entityId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityId == entityId && !a.IsArchived);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items.Select(a => a.ToDto()),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AuditLogDto>> GetByUserAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsArchived);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items.Select(a => a.ToDto()),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AuditLogDto>> GetByFilterAsync(
        AuditLogFilterDto filter,
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking();

        if (!filter.IncludeArchived)
        {
            query = query.Where(a => !a.IsArchived);
        }

        // Apply filters
        if (filter.EntityType.HasValue)
        {
            query = query.Where(a => a.EntityType == filter.EntityType.Value);
        }

        if (filter.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == filter.EntityId.Value);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == filter.UserId.Value);
        }

        if (filter.Action.HasValue)
        {
            query = query.Where(a => a.Action == filter.Action.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            // Include the entire end date (up to 23:59:59.999)
            var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.Timestamp <= endOfDay);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items.Select(a => a.ToDto()),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    /// <inheritdoc/>
    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var auditLog = await context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
            
        return auditLog?.ToDto();
    }
}

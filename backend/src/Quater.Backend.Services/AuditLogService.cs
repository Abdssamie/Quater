using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Extensions;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data.Interfaces;

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
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AuditLogDto>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var allLogs = await _unitOfWork.AuditLogs.GetAllAsync(ct);
        
        var filteredLogs = allLogs
            .Where(a => !a.IsArchived)
            .OrderByDescending(a => a.Timestamp)
            .ToList();

        var totalCount = filteredLogs.Count;

        var items = filteredLogs
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => a.ToDto())
            .ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
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
        var allLogs = await _unitOfWork.AuditLogs.GetAllAsync(ct);
        
        var filteredLogs = allLogs
            .Where(a => a.EntityId == entityId && !a.IsArchived)
            .OrderByDescending(a => a.Timestamp)
            .ToList();

        var totalCount = filteredLogs.Count;

        var items = filteredLogs
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => a.ToDto())
            .ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AuditLogDto>> GetByUserAsync(
        string userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var allLogs = await _unitOfWork.AuditLogs.GetAllAsync(ct);
        
        var filteredLogs = allLogs
            .Where(a => a.UserId == userId && !a.IsArchived)
            .OrderByDescending(a => a.Timestamp)
            .ToList();

        var totalCount = filteredLogs.Count;

        var items = filteredLogs
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => a.ToDto())
            .ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
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
        var allLogs = await _unitOfWork.AuditLogs.GetAllAsync(ct);
        
        var query = allLogs.AsQueryable();

        // Apply filters
        if (filter.EntityType.HasValue)
        {
            query = query.Where(a => a.EntityType == filter.EntityType.Value);
        }

        if (filter.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == filter.EntityId.Value);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            query = query.Where(a => a.UserId == filter.UserId);
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

        if (!filter.IncludeArchived)
        {
            query = query.Where(a => !a.IsArchived);
        }

        var filteredLogs = query
            .OrderByDescending(a => a.Timestamp)
            .ToList();

        var totalCount = filteredLogs.Count;

        var items = filteredLogs
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => a.ToDto())
            .ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    /// <inheritdoc/>
    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var auditLog = await _unitOfWork.AuditLogs.GetByIdAsync(id, ct);
        return auditLog?.ToDto();
    }
}

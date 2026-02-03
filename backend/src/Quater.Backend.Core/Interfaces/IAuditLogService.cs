using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

/**
 * @id: audit-log-service-interface
 * @priority: high
 * @progress: 100
 * @deps: ["audit-log-dto"]
 * @spec: Service interface for querying audit logs. Methods: GetAllAsync (paginated), GetByEntityAsync, GetByUserAsync, GetByFilterAsync
 * @skills: ["csharp", "dotnet"]
 */

/// <summary>
/// Service for querying audit logs (read-only)
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Get all audit logs with pagination
    /// </summary>
    Task<PagedResult<AuditLogDto>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Get audit logs by entity type and ID
    /// </summary>
    Task<PagedResult<AuditLogDto>> GetByEntityAsync(
        Guid entityId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Get audit logs by user ID
    /// </summary>
    Task<PagedResult<AuditLogDto>> GetByUserAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Get audit logs with advanced filtering
    /// </summary>
    Task<PagedResult<AuditLogDto>> GetByFilterAsync(
        AuditLogFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Get audit log by ID
    /// </summary>
    Task<AuditLogDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);
}

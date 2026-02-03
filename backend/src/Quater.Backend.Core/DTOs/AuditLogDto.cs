using Quater.Shared.Enums;

namespace Quater.Backend.Core.DTOs;

/**
 * @id: audit-log-dto
 * @priority: high
 * @progress: 100
 * @spec: Read-only DTOs for AuditLog viewing. Includes: AuditLogDto, AuditLogFilterDto
 * @skills: ["csharp", "dotnet"]
 */

/// <summary>
/// DTO for audit log entry (read-only)
/// </summary>
public record AuditLogDto
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? UserEmail { get; init; }
    public EntityType EntityType { get; init; }
    public Guid EntityId { get; init; }
    public AuditAction Action { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public bool IsTruncated { get; init; }
    public DateTime Timestamp { get; init; }
    public string? IpAddress { get; init; }
    public bool IsArchived { get; init; }
}

/// <summary>
/// DTO for filtering audit logs
/// </summary>
public record AuditLogFilterDto
{
    /// <summary>
    /// Filter by entity type
    /// </summary>
    public EntityType? EntityType { get; init; }

    /// <summary>
    /// Filter by entity ID
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Filter by user ID
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Filter by action type
    /// </summary>
    public AuditAction? Action { get; init; }

    /// <summary>
    /// Filter by start date (inclusive)
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Filter by end date (inclusive)
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Include archived records
    /// </summary>
    public bool IncludeArchived { get; init; } = false;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; init; } = 50;
}

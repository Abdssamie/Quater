using Quater.Backend.Core.DTOs;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Extensions;

/**
 * @id: audit-log-mapping-extensions
 * @priority: high
 * @progress: 100
 * @deps: ["audit-log-dto"]
 * @spec: Mapping extensions to convert AuditLog entity to AuditLogDto
 * @skills: ["csharp", "dotnet"]
 */
/// <summary>
/// Extension methods for mapping AuditLog entities to DTOs
/// </summary>
public static class AuditLogMappingExtensions
{
    /// <summary>
    /// Maps AuditLog entity to AuditLogDto
    /// </summary>
    public static AuditLogDto ToDto(this AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            UserEmail = auditLog.User?.Email,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            Action = auditLog.Action,
            OldValue = auditLog.OldValue,
            NewValue = auditLog.NewValue,
            IsTruncated = auditLog.IsTruncated,
            Timestamp = auditLog.Timestamp,
            IpAddress = auditLog.IpAddress,
            IsArchived = auditLog.IsArchived
        };
    }
}

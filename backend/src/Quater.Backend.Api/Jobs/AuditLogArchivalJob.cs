using Quartz;

namespace Quater.Backend.Api.Jobs;

/// <summary>
/// Job to archive old audit logs nightly.
/// This job will be implemented in a future phase to move old audit logs to the archive table.
/// </summary>
public class AuditLogArchivalJob : IJob
{
    private readonly ILogger<AuditLogArchivalJob> _logger;

    public AuditLogArchivalJob(ILogger<AuditLogArchivalJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("AuditLogArchivalJob executed at {Time}", DateTime.UtcNow);
        
        // TODO: Implement audit log archival logic
        // 1. Query AuditLog table for records older than retention period (e.g., 90 days)
        // 2. Move records to AuditLogArchive table
        // 3. Delete archived records from AuditLog table
        
        return Task.CompletedTask;
    }
}

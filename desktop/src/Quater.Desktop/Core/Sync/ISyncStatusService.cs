namespace Quater.Desktop.Core.Sync;

public interface ISyncStatusService
{
    Task<SyncQueueSummary> GetSummaryAsync(CancellationToken ct = default);
    Task RetryAllFailedAsync(CancellationToken ct = default);
    Task RetryAsync(string operationId, CancellationToken ct = default);
}

public sealed record SyncQueueSummary(
    int PendingCount,
    int FailedCount,
    int InProgressCount,
    string LastSyncStatusText);

namespace Quater.Desktop.Core.Sync;

public interface ISyncStatusService
{
    SyncStatusSummary GetSummary();
    void UpdateQueueCounts(int pendingCount, int failedCount, int inProgressCount);
    void RetryAllFailed();
}

public sealed record SyncStatusSummary(
    int PendingCount,
    int FailedCount,
    int InProgressCount,
    string LastSyncStatusText
);

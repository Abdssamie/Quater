using Quater.Desktop.Core.State;

namespace Quater.Desktop.Core.Sync;

public sealed class SyncStatusService(AppState appState) : ISyncStatusService
{
    private int _failedCount;
    private int _inProgressCount;

    public SyncStatusSummary GetSummary()
    {
        var lastSyncStatusText = appState.LastSyncTime == "Never"
            ? "Never synced"
            : $"Last synced at {appState.LastSyncTime}";

        return new SyncStatusSummary(
            PendingCount: appState.PendingSyncCount,
            FailedCount: _failedCount,
            InProgressCount: _inProgressCount,
            LastSyncStatusText: lastSyncStatusText);
    }

    public void UpdateQueueCounts(int pendingCount, int failedCount, int inProgressCount)
    {
        appState.PendingSyncCount = pendingCount;
        _failedCount = failedCount;
        _inProgressCount = inProgressCount;
    }

    public void RetryAllFailed()
    {
        appState.PendingSyncCount += _failedCount;
        _failedCount = 0;
    }
}

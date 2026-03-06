using Quater.Desktop.Core.State;

namespace Quater.Desktop.Core.Sync;

public sealed class SyncStatusService(AppState appState) : ISyncStatusService
{
    private int _legacyFailedCount;
    private int _legacyInProgressCount;

    public Task<SyncQueueSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var failed = appState.FailedSyncCount;
        var pending = appState.PendingSyncCount;
        var inProgress = appState.IsSyncing ? 1 : 0;

        var syncText = string.IsNullOrWhiteSpace(appState.SyncStatusText)
            ? $"Last sync: {appState.LastSyncTime}"
            : appState.SyncStatusText;

        return Task.FromResult(new SyncQueueSummary(pending, failed, inProgress, syncText));
    }

    public Task RetryAllFailedAsync(CancellationToken ct = default)
    {
        appState.PendingSyncCount += appState.FailedSyncCount;
        appState.FailedSyncCount = 0;
        appState.SyncStatusText = "Retry scheduled";
        return Task.CompletedTask;
    }

    public Task RetryAsync(string operationId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        if (appState.FailedSyncCount > 0)
        {
            appState.FailedSyncCount -= 1;
            appState.PendingSyncCount += 1;
        }

        appState.SyncStatusText = $"Retry scheduled for {operationId}";
        return Task.CompletedTask;
    }

    public SyncStatusSummary GetSummary()
    {
        var lastSyncStatusText = appState.LastSyncTime == "Never"
            ? "Never synced"
            : $"Last synced at {appState.LastSyncTime}";

        return new SyncStatusSummary(
            PendingCount: appState.PendingSyncCount,
            FailedCount: _legacyFailedCount,
            InProgressCount: _legacyInProgressCount,
            LastSyncStatusText: lastSyncStatusText);
    }

    public void UpdateQueueCounts(int pendingCount, int failedCount, int inProgressCount)
    {
        appState.PendingSyncCount = pendingCount;
        _legacyFailedCount = failedCount;
        _legacyInProgressCount = inProgressCount;
    }

    public void RetryAllFailed()
    {
        appState.PendingSyncCount += _legacyFailedCount;
        _legacyFailedCount = 0;
    }
}

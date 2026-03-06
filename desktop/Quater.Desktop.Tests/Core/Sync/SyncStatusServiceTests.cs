using Quater.Desktop.Core.State;
using Quater.Desktop.Core.Sync;

namespace Quater.Desktop.Tests.Core.Sync;

public sealed class SyncStatusServiceTests
{
    [Fact]
    public void GetSummary_WhenQueueStateIsUpdated_ReturnsPendingFailedAndInProgressCounts()
    {
        var appState = new AppState();
        var service = new SyncStatusService(appState);

        service.UpdateQueueCounts(pendingCount: 3, failedCount: 2, inProgressCount: 1);

        var summary = service.GetSummary();

        Assert.Equal(3, summary.PendingCount);
        Assert.Equal(2, summary.FailedCount);
        Assert.Equal(1, summary.InProgressCount);
    }

    [Fact]
    public void GetSummary_WhenLastSyncTimeIsNever_ReturnsNeverSyncedStatusText()
    {
        var appState = new AppState { LastSyncTime = "Never" };
        var service = new SyncStatusService(appState);

        var summary = service.GetSummary();

        Assert.Equal("Never synced", summary.LastSyncStatusText);
    }

    [Fact]
    public void RetryAllFailed_WhenFailedItemsExist_MovesFailedItemsBackToPending()
    {
        var appState = new AppState { LastSyncTime = "12:45" };
        var service = new SyncStatusService(appState);
        service.UpdateQueueCounts(pendingCount: 1, failedCount: 2, inProgressCount: 0);

        service.RetryAllFailed();

        var summary = service.GetSummary();

        Assert.Equal(3, summary.PendingCount);
        Assert.Equal(0, summary.FailedCount);
        Assert.Equal(0, summary.InProgressCount);
        Assert.Equal("Last synced at 12:45", summary.LastSyncStatusText);
    }
}

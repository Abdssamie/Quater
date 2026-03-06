using Moq;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.State;
using Quater.Desktop.Core.Sync;
using Quater.Desktop.Features.Sync.Center;

namespace Quater.Desktop.Tests.Features.Sync;

public sealed class SyncCenterViewModelTests
{
    [Fact]
    public async Task RefreshCommand_LoadsQueueSummary_FromSyncStatusService()
    {
        var syncStatusService = new Mock<ISyncStatusService>(MockBehavior.Strict);
        var conflictResolutionService = new Mock<IConflictResolutionService>(MockBehavior.Strict);
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        var appState = new AppState();

        syncStatusService
            .Setup(service => service.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncQueueSummary(5, 2, 1, "Last sync: 09:42"));

        var viewModel = new SyncCenterViewModel(syncStatusService.Object, conflictResolutionService.Object, dialogService.Object, appState);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(5, viewModel.PendingCount);
        Assert.Equal(2, viewModel.FailedCount);
        Assert.Equal(1, viewModel.InProgressCount);
        Assert.Equal("Last sync: 09:42", viewModel.LastSyncStatusText);
    }

    [Fact]
    public async Task RetryAllFailedCommand_RequestsBulkRetry_AndRefreshesSummary()
    {
        var syncStatusService = new Mock<ISyncStatusService>(MockBehavior.Strict);
        var conflictResolutionService = new Mock<IConflictResolutionService>(MockBehavior.Strict);
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        var appState = new AppState();

        syncStatusService
            .Setup(service => service.RetryAllFailedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        syncStatusService
            .Setup(service => service.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncQueueSummary(3, 0, 0, "Last sync: 09:50"));

        var viewModel = new SyncCenterViewModel(syncStatusService.Object, conflictResolutionService.Object, dialogService.Object, appState);

        await viewModel.RetryAllFailedCommand.ExecuteAsync(null);

        syncStatusService.Verify(service => service.RetryAllFailedAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(0, viewModel.FailedCount);
    }

    [Fact]
    public async Task RetrySingleCommand_RequestsRetryForSelectedOperation()
    {
        var syncStatusService = new Mock<ISyncStatusService>(MockBehavior.Strict);
        var conflictResolutionService = new Mock<IConflictResolutionService>(MockBehavior.Strict);
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        var appState = new AppState();

        syncStatusService
            .Setup(service => service.RetryAsync("op-42", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        syncStatusService
            .Setup(service => service.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncQueueSummary(2, 0, 0, "Last sync: 10:03"));

        var viewModel = new SyncCenterViewModel(syncStatusService.Object, conflictResolutionService.Object, dialogService.Object, appState);

        await viewModel.RetryOperationCommand.ExecuteAsync("op-42");

        syncStatusService.Verify(service => service.RetryAsync("op-42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ConflictResolutionChoice.KeepLocal)]
    [InlineData(ConflictResolutionChoice.KeepServer)]
    [InlineData(ConflictResolutionChoice.Reload)]
    public async Task ResolveConflictCommand_DelegatesToConflictResolutionService(ConflictResolutionChoice choice)
    {
        var syncStatusService = new Mock<ISyncStatusService>(MockBehavior.Strict);
        var conflictResolutionService = new Mock<IConflictResolutionService>(MockBehavior.Strict);
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        var appState = new AppState();

        conflictResolutionService
            .Setup(service => service.ResolveAsync("conflict-7", choice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        syncStatusService
            .Setup(service => service.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncQueueSummary(1, 0, 0, "Last sync: 10:21"));

        var viewModel = new SyncCenterViewModel(syncStatusService.Object, conflictResolutionService.Object, dialogService.Object, appState);

        await viewModel.ResolveConflictCommand.ExecuteAsync(new ResolveConflictRequest("conflict-7", choice));

        conflictResolutionService.Verify(service => service.ResolveAsync("conflict-7", choice, It.IsAny<CancellationToken>()), Times.Once);
    }
}

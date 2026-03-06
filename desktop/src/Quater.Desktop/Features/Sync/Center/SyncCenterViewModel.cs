using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.State;
using Quater.Desktop.Core.Sync;

namespace Quater.Desktop.Features.Sync.Center;

public sealed partial class SyncCenterViewModel(
    ISyncStatusService syncStatusService,
    IConflictResolutionService conflictResolutionService,
    IDialogService dialogService,
    AppState appState) : ViewModelBase
{
    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private int _inProgressCount;

    [ObservableProperty]
    private string _lastSyncStatusText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        await RefreshCoreAsync(ct);
    }

    [RelayCommand]
    private async Task Refresh(CancellationToken ct = default)
    {
        await RefreshCoreAsync(ct);
    }

    [RelayCommand]
    private async Task RetryAllFailed(CancellationToken ct = default)
    {
        await ExecuteBusyOperationAsync(async () =>
        {
            await syncStatusService.RetryAllFailedAsync(ct);
            await RefreshCoreAsync(ct);
            appState.SyncStatusText = LastSyncStatusText;
        });
    }

    [RelayCommand]
    private async Task RetryOperation(string? operationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        await ExecuteBusyOperationAsync(async () =>
        {
            await syncStatusService.RetryAsync(operationId, ct);
            await RefreshCoreAsync(ct);
            appState.SyncStatusText = LastSyncStatusText;
        });
    }

    [RelayCommand]
    private async Task ResolveConflict(ResolveConflictRequest? request, CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ConflictId))
        {
            return;
        }

        await ExecuteBusyOperationAsync(async () =>
        {
            await conflictResolutionService.ResolveAsync(request.ConflictId, request.Choice, ct);
            await RefreshCoreAsync(ct);
            appState.SyncStatusText = LastSyncStatusText;
        });
    }

    private async Task RefreshCoreAsync(CancellationToken ct = default)
    {
        var summary = await syncStatusService.GetSummaryAsync(ct);
        PendingCount = summary.PendingCount;
        FailedCount = summary.FailedCount;
        InProgressCount = summary.InProgressCount;
        LastSyncStatusText = summary.LastSyncStatusText;

        appState.PendingSyncCount = summary.PendingCount;
        appState.FailedSyncCount = summary.FailedCount;
        appState.SyncStatusText = summary.LastSyncStatusText;
    }

    private async Task ExecuteBusyOperationAsync(Func<Task> operation)
    {
        try
        {
            IsBusy = true;
            await operation();
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Sync action failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed record ResolveConflictRequest(string ConflictId, ConflictResolutionChoice Choice);

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.Export;

namespace Quater.Desktop.Features.Audit.List;

public sealed partial class AuditListViewModel(
    IApiClientFactory apiClientFactory,
    IDialogService dialogService,
    ICsvExportService csvExportService) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<AuditLogDto> _auditLogs = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private EntityType? _entityTypeFilter;

    [ObservableProperty]
    private AuditAction? _actionFilter;

    [ObservableProperty]
    private DateTime? _startDateFilter;

    [ObservableProperty]
    private DateTime? _endDateFilter;

    [ObservableProperty]
    private Guid? _userIdFilter;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _exportedCsv = string.Empty;

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        await LoadAuditLogsCoreAsync(ct);
    }

    [RelayCommand]
    private async Task LoadAuditLogs(CancellationToken ct = default)
    {
        await LoadAuditLogsCoreAsync(ct);
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        try
        {
            ExportedCsv = csvExportService.ExportAuditLogs(AuditLogs);
            dialogService.ShowSuccess("Audit logs exported to CSV.");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Failed to export audit logs: {ex.Message}");
        }
    }

    private async Task LoadAuditLogsCoreAsync(CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;

            var api = apiClientFactory.GetAuditLogsApi();
            var filter = new AuditLogFilterDto(
                entityType: EntityTypeFilter,
                userId: UserIdFilter,
                action: ActionFilter,
                startDate: StartDateFilter,
                endDate: EndDateFilter,
                pageNumber: PageNumber,
                pageSize: PageSize);

            var response = await api.ApiAuditLogsFilterPostAsync(auditLogFilterDto: filter, cancellationToken: ct);

            AuditLogs.Clear();
            foreach (var log in response.Items ?? [])
            {
                AuditLogs.Add(log);
            }

            TotalCount = response.TotalCount;
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Failed to load audit logs: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

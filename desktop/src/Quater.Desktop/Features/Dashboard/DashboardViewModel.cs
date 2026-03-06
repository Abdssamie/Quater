using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Features.Dashboard;

public sealed partial class DashboardViewModel(
    IApiClientFactory apiClientFactory,
    ISyncStatusService syncStatusService,
    IApiErrorFormatter apiErrorFormatter,
    AppState appState) : ViewModelBase
{
    [ObservableProperty]
    private string _complianceRate = "0%";
    
    [ObservableProperty]
    private string _samplesThisWeek = "0";
    
    [ObservableProperty]
    private string _pendingAlerts = "0";

    [ObservableProperty]
    private string _syncIndicator = "Unknown";

    [ObservableProperty]
    private string _warningMessage = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<DashboardStat> _stats = [];
    
    [ObservableProperty]
    private ObservableCollection<RecentSample> _recentSamples = [];

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        await LoadMetricsAsync(ct);
        await LoadRecentSamplesAsync(ct);
    }

    private async Task LoadMetricsAsync(CancellationToken ct)
    {
        var warnings = new List<string>();

        await LoadSampleCountAsync(warnings, ct);
        await LoadComplianceMetricsAsync(warnings, ct);

        var syncSummary = syncStatusService.GetSummary(appState);
        SyncIndicator = syncSummary.StatusText;

        WarningMessage = string.Join(" ", warnings);

        Stats =
        [
            new("Total Samples", SamplesThisWeek, "M18,17L21,22H3L6,17H18M18,17L14,5H10L6,17H18M15,4H9L8,4H9L12,1L15,4Z"),
            new("Compliance Rate", ComplianceRate, "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M10 17L5 12L6.41 10.59L10 14.17L17.59 6.58L19 8L10 17Z", true),
            new("Critical Alerts", PendingAlerts, "M13 14H11V9H13M13 18H11V16H13M1 21H23L12 2L1 21Z", false, true),
            new("Sync", SyncIndicator, "M12,3C16.42,3 20,4.79 20,7C20,9.21 16.42,11 12,11C7.58,11 4,9.21 4,7C4,4.79 7.58,3 12,3M4,9V12C4,14.21 7.58,16 12,16C16.42,16 20,14.21 20,12V9C20,11.21 16.42,13 12,13C7.58,13 4,11.21 4,9")
        ];
    }

    private async Task LoadSampleCountAsync(List<string> warnings, CancellationToken ct)
    {
        try
        {
            var samplesApi = apiClientFactory.GetSamplesApi();
            var sampleResponse = await samplesApi.ApiSamplesGetAsync(cancellationToken: ct);
            SamplesThisWeek = sampleResponse.TotalCount.ToString();
        }
        catch (Exception ex)
        {
            warnings.Add(apiErrorFormatter.ToDisplayMessage(ex, "Unable to load total samples."));
        }
    }

    private async Task LoadComplianceMetricsAsync(List<string> warnings, CancellationToken ct)
    {
        try
        {
            var resultsApi = apiClientFactory.GetTestResultsApi();
            var resultResponse = await resultsApi.ApiTestResultsGetAsync(cancellationToken: ct);
            var items = resultResponse.Items ?? [];

            if (items.Count == 0)
            {
                ComplianceRate = "0.0%";
                PendingAlerts = "0";
                return;
            }

            var compliantCount = items.Count(item => item.ComplianceStatus == ComplianceStatus.NUMBER_0);
            var alertCount = items.Count(item => item.ComplianceStatus is ComplianceStatus.NUMBER_1 or ComplianceStatus.NUMBER_2);
            var rate = (double)compliantCount / items.Count * 100d;

            ComplianceRate = $"{rate:0.0}%";
            PendingAlerts = alertCount.ToString();
        }
        catch (Exception ex)
        {
            warnings.Add(apiErrorFormatter.ToDisplayMessage(ex, "Unable to load compliance metrics."));
        }
    }

    private Task LoadRecentSamplesAsync(CancellationToken ct)
    {
        RecentSamples = [];

        return Task.CompletedTask;
    }
}

public interface IApiErrorFormatter
{
    string ToDisplayMessage(Exception exception, string fallbackMessage);
}

public interface ISyncStatusService
{
    SyncStatusSummary GetSummary(AppState appState);
}

public sealed record SyncStatusSummary(string StatusText, int pendingCount, int failedCount);

public sealed record DashboardStat(
    string Title,
    string Value,
    string Icon,
    bool IsGood = false,
    bool IsBad = false
);

public sealed record RecentSample(
    string Id,
    string Location,
    string Parameter,
    string Result,
    string Status
);

using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Settings;

namespace Quater.Desktop.Features.Diagnostics;

public sealed partial class DiagnosticsViewModel(
    ISettingsStore settingsStore,
    IApiClientFactory apiClientFactory) : ViewModelBase
{
    [ObservableProperty]
    private string _backendUrl = string.Empty;

    [ObservableProperty]
    private string _runtimeVersion = string.Empty;

    [ObservableProperty]
    private string _operatingSystem = string.Empty;

    [ObservableProperty]
    private string _backendStatus = "Unknown";

    [ObservableProperty]
    private bool _isChecking;

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        var settings = await settingsStore.LoadAsync(ct);
        BackendUrl = settings.BackendUrl;
        RuntimeVersion = RuntimeInformation.FrameworkDescription;
        OperatingSystem = RuntimeInformation.OSDescription;

        await CheckBackendHealth(ct);
    }

    [RelayCommand]
    private async Task CheckBackendHealth(CancellationToken ct = default)
    {
        try
        {
            IsChecking = true;
            var versionApi = apiClientFactory.GetVersionApi();
            await versionApi.ApiVersionGetAsync(cancellationToken: ct);
            BackendStatus = "Reachable";
        }
        catch
        {
            BackendStatus = "Unreachable";
        }
        finally
        {
            IsChecking = false;
        }
    }
}

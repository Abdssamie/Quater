using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Settings;

namespace Quater.Desktop.Features.Onboarding;

public sealed partial class OnboardingViewModel(
    SettingsUpdater settingsUpdater,
    IBackendConnectivityProbe connectivityProbe) : ViewModelBase
{
    private const string DefaultCustomUrl = "http://127.0.0.1:5198";

    public event EventHandler? OnboardingCompleted;

    [ObservableProperty]
    private bool _useCloud = true;

    [ObservableProperty]
    private string _customUrl = DefaultCustomUrl;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isConnecting;

    [RelayCommand]
    private async Task ContinueAsync(CancellationToken ct = default)
    {
        ErrorMessage = string.Empty;
        IsConnecting = true;

        try
        {
            var targetUrl = UseCloud ? AppSettings.QuaterCloudUrl.TrimEnd('/') : NormalizeCustomUrl();
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return;
            }

            await connectivityProbe.ProbeAsync(targetUrl, ct);
            await settingsUpdater.UpdateBackendUrlAsync(targetUrl, ct);
            await settingsUpdater.MarkOnboardedAsync(ct);
            OnboardingCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            ErrorMessage = "Could not reach the backend. Verify the URL and that the server is running.";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private string NormalizeCustomUrl()
    {
        if (string.IsNullOrWhiteSpace(CustomUrl))
        {
            ErrorMessage = "Server URL is required.";
            return string.Empty;
        }

        if (!TryNormalizeUrl(CustomUrl, out var normalized))
        {
            ErrorMessage = "Invalid URL. Use a full http or https URL.";
            return string.Empty;
        }

        return normalized.TrimEnd('/');
    }

    partial void OnUseCloudChanged(bool value)
    {
        ErrorMessage = string.Empty;
    }

    partial void OnCustomUrlChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            ErrorMessage = string.Empty;
        }
    }

    private static bool TryNormalizeUrl(string input, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}

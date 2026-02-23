using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Settings;

namespace Quater.Desktop.Features.Onboarding;

public sealed partial class OnboardingViewModel : ViewModelBase
{
    private const string DefaultCustomUrl = "http://127.0.0.1:5198";
    private readonly SettingsUpdater _settingsUpdater;

    public event EventHandler? OnboardingCompleted;

    [ObservableProperty]
    private bool _useCloud = true;

    [ObservableProperty]
    private string _customUrl = DefaultCustomUrl;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public OnboardingViewModel(SettingsUpdater settingsUpdater)
    {
        _settingsUpdater = settingsUpdater;
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (UseCloud)
        {
            var cloudUrl = AppSettings.QuaterCloudUrl.TrimEnd('/');
            await _settingsUpdater.UpdateBackendUrlAsync(cloudUrl);
            await _settingsUpdater.MarkOnboardedAsync();
            OnboardingCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomUrl))
        {
            ErrorMessage = "Server URL is required.";
            return;
        }

        if (!TryNormalizeUrl(CustomUrl, out var normalized))
        {
            ErrorMessage = "Invalid URL. Use a full http or https URL.";
            return;
        }

        await _settingsUpdater.UpdateBackendUrlAsync(normalized.TrimEnd('/'));
        await _settingsUpdater.MarkOnboardedAsync();
        OnboardingCompleted?.Invoke(this, EventArgs.Empty);
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Features.Auth;

public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly AppState _appState;
    private readonly IAuthApi _authApi;
    private readonly SettingsUpdater _settingsUpdater;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LoginViewModel(IAuthService authService, AppState appState, IAuthApi authApi, SettingsUpdater settingsUpdater)
    {
        _authService = authService;
        _appState = appState;
        _authApi = authApi;
        _settingsUpdater = settingsUpdater;
    }

    [RelayCommand]
    private async Task SignIn()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync();
            if (result.IsError)
            {
                ErrorMessage = result.Error ?? "Authentication failed.";
                return;
            }

            var userInfo = await _authApi.ApiAuthUserinfoGetAsync();
            _appState.CurrentUser = userInfo;
            _appState.AvailableLabs = userInfo.Labs ?? [];

            var defaultLabId = SelectDefaultLabId(userInfo);
            if (defaultLabId.HasValue)
            {
                _appState.CurrentLabId = defaultLabId.Value;
                _appState.CurrentLabName = userInfo.Labs?.FirstOrDefault(lab => lab.LabId == defaultLabId)?.LabName ?? string.Empty;
                _settingsUpdater.Current.LastUsedLabId = defaultLabId.Value;
                await _settingsUpdater.SaveAsync();
            }

            _appState.IsAuthenticated = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Guid? SelectDefaultLabId(Quater.Desktop.Api.Model.UserDto userInfo)
    {
        if (userInfo.Labs == null || userInfo.Labs.Count == 0)
        {
            return null;
        }

        var lastUsedLabId = _settingsUpdater.Current.LastUsedLabId;
        if (lastUsedLabId.HasValue && userInfo.Labs.Any(lab => lab.LabId == lastUsedLabId.Value))
        {
            return lastUsedLabId.Value;
        }

        if (userInfo.Labs.Count == 1)
        {
            return userInfo.Labs[0].LabId;
        }

        return null;
    }
}

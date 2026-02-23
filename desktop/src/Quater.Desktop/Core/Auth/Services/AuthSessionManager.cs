using Microsoft.Extensions.Logging;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Core.Auth.Services;

public sealed class AuthSessionManager(
    IAuthService authService,
    IAccessTokenCache accessTokenCache,
    IApiClientFactory apiClientFactory,
    AppState appState,
    SettingsUpdater settingsUpdater,
    IDialogService dialogService,
    ILogger<AuthSessionManager> logger)
{
    private const string SessionExpiredMessage = "Session expired. Please sign in again.";
    private bool _isHandlingUnauthorized;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("AuthSessionManager.InitializeAsync started");
            await accessTokenCache.InitializeAsync(ct);
            var token = accessTokenCache.CurrentToken ?? await authService.GetValidAccessTokenAsync(ct);
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("No access token available during initialization");
                appState.IsAuthenticated = false;
                return;
            }

            var usersApi = apiClientFactory.GetUsersApi();
            logger.LogInformation("Calling ApiUsersMeGetAsync during initialization");
            var userInfo = await usersApi.ApiUsersMeGetAsync(cancellationToken: ct);
            ApplyUserInfo(userInfo);
            RestoreLastUsedLab(userInfo);
            appState.IsAuthenticated = true;
            appState.AuthNotice = string.Empty;
            accessTokenCache.StartAutoRefresh();
            logger.LogInformation("AuthSessionManager.InitializeAsync completed");
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "ApiUsersMeGetAsync failed with status {StatusCode}. Content: {Content}", ex.ErrorCode, ex.ErrorContent);
            appState.IsAuthenticated = false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthSessionManager.InitializeAsync failed");
            appState.IsAuthenticated = false;
        }
    }

    public async Task HandleLoginSuccessAsync(AuthResult result, CancellationToken ct = default)
    {
        if (result.IsError)
        {
            return;
        }

        Quater.Desktop.Api.Client.ApiClient.ResetUnauthorizedSignal();

        logger.LogInformation("HandleLoginSuccessAsync started");
        await accessTokenCache.InitializeAsync(ct);
        if (string.IsNullOrWhiteSpace(accessTokenCache.CurrentToken))
        {
            logger.LogInformation("Access token not cached, refreshing");
            await accessTokenCache.RefreshAsync(ct);
        }

        var usersApi = apiClientFactory.GetUsersApi();
        logger.LogInformation("Calling ApiUsersMeGetAsync after login");
        UserDto userInfo;
        try
        {
            userInfo = await usersApi.ApiUsersMeGetAsync(cancellationToken: ct);
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "ApiUsersMeGetAsync failed with status {StatusCode}. Content: {Content}", ex.ErrorCode, ex.ErrorContent);
            throw;
        }
        ApplyUserInfo(userInfo);
        await SaveLastUsedLabAsync(userInfo, ct);
        appState.IsAuthenticated = true;
        appState.AuthNotice = string.Empty;
        accessTokenCache.StartAutoRefresh();
        dialogService.ShowSuccess("Signed in successfully");
        logger.LogInformation("HandleLoginSuccessAsync completed");
    }

    public async Task HandleLogoutAsync(CancellationToken ct = default)
    {
        Quater.Desktop.Api.Client.ApiClient.ResetUnauthorizedSignal();
        await authService.LogoutAsync(ct);
        appState.CurrentUser = null;
        appState.AvailableLabs = [];
        appState.CurrentLabId = Guid.Empty;
        appState.CurrentLabName = string.Empty;
        appState.IsAuthenticated = false;
        accessTokenCache.StopAutoRefresh();
        accessTokenCache.Clear();
        dialogService.ShowToast("Signed out");
    }

    public async Task HandleUnauthorizedAsync(CancellationToken ct = default)
    {
        if (_isHandlingUnauthorized)
        {
            return;
        }

        _isHandlingUnauthorized = true;
        try
        {
            await HandleLogoutAsync(ct);
            appState.AuthNotice = SessionExpiredMessage;
            dialogService.ShowWarning(appState.AuthNotice);
        }
        finally
        {
            _isHandlingUnauthorized = false;
        }
    }

    private void ApplyUserInfo(UserDto userInfo)
    {
        appState.CurrentUser = userInfo;
        appState.AvailableLabs = userInfo.Labs;
    }

    private void RestoreLastUsedLab(UserDto userInfo)
    {
        var defaultLabId = SelectDefaultLabId(userInfo);
        if (defaultLabId.HasValue)
        {
            appState.CurrentLabId = defaultLabId.Value;
            appState.CurrentLabName = userInfo.Labs.FirstOrDefault(lab => lab.LabId == defaultLabId)?.LabName ?? string.Empty;
        }
    }

    private async Task SaveLastUsedLabAsync(UserDto userInfo, CancellationToken ct)
    {
        var defaultLabId = SelectDefaultLabId(userInfo);
        if (defaultLabId.HasValue)
        {
            appState.CurrentLabId = defaultLabId.Value;
            appState.CurrentLabName = userInfo.Labs.FirstOrDefault(lab => lab.LabId == defaultLabId)?.LabName ?? string.Empty;
            settingsUpdater.Current.LastUsedLabId = defaultLabId.Value;
            await settingsUpdater.SaveAsync(ct);
        }
    }

    private Guid? SelectDefaultLabId(UserDto userInfo)
    {
        if (userInfo.Labs.Count == 0)
        {
            return null;
        }

        var lastUsedLabId = settingsUpdater.Current.LastUsedLabId;
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

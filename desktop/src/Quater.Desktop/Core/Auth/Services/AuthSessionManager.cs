using Avalonia.Threading;
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
                Dispatcher.UIThread.Post(() => appState.IsAuthenticated = false);
                return;
            }

            var usersApi = apiClientFactory.GetUsersApi();
            logger.LogInformation("Calling ApiUsersMeGetAsync during initialization");
            var userInfo = await usersApi.ApiUsersMeGetAsync(cancellationToken: ct);
            var (defaultLabId, labName) = ComputeDefaultLab(userInfo);
            Dispatcher.UIThread.Post(() =>
            {
                appState.CurrentUser = userInfo;
                appState.AvailableLabs = userInfo.Labs;
                if (defaultLabId.HasValue)
                {
                    appState.CurrentLabId = defaultLabId.Value;
                    appState.CurrentLabName = labName;
                }
                appState.IsAuthenticated = true;
                appState.AuthNotice = string.Empty;
            });
            accessTokenCache.StartAutoRefresh();
            logger.LogInformation("AuthSessionManager.InitializeAsync completed");
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "ApiUsersMeGetAsync failed with status {StatusCode}. Content: {Content}", ex.ErrorCode, ex.ErrorContent);
            Dispatcher.UIThread.Post(() => appState.IsAuthenticated = false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthSessionManager.InitializeAsync failed");
            Dispatcher.UIThread.Post(() => appState.IsAuthenticated = false);
        }
    }

    public async Task HandleLoginSuccessAsync(AuthResult result, CancellationToken ct = default)
    {
        logger.LogInformation("[AuthSessionManager] HandleLoginSuccessAsync started. AuthResult: IsError={IsError}, HasAccessToken={HasToken}", 
            result.IsError, 
            !string.IsNullOrWhiteSpace(result.AccessToken));
        
        if (result.IsError)
        {
            logger.LogWarning("[AuthSessionManager] AuthResult has error: {Error}", result.Error);
            return;
        }

        Quater.Desktop.Api.Client.ApiClient.ResetUnauthorizedSignal();

        logger.LogInformation("[AuthSessionManager] Calling accessTokenCache.InitializeAsync...");
        await accessTokenCache.InitializeAsync(ct);
        
        var tokenAfterInit = accessTokenCache.CurrentToken;
        logger.LogInformation("[AuthSessionManager] After InitializeAsync, CurrentToken is {TokenStatus}", string.IsNullOrWhiteSpace(tokenAfterInit) ? "NULL/EMPTY" : $"length {tokenAfterInit.Length}");
        
        if (string.IsNullOrWhiteSpace(tokenAfterInit))
        {
            logger.LogInformation("[AuthSessionManager] Access token not cached, calling RefreshAsync...");
            await accessTokenCache.RefreshAsync(ct);
            var tokenAfterRefresh = accessTokenCache.CurrentToken;
            logger.LogInformation("[AuthSessionManager] After RefreshAsync, CurrentToken is {TokenStatus}", string.IsNullOrWhiteSpace(tokenAfterRefresh) ? "NULL/EMPTY" : $"length {tokenAfterRefresh.Length}");
        }

        // Ensure we have a valid token before making API calls
        var finalToken = accessTokenCache.CurrentToken;
        if (string.IsNullOrWhiteSpace(finalToken))
        {
            logger.LogError("[AuthSessionManager] No access token available after login");
            throw new InvalidOperationException("Authentication failed: No access token available");
        }

        logger.LogInformation("[AuthSessionManager] Creating UsersApi client...");
        var usersApi = apiClientFactory.GetUsersApi();
        logger.LogInformation("[AuthSessionManager] Calling ApiUsersMeGetAsync...");
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

        var (defaultLabId, labName) = ComputeDefaultLab(userInfo);
        if (defaultLabId.HasValue)
        {
            settingsUpdater.Current.LastUsedLabId = defaultLabId.Value;
            await settingsUpdater.SaveAsync(ct);
        }

        Dispatcher.UIThread.Post(() =>
        {
            appState.CurrentUser = userInfo;
            appState.AvailableLabs = userInfo.Labs;
            if (defaultLabId.HasValue)
            {
                appState.CurrentLabId = defaultLabId.Value;
                appState.CurrentLabName = labName;
            }
            appState.IsAuthenticated = true;
            appState.AuthNotice = string.Empty;
        });
        accessTokenCache.StartAutoRefresh();
        dialogService.ShowSuccess("Signed in successfully");
        logger.LogInformation("HandleLoginSuccessAsync completed");
    }

    public async Task HandleLogoutAsync(CancellationToken ct = default)
    {
        Quater.Desktop.Api.Client.ApiClient.ResetUnauthorizedSignal();
        await authService.LogoutAsync(ct);
        Dispatcher.UIThread.Post(() =>
        {
            appState.CurrentUser = null;
            appState.AvailableLabs = [];
            appState.CurrentLabId = Guid.Empty;
            appState.CurrentLabName = string.Empty;
            appState.IsAuthenticated = false;
        });
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
            Dispatcher.UIThread.Post(() => appState.AuthNotice = SessionExpiredMessage);
            dialogService.ShowWarning(SessionExpiredMessage);
        }
        finally
        {
            _isHandlingUnauthorized = false;
        }
    }

    private (Guid? LabId, string LabName) ComputeDefaultLab(UserDto userInfo)
    {
        var defaultLabId = SelectDefaultLabId(userInfo);
        if (!defaultLabId.HasValue)
        {
            return (null, string.Empty);
        }

        var labName = userInfo.Labs.FirstOrDefault(lab => lab.LabId == defaultLabId)?.LabName ?? string.Empty;
        return (defaultLabId, labName);
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

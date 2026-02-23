using Microsoft.Extensions.Logging;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Core.Api;

public sealed class ApiHeaders(
    IAccessTokenCache accessTokenCache,
    AppState appState,
    ILogger<ApiHeaders> logger)
{
    public Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var token = accessTokenCache.CurrentToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            logger.LogInformation("Using cached access token");
            return Task.FromResult<string?>(token);
        }

        logger.LogInformation("No cached access token, refreshing");
        return RefreshTokenAsync(ct);
    }

    public Guid? GetLabId()
    {
        var labId = appState.CurrentLabId == Guid.Empty ? (Guid?)null : appState.CurrentLabId;
        logger.LogInformation("Providing lab id header: {LabId}", labId);
        return labId;
    }

    private async Task<string?> RefreshTokenAsync(CancellationToken ct)
    {
        logger.LogInformation("Refreshing access token for API request");
        await accessTokenCache.RefreshAsync(ct);
        if (string.IsNullOrWhiteSpace(accessTokenCache.CurrentToken))
        {
            logger.LogWarning("Access token refresh did not yield a token");
        }
        else
        {
            logger.LogInformation("Access token refresh succeeded");
        }

        return accessTokenCache.CurrentToken;
    }
}
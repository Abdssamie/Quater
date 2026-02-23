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
            return Task.FromResult<string?>(token);
        }

        logger.LogInformation("No cached access token, refreshing");
        return RefreshTokenAsync(ct);
    }

    public Guid? GetLabId() => appState.CurrentLabId == Guid.Empty ? null : appState.CurrentLabId;

    private async Task<string?> RefreshTokenAsync(CancellationToken ct)
    {
        await accessTokenCache.RefreshAsync(ct);
        var token = accessTokenCache.CurrentToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Access token refresh did not yield a token");
        }

        return token;
    }
}
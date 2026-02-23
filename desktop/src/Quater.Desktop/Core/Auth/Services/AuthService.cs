using Duende.IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;
using Quater.Desktop.Core.Auth.Storage;

namespace Quater.Desktop.Core.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly OidcClientFactory _oidcClientFactory;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<AuthService> _logger;

    public AuthService(OidcClientFactory oidcClientFactory, ITokenStore tokenStore, ILogger<AuthService> logger)
    {
        _oidcClientFactory = oidcClientFactory;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[AuthService] LoginAsync started");
        var oidcClient = _oidcClientFactory.Create();
        var result = await oidcClient.LoginAsync(new LoginRequest(), ct);
        _logger.LogInformation("[AuthService] OIDC login result: IsError={IsError}, HasAccessToken={HasToken}", 
            result.IsError, 
            !string.IsNullOrWhiteSpace(result.AccessToken));
        
        if (result.IsError)
        {
            _logger.LogWarning("[AuthService] OIDC login failed: {Error}", result.Error);
            return new AuthResult(true, result.Error, null, null, null);
        }

        _logger.LogInformation("[AuthService] Saving tokens to store...");
        await SaveTokensAsync(result.AccessToken, result.RefreshToken, result.AccessTokenExpiration.UtcDateTime, ct);
        _logger.LogInformation("[AuthService] Tokens saved successfully");

        return new AuthResult(false, null, result.AccessToken, result.RefreshToken, result.AccessTokenExpiration.UtcDateTime);
    }

    public async Task<AuthResult> RefreshAsync(CancellationToken ct = default)
    {
        var existing = await _tokenStore.GetAsync(ct);
        if (existing is null)
        {
            return new AuthResult(true, "No stored refresh token", null, null, null);
        }

        var oidcClient = _oidcClientFactory.Create();
        var result = await oidcClient.RefreshTokenAsync(existing.RefreshToken, cancellationToken: ct);
        if (result.IsError)
        {
            return new AuthResult(true, result.Error, null, null, null);
        }

        await SaveTokensAsync(result.AccessToken, result.RefreshToken, result.AccessTokenExpiration.UtcDateTime, ct);

        return new AuthResult(false, null, result.AccessToken, result.RefreshToken, result.AccessTokenExpiration.UtcDateTime);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        await _tokenStore.ClearAsync(ct);
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[AuthService] GetValidAccessTokenAsync called");
        var existing = await _tokenStore.GetAsync(ct);
        _logger.LogInformation("[AuthService] tokenStore.GetAsync returned: {Status}", existing == null ? "NULL" : $"token length {existing.AccessToken?.Length ?? 0}");
        
        if (existing is null)
        {
            _logger.LogWarning("[AuthService] No stored tokens found");
            return null;
        }

        var isValid = existing.ExpiresAtUtc > DateTime.UtcNow.AddSeconds(60);
        _logger.LogInformation("[AuthService] Token expiry check: ExpiresAt={ExpiresAt}, IsValid={IsValid}", existing.ExpiresAtUtc, isValid);
        
        if (isValid)
        {
            _logger.LogInformation("[AuthService] Returning valid access token from store");
            return existing.AccessToken;
        }

        _logger.LogInformation("[AuthService] Token expired or expiring soon, refreshing...");
        var refresh = await RefreshAsync(ct);
        _logger.LogInformation("[AuthService] Refresh result: IsError={IsError}, HasToken={HasToken}", refresh.IsError, !string.IsNullOrWhiteSpace(refresh.AccessToken));
        return refresh.IsError ? null : refresh.AccessToken;
    }

    private async Task SaveTokensAsync(string? accessToken, string? refreshToken, DateTime? expiresAt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken) || expiresAt is null)
        {
            return;
        }

        var data = new TokenData(accessToken, refreshToken, expiresAt.Value);
        await _tokenStore.SaveAsync(data, ct);
    }
}

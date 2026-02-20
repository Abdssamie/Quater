using Duende.IdentityModel.OidcClient;
using Quater.Desktop.Core.Auth.Storage;

namespace Quater.Desktop.Core.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly OidcClient _oidcClient;
    private readonly ITokenStore _tokenStore;

    public AuthService(OidcClient oidcClient, ITokenStore tokenStore)
    {
        _oidcClient = oidcClient;
        _tokenStore = tokenStore;
    }

    public async Task<AuthResult> LoginAsync(CancellationToken ct = default)
    {
        var result = await _oidcClient.LoginAsync(new LoginRequest(), ct);
        if (result.IsError)
        {
            return new AuthResult(true, result.Error, null, null, null);
        }

        await SaveTokensAsync(result.AccessToken, result.RefreshToken, result.AccessTokenExpiration.UtcDateTime, ct);

        return new AuthResult(false, null, result.AccessToken, result.RefreshToken, result.AccessTokenExpiration.UtcDateTime);
    }

    public async Task<AuthResult> RefreshAsync(CancellationToken ct = default)
    {
        var existing = await _tokenStore.GetAsync(ct);
        if (existing is null)
        {
            return new AuthResult(true, "No stored refresh token", null, null, null);
        }

        var result = await _oidcClient.RefreshTokenAsync(existing.RefreshToken, cancellationToken: ct);
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
        var existing = await _tokenStore.GetAsync(ct);
        if (existing is null)
        {
            return null;
        }

        if (existing.ExpiresAtUtc > DateTime.UtcNow.AddSeconds(60))
        {
            return existing.AccessToken;
        }

        var refresh = await RefreshAsync(ct);
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

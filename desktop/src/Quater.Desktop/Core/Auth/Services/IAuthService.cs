namespace Quater.Desktop.Core.Auth.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default);
}

namespace Quater.Desktop.Core.Api;

public interface IAccessTokenCache
{
    string? CurrentToken { get; }
    DateTime? ExpiresAtUtc { get; }
    Task InitializeAsync(CancellationToken ct = default);
    Task RefreshAsync(CancellationToken ct = default);
    void StartAutoRefresh();
    void StopAutoRefresh();
    void Clear();
}

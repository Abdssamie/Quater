using Microsoft.Extensions.Logging;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Auth.Storage;

namespace Quater.Desktop.Core.Api;

public sealed class AccessTokenCache(
    IAuthService authService,
    ITokenStore tokenStore,
    ILogger<AccessTokenCache> logger) : IAccessTokenCache
{
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FallbackDelay = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly object _sync = new();
    private CancellationTokenSource? _autoRefreshCts;
    private Task? _autoRefreshTask;
    private string? _currentToken;
    private DateTime? _expiresAtUtc;

    public string? CurrentToken
    {
        get
        {
            lock (_sync)
            {
                return _currentToken;
            }
        }
    }

    public DateTime? ExpiresAtUtc
    {
        get
        {
            lock (_sync)
            {
                return _expiresAtUtc;
            }
        }
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _refreshGate.WaitAsync(ct);
        try
        {
            var token = await authService.GetValidAccessTokenAsync(ct);
            var stored = await tokenStore.GetAsync(ct);

            lock (_sync)
            {
                _currentToken = token;
                _expiresAtUtc = stored?.ExpiresAtUtc;
            }
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await _refreshGate.WaitAsync(ct);
        try
        {
            var result = await authService.RefreshAsync(ct);

            if (result.IsError || string.IsNullOrWhiteSpace(result.AccessToken) || result.ExpiresAtUtc is null)
            {
                logger.LogWarning("Token refresh failed");
                return;
            }

            lock (_sync)
            {
                _currentToken = result.AccessToken;
                _expiresAtUtc = result.ExpiresAtUtc;
            }
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public void StartAutoRefresh()
    {
        if (_autoRefreshTask is not null)
        {
            return;
        }

        _autoRefreshCts = new CancellationTokenSource();
        _autoRefreshTask = Task.Run(() => AutoRefreshLoopAsync(_autoRefreshCts.Token));
    }

    public void StopAutoRefresh()
    {
        if (_autoRefreshCts is null)
        {
            return;
        }

        _autoRefreshCts.Cancel();
        _autoRefreshCts.Dispose();
        _autoRefreshCts = null;
        _autoRefreshTask = null;
    }

    public void Clear()
    {
        lock (_sync)
        {
            _currentToken = null;
            _expiresAtUtc = null;
        }
    }

    private async Task AutoRefreshLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var delay = GetRefreshDelay();
                if (delay is null)
                {
                    await Task.Delay(FallbackDelay, ct);
                }
                else if (delay.Value > TimeSpan.Zero)
                {
                    await Task.Delay(delay.Value, ct);
                }

                if (ct.IsCancellationRequested)
                {
                    return;
                }

                await RefreshAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private TimeSpan? GetRefreshDelay()
    {
        DateTime? expiresAt;
        lock (_sync)
        {
            expiresAt = _expiresAtUtc;
        }

        if (expiresAt is null)
        {
            return null;
        }

        var scheduledAt = expiresAt.Value.Subtract(RefreshBuffer);
        var delay = scheduledAt - DateTime.UtcNow;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }
}

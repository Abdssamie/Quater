using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Core.Api;

public sealed class ApiHeaders(IAuthService authService, AppState appState)
{
    public Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        return authService.GetValidAccessTokenAsync(ct);
    }

    public Guid? GetLabId()
    {
        return appState.CurrentLabId == Guid.Empty ? null : appState.CurrentLabId;
    }
}

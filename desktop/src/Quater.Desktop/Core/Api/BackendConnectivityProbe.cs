using Quater.Desktop.Api.Api;

namespace Quater.Desktop.Core.Api;

public sealed class BackendConnectivityProbe : IBackendConnectivityProbe
{
    public Task ProbeAsync(string backendUrl, CancellationToken ct = default)
    {
        var healthApi = new HealthApi(backendUrl);
        return healthApi.ApiHealthLiveGetAsync(cancellationToken: ct);
    }
}

namespace Quater.Desktop.Core.Api;

public interface IBackendConnectivityProbe
{
    Task ProbeAsync(string backendUrl, CancellationToken ct = default);
}

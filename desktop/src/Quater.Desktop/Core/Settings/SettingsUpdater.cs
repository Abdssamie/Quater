namespace Quater.Desktop.Core.Settings;

public sealed class SettingsUpdater(
    ISettingsStore store,
    AppSettings settings)
{
    public AppSettings Current => settings;

    public Task UpdateBackendUrlAsync(string? backendUrl, CancellationToken ct = default)
    {
        var normalized = backendUrl?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            normalized = normalized.TrimEnd('/');
        }

        if (string.Equals(settings.BackendUrl, normalized, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        settings.BackendUrl = normalized;
        var apiBasePath = settings.ApiBasePath;
        if (!string.IsNullOrWhiteSpace(apiBasePath))
        {
            var apiConfiguration = new Desktop.Api.Client.Configuration { BasePath = apiBasePath };
            Desktop.Api.Client.GlobalConfiguration.Instance = apiConfiguration;
        }
        return store.SaveAsync(settings, ct);
    }

    public Task MarkOnboardedAsync(CancellationToken ct = default)
    {
        if (settings.IsOnboarded)
        {
            return Task.CompletedTask;
        }

        settings.IsOnboarded = true;
        return store.SaveAsync(settings, ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
    {
        return store.SaveAsync(settings, ct);
    }
}

namespace Quater.Desktop.Core.Settings;

public sealed class SettingsUpdater(ISettingsStore store, AppSettings settings)
{
    public AppSettings Current => settings;

    public Task SaveAsync(CancellationToken ct = default)
    {
        return store.SaveAsync(settings, ct);
    }
}

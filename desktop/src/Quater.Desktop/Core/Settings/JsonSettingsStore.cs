using System.Text.Json;

namespace Quater.Desktop.Core.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Quater",
        "settings.json");

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        var json = await File.ReadAllTextAsync(SettingsPath, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(settings, JsonSerializerOptions.Default);
            await File.WriteAllTextAsync(SettingsPath, json, ct).ConfigureAwait(false);
        }
}

namespace Quater.Desktop.Core.Settings;

public sealed class AppSettings
{
    public const string QuaterCloudUrl = "https://cloud.quater.app";

    public string BackendUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = "quater-desktop-client";
    public Guid? LastUsedLabId { get; set; }
    public bool IsOnboarded { get; set; }

    public bool HasBackendUrl => !string.IsNullOrWhiteSpace(BackendUrl);
}

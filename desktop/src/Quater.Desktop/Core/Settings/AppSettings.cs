namespace Quater.Desktop.Core.Settings;

public sealed class AppSettings
{
    public string BackendUrl { get; set; } = "http://localhost:5000";
    public Guid? LastUsedLabId { get; set; }
}

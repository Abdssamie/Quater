namespace Quater.Desktop.Core.Settings;

public sealed class AppSettings
{
    public const string QuaterCloudUrl = "https://cloud.quater.app";

    public string BackendUrl { get; set; } = string.Empty;
    public Guid? LastUsedLabId { get; set; }
    public bool IsOnboarded { get; set; }

    public bool HasBackendUrl => !string.IsNullOrWhiteSpace(BackendUrl);

    public string ApiBasePath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(BackendUrl))
            {
                return string.Empty;
            }

            var trimmed = BackendUrl.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                return string.Empty;
            }

            var authority = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            return authority;
        }
    }
}

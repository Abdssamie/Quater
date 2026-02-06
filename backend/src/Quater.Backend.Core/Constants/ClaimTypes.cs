namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains custom claim type constants for authorization.
/// Uses enterprise-style URIs for better namespacing and interoperability.
/// </summary>
public static class QuaterClaimTypes
{
    private const string ClaimNamespace = "identity.quater.app";

    /// <summary>
    /// User's role (Admin, Technician, Viewer).
    /// </summary>
    public const string Role = $"{ClaimNamespace}/role";

    /// <summary>
    /// User's associated laboratory ID.
    /// </summary>
    public const string LabId = $"{ClaimNamespace}/lab_id";
}

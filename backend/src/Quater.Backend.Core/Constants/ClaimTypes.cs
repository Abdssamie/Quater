namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains custom claim type constants for authorization.
/// Uses enterprise-style URIs for better namespacing and interoperability.
/// </summary>
public static class QuaterClaimTypes
{
    private const string ClaimNamespace = "identity.quater.app";

    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public const string UserId = $"{ClaimNamespace}/user_id";

    /// <summary>
    /// User's email address.
    /// </summary>
    public const string Email = $"{ClaimNamespace}/email";

    /// <summary>
    /// User's role (Admin, Technician, Viewer).
    /// </summary>
    public const string Role = $"{ClaimNamespace}/role";

    /// <summary>
    /// User's associated laboratory ID.
    /// </summary>
    public const string LabId = $"{ClaimNamespace}/lab_id";

    /// <summary>
    /// User's full name.
    /// </summary>
    public const string FullName = $"{ClaimNamespace}/full_name";

    /// <summary>
    /// User's permissions (comma-separated or JSON array).
    /// </summary>
    public const string Permissions = $"{ClaimNamespace}/permissions";
}

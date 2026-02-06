namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains role name constants for authorization.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Technician = "Technician";
    public const string Viewer = "Viewer";

    /// <summary>
    /// Gets all available roles.
    /// </summary>
    public static readonly string[] All = [Admin, Technician, Viewer];
}

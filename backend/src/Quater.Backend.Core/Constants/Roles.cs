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

    /// <summary>
    /// Gets roles that can modify data (Admin and Technician).
    /// </summary>
    public static readonly string[] CanModify = [Admin, Technician];

    /// <summary>
    /// Gets roles that can only view data (all roles).
    /// </summary>
    public static readonly string[] CanView = [Admin, Technician, Viewer];
}

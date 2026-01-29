namespace Quater.Backend.Core.Enums;

/// <summary>
/// User roles for role-based access control.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Administrator with full system access
    /// </summary>
    Admin,

    /// <summary>
    /// Lab technician who can create/edit samples and test results
    /// </summary>
    Technician,

    /// <summary>
    /// Read-only viewer with no edit permissions
    /// </summary>
    Viewer
}

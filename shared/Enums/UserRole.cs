namespace Quater.Shared.Enums;

/// <summary>
/// User roles for role-based access control.
/// Explicit numeric values define the role hierarchy: Viewer (1) &lt; Technician (2) &lt; Admin (3).
/// Higher numeric values indicate higher privileges.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Read-only viewer with no edit permissions (lowest privilege level)
    /// </summary>
    Viewer = 1,

    /// <summary>
    /// Lab technician who can create/edit samples and test results (medium privilege level)
    /// </summary>
    Technician = 2,

    /// <summary>
    /// Administrator with full system access (highest privilege level)
    /// </summary>
    Admin = 3
}

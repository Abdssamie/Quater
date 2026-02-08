namespace Quater.Backend.Data.Constants;

/// <summary>
/// Constants for PostgreSQL Row-Level Security (RLS) session variables.
/// </summary>
public static class RlsConstants
{
    /// <summary>
    /// PostgreSQL session variable name for system admin flag.
    /// When set to 'true', RLS policies are bypassed.
    /// </summary>
    public const string IsSystemAdminVariable = "app.is_system_admin";

    /// <summary>
    /// PostgreSQL session variable name for current lab ID.
    /// Used by RLS policies to filter data by lab context.
    /// </summary>
    public const string CurrentLabIdVariable = "app.current_lab_id";
}

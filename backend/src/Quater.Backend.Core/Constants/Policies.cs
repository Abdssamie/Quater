namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains authorization policy name constants.
/// These must match the policy names defined in Program.cs.
/// </summary>
public static class Policies
{
    /// <summary>
    /// Requires Admin role only
    /// </summary>
    public const string AdminOnly = "AdminOnly";
    
    /// <summary>
    /// Requires Technician or Admin role
    /// </summary>
    public const string TechnicianOrAbove = "TechnicianOrAbove";
    
    /// <summary>
    /// Requires any authenticated user (Viewer, Technician, or Admin)
    /// </summary>
    public const string ViewerOrAbove = "ViewerOrAbove";
}

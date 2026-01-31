namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains authorization policy name constants.
/// </summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string TechnicianOrAdmin = "TechnicianOrAdmin";
    public const string AllRoles = "AllRoles";
    public const string CanModifyData = "CanModifyData";
    public const string CanViewData = "CanViewData";
}

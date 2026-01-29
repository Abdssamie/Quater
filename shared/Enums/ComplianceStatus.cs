namespace Quater.Shared.Enums;

/// <summary>
/// Compliance status of a test result against water quality standards.
/// </summary>
public enum ComplianceStatus
{
    /// <summary>
    /// Test result meets water quality standards
    /// </summary>
    Pass,

    /// <summary>
    /// Test result fails to meet water quality standards
    /// </summary>
    Fail,

    /// <summary>
    /// Test result is borderline or requires attention
    /// </summary>
    Warning
}

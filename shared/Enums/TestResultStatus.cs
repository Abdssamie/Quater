namespace Quater.Shared.Enums;

/// <summary>
/// Defines the lifecycle status of a TestResult.
/// Used to enforce immutability after submission for regulatory compliance.
/// </summary>
public enum TestResultStatus
{
    /// <summary>
    /// Draft result, can be modified
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Submitted result, immutable for regulatory compliance
    /// </summary>
    Submitted = 1,
    
    /// <summary>
    /// Voided result (replaced by another TestResult)
    /// </summary>
    Voided = 2
}

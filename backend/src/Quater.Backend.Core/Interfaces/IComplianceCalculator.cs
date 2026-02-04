using Quater.Shared.Enums;

namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Service for calculating water quality compliance status based on WHO and Moroccan standards
/// </summary>
public interface IComplianceCalculator
{
    /// <summary>
    /// Calculate compliance status for a test result value
    /// </summary>
    /// <param name="parameterName">Name of the water quality parameter</param>
    /// <param name="value">Measured value</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Compliance status (Pass, Warning, Fail)</returns>
    Task<ComplianceStatus> CalculateComplianceAsync(string parameterName, double value, CancellationToken ct = default);

    /// <summary>
    /// Calculate compliance status for multiple test results
    /// </summary>
    /// <param name="testResults">Dictionary of parameter names and their values</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dictionary of parameter names and their compliance status</returns>
    Task<Dictionary<string, ComplianceStatus>> CalculateBatchComplianceAsync(
        Dictionary<string, double> testResults, 
        CancellationToken ct = default);

    /// <summary>
    /// Get overall compliance status for a sample based on all its test results
    /// </summary>
    /// <param name="testResults">Dictionary of parameter names and their values</param>
    /// <returns>Overall compliance status (Fail if any fail, Warning if any warning, Pass if all pass)</returns>
    ComplianceStatus GetOverallCompliance(Dictionary<string, ComplianceStatus> testResults);
}

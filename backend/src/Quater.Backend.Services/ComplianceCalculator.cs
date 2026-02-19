using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Enums;

namespace Quater.Backend.Services;

/// <summary>
/// Service for calculating water quality compliance status based on WHO and Moroccan standards
/// </summary>
public class ComplianceCalculator(QuaterDbContext context) : IComplianceCalculator
{
    /// <summary>
    /// Calculate compliance status for a test result value based on WHO and Moroccan standards
    /// </summary>
    public async Task<ComplianceStatus> CalculateComplianceAsync(string parameterName, double value, CancellationToken ct = default)
    {
        var parameter = await context.Parameters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == parameterName && p.IsActive, ct);

        if (parameter == null)
            return ComplianceStatus.Warning;

        // Check if value is within acceptable range (hard limits)
        if (parameter.MinValue.HasValue && value < parameter.MinValue.Value)
            return ComplianceStatus.Fail;

        if (parameter.MaxValue.HasValue && value > parameter.MaxValue.Value)
            return ComplianceStatus.Fail;

        // Check WHO threshold (international standard)
        if (parameter.Threshold.HasValue && value > parameter.Threshold.Value)
            return ComplianceStatus.Fail;

        return ComplianceStatus.Pass;
    }

    /// <summary>
    /// Calculate compliance status for multiple test results
    /// </summary>
    public async Task<Dictionary<string, ComplianceStatus>> CalculateBatchComplianceAsync(
        Dictionary<string, double> testResults,
        CancellationToken ct = default)
    {
        var results = new Dictionary<string, ComplianceStatus>();

        // Extract distinct parameter names
        var parameterNames = testResults.Keys.ToList();

        // Fetch all parameters in a single query
        var parameters = await context.Parameters
            .AsNoTracking()
            .Where(p => parameterNames.Contains(p.Name) && p.IsActive)
            .ToDictionaryAsync(p => p.Name, ct);

        // Calculate compliance for each test result
        foreach (var (parameterName, value) in testResults)
        {
            if (!parameters.TryGetValue(parameterName, out var parameter))
            {
                results[parameterName] = ComplianceStatus.Warning;
                continue;
            }

            // Check if value is within acceptable range (hard limits)
            if (parameter.MinValue.HasValue && value < parameter.MinValue.Value)
            {
                results[parameterName] = ComplianceStatus.Fail;
                continue;
            }

            if (parameter.MaxValue.HasValue && value > parameter.MaxValue.Value)
            {
                results[parameterName] = ComplianceStatus.Fail;
                continue;
            }

            // Check WHO threshold
            if (parameter.Threshold.HasValue && value > parameter.Threshold.Value)
            {
                results[parameterName] = ComplianceStatus.Fail;
                continue;
            }

            results[parameterName] = ComplianceStatus.Pass;
        }

        return results;
    }

    /// <summary>
    /// Get overall compliance status for a sample based on all its test results
    /// Priority: Fail > Warning > Pass
    /// </summary>
    public ComplianceStatus GetOverallCompliance(Dictionary<string, ComplianceStatus> testResults)
    {
        if (!testResults.Any())
            return ComplianceStatus.Warning;

        // If any test fails, overall status is Fail
        if (testResults.Values.Any(s => s == ComplianceStatus.Fail))
            return ComplianceStatus.Fail;

        // If any test has warning, overall status is Warning
        if (testResults.Values.Any(s => s == ComplianceStatus.Warning))
            return ComplianceStatus.Warning;

        // All tests passed
        return ComplianceStatus.Pass;
    }
}

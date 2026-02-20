using System.Text.RegularExpressions;

namespace Quater.Backend.Api.Infrastructure;

/// <summary>
/// Transforms route parameters from PascalCase to snake_case.
/// Example: "AuditLogs" -> "audit_logs", "TestResults" -> "test_results"
/// </summary>
public sealed class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    /// <summary>
    /// Transforms the outbound route value to snake_case.
    /// </summary>
    /// <param name="value">The route value to transform.</param>
    /// <returns>The transformed snake_case string, or null if input is null.</returns>
    public string? TransformOutbound(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var str = value.ToString();
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        // Convert PascalCase to snake_case
        // Example: "AuditLogs" -> "audit_logs"
        return Regex.Replace(str, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
    }
}

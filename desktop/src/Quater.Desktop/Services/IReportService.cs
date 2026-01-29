using System.Threading;
using System.Threading.Tasks;
using Quater.Desktop.Models;

namespace Quater.Desktop.Services;

/// <summary>
/// Service for generating compliance reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates a compliance report based on the provided parameters.
    /// </summary>
    /// <param name="parameters">Report filter parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Complete report data</returns>
    Task<ComplianceReportData> GenerateComplianceReportAsync(
        ReportParameters parameters,
        CancellationToken ct);

    /// <summary>
    /// Exports a compliance report to PDF format.
    /// </summary>
    /// <param name="reportData">Report data to export</param>
    /// <param name="filePath">Output file path</param>
    /// <param name="ct">Cancellation token</param>
    Task ExportToPdfAsync(
        ComplianceReportData reportData,
        string filePath,
        CancellationToken ct);
}

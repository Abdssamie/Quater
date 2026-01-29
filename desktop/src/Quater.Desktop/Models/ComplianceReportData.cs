using System;

namespace Quater.Desktop.Models;

/// <summary>
/// Complete data structure for a compliance report.
/// </summary>
public sealed record ComplianceReportData
{
    /// <summary>
    /// Report generation timestamp
    /// </summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// Report parameters used
    /// </summary>
    public required ReportParameters Parameters { get; init; }

    /// <summary>
    /// Summary statistics
    /// </summary>
    public required ComplianceSummary Summary { get; init; }

    /// <summary>
    /// Detailed sample results
    /// </summary>
    public SampleReportItem[] Samples { get; init; } = [];

    /// <summary>
    /// Compliance trend data (grouped by date)
    /// </summary>
    public ComplianceTrendItem[] Trends { get; init; } = [];
}

/// <summary>
/// Summary statistics for the report period.
/// </summary>
public sealed record ComplianceSummary
{
    /// <summary>
    /// Total number of samples
    /// </summary>
    public required int TotalSamples { get; init; }

    /// <summary>
    /// Number of compliant samples (all tests pass)
    /// </summary>
    public required int CompliantSamples { get; init; }

    /// <summary>
    /// Number of non-compliant samples (any test fails)
    /// </summary>
    public required int NonCompliantSamples { get; init; }

    /// <summary>
    /// Number of samples with warnings
    /// </summary>
    public required int WarningSamples { get; init; }

    /// <summary>
    /// Overall compliance rate (0-100)
    /// </summary>
    public required decimal ComplianceRate { get; init; }

    /// <summary>
    /// Total number of tests performed
    /// </summary>
    public required int TotalTests { get; init; }

    /// <summary>
    /// Number of passing tests
    /// </summary>
    public required int PassingTests { get; init; }

    /// <summary>
    /// Number of failing tests
    /// </summary>
    public required int FailingTests { get; init; }
}

/// <summary>
/// Individual sample data for the report.
/// </summary>
public sealed record SampleReportItem
{
    /// <summary>
    /// Sample ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Sample type
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Collection date
    /// </summary>
    public required DateTimeOffset CollectionDate { get; init; }

    /// <summary>
    /// Location description
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Collector name
    /// </summary>
    public required string CollectorName { get; init; }

    /// <summary>
    /// Overall compliance status
    /// </summary>
    public required string ComplianceStatus { get; init; }

    /// <summary>
    /// Test results for this sample
    /// </summary>
    public TestReportItem[] TestResults { get; init; } = [];
}

/// <summary>
/// Individual test result data for the report.
/// </summary>
public sealed record TestReportItem
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public required string ParameterName { get; init; }

    /// <summary>
    /// Measured value
    /// </summary>
    public required double Value { get; init; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// Compliance status
    /// </summary>
    public required string ComplianceStatus { get; init; }

    /// <summary>
    /// Test date
    /// </summary>
    public required DateTimeOffset TestDate { get; init; }

    /// <summary>
    /// Technician name
    /// </summary>
    public required string TechnicianName { get; init; }
}

/// <summary>
/// Compliance trend data point (grouped by date).
/// </summary>
public sealed record ComplianceTrendItem
{
    /// <summary>
    /// Date for this data point
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Number of samples on this date
    /// </summary>
    public required int TotalSamples { get; init; }

    /// <summary>
    /// Number of compliant samples
    /// </summary>
    public required int CompliantSamples { get; init; }

    /// <summary>
    /// Compliance rate for this date (0-100)
    /// </summary>
    public required decimal ComplianceRate { get; init; }
}

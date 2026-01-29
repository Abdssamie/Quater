using System;

namespace Quater.Desktop.Models;

/// <summary>
/// Parameters for filtering and configuring compliance report generation.
/// </summary>
public sealed record ReportParameters
{
    /// <summary>
    /// Start date for report data (inclusive)
    /// </summary>
    public required DateTimeOffset StartDate { get; init; }

    /// <summary>
    /// End date for report data (inclusive)
    /// </summary>
    public required DateTimeOffset EndDate { get; init; }

    /// <summary>
    /// Sample types to include in report (empty = all types)
    /// </summary>
    public string[] SampleTypes { get; init; } = [];

    /// <summary>
    /// Lab ID to filter samples (null = all labs)
    /// </summary>
    public Guid? LabId { get; init; }

    /// <summary>
    /// Include only completed samples
    /// </summary>
    public bool CompletedOnly { get; init; } = true;

    /// <summary>
    /// Include archived samples
    /// </summary>
    public bool IncludeArchived { get; init; } = false;
}

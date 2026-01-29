using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using Quater.Desktop.Data;
using Quater.Desktop.Models;
using Quater.Desktop.Reports;

namespace Quater.Desktop.Services;

/// <summary>
/// Service for generating compliance reports with performance optimizations.
/// </summary>
public sealed class ReportService(
    QuaterLocalContext dbContext,
    TimeProvider timeProvider,
    ILogger<ReportService> logger) : IReportService
{
    /// <inheritdoc/>
    public async Task<ComplianceReportData> GenerateComplianceReportAsync(
        ReportParameters parameters,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        logger.LogInformation(
            "Generating compliance report from {StartDate} to {EndDate}",
            parameters.StartDate,
            parameters.EndDate);

        var startTime = timeProvider.GetTimestamp();

        // Build query with filters
        var query = dbContext.Samples
            .AsNoTracking()
            .Include(s => s.TestResults)
            .Where(s => !s.IsDeleted)
            .Where(s => s.CollectionDate >= parameters.StartDate.UtcDateTime
                     && s.CollectionDate <= parameters.EndDate.UtcDateTime);

        // Apply optional filters
        if (parameters.CompletedOnly)
        {
            query = query.Where(s => s.Status == "Completed");
        }

        if (!parameters.IncludeArchived)
        {
            query = query.Where(s => s.Status != "Archived");
        }

        if (parameters.SampleTypes.Length > 0)
        {
            query = query.Where(s => parameters.SampleTypes.Contains(s.Type));
        }

        if (parameters.LabId.HasValue)
        {
            query = query.Where(s => s.LabId == parameters.LabId.Value);
        }

        // Execute query
        var samples = await query
            .OrderBy(s => s.CollectionDate)
            .ToListAsync(ct);

        logger.LogInformation(
            "Loaded {SampleCount} samples for report",
            samples.Count);

        // Calculate summary statistics
        var summary = CalculateSummary(samples);

        // Build sample report items
        var sampleItems = samples.Select(s => new SampleReportItem
        {
            Id = s.Id,
            Type = s.Type,
            CollectionDate = new DateTimeOffset(s.CollectionDate, TimeSpan.Zero),
            Location = s.LocationDescription ?? $"{s.LocationLatitude:F6}, {s.LocationLongitude:F6}",
            CollectorName = s.CollectorName,
            ComplianceStatus = DetermineOverallComplianceStatus(s.TestResults),
            TestResults = s.TestResults
                .Where(tr => !tr.IsDeleted)
                .Select(tr => new TestReportItem
                {
                    ParameterName = tr.ParameterName,
                    Value = tr.Value,
                    Unit = tr.Unit,
                    ComplianceStatus = tr.ComplianceStatus,
                    TestDate = new DateTimeOffset(tr.TestDate, TimeSpan.Zero),
                    TechnicianName = tr.TechnicianName
                })
                .ToArray()
        }).ToArray();

        // Calculate trends
        var trends = CalculateTrends(samples);

        var elapsed = timeProvider.GetElapsedTime(startTime);
        logger.LogInformation(
            "Report generation completed in {ElapsedMs}ms",
            elapsed.TotalMilliseconds);

        return new ComplianceReportData
        {
            GeneratedAt = timeProvider.GetUtcNow(),
            Parameters = parameters,
            Summary = summary,
            Samples = sampleItems,
            Trends = trends
        };
    }

    /// <inheritdoc/>
    public async Task ExportToPdfAsync(
        ComplianceReportData reportData,
        string filePath,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reportData);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

        logger.LogInformation(
            "Exporting report to PDF: {FilePath}",
            filePath);

        var startTime = timeProvider.GetTimestamp();

        // Generate PDF document using QuestPDF fluent API
        var document = new ComplianceReportDocument(reportData);

        // Execute PDF generation on thread pool to avoid blocking UI
        await Task.Run(() =>
        {
            // QuestPDF 2025.1.0: Use extension method from QuestPDF.Fluent namespace
            document.GeneratePdf(filePath);
        }, ct);

        var elapsed = timeProvider.GetElapsedTime(startTime);
        logger.LogInformation(
            "PDF export completed in {ElapsedMs}ms",
            elapsed.TotalMilliseconds);
    }

    private static ComplianceSummary CalculateSummary(List<Data.Models.Sample> samples)
    {
        var totalSamples = samples.Count;
        var compliantSamples = 0;
        var nonCompliantSamples = 0;
        var warningSamples = 0;
        var totalTests = 0;
        var passingTests = 0;
        var failingTests = 0;

        foreach (var sample in samples)
        {
            var activeTests = sample.TestResults.Where(tr => !tr.IsDeleted).ToList();
            totalTests += activeTests.Count;

            var hasFailure = false;
            var hasWarning = false;

            foreach (var test in activeTests)
            {
                switch (test.ComplianceStatus.ToLowerInvariant())
                {
                    case "pass":
                        passingTests++;
                        break;
                    case "fail":
                        failingTests++;
                        hasFailure = true;
                        break;
                    case "warning":
                        hasWarning = true;
                        break;
                }
            }

            if (hasFailure)
            {
                nonCompliantSamples++;
            }
            else if (hasWarning)
            {
                warningSamples++;
            }
            else if (activeTests.Count > 0)
            {
                compliantSamples++;
            }
        }

        var complianceRate = totalSamples > 0
            ? Math.Round((decimal)compliantSamples / totalSamples * 100, 2)
            : 0m;

        return new ComplianceSummary
        {
            TotalSamples = totalSamples,
            CompliantSamples = compliantSamples,
            NonCompliantSamples = nonCompliantSamples,
            WarningSamples = warningSamples,
            ComplianceRate = complianceRate,
            TotalTests = totalTests,
            PassingTests = passingTests,
            FailingTests = failingTests
        };
    }

    private static string DetermineOverallComplianceStatus(ICollection<Data.Models.TestResult> testResults)
    {
        var activeTests = testResults.Where(tr => !tr.IsDeleted).ToList();

        if (activeTests.Count == 0)
        {
            return "Pending";
        }

        var hasFailure = activeTests.Any(tr => tr.ComplianceStatus.Equals("Fail", StringComparison.OrdinalIgnoreCase));
        if (hasFailure)
        {
            return "Non-Compliant";
        }

        var hasWarning = activeTests.Any(tr => tr.ComplianceStatus.Equals("Warning", StringComparison.OrdinalIgnoreCase));
        if (hasWarning)
        {
            return "Warning";
        }

        return "Compliant";
    }

    private static ComplianceTrendItem[] CalculateTrends(List<Data.Models.Sample> samples)
    {
        return samples
            .GroupBy(s => DateOnly.FromDateTime(s.CollectionDate.Date))
            .Select(g =>
            {
                var totalSamples = g.Count();
                var compliantSamples = g.Count(s =>
                {
                    var activeTests = s.TestResults.Where(tr => !tr.IsDeleted).ToList();
                    return activeTests.Count > 0 &&
                           activeTests.All(tr => tr.ComplianceStatus.Equals("Pass", StringComparison.OrdinalIgnoreCase));
                });

                var complianceRate = totalSamples > 0
                    ? Math.Round((decimal)compliantSamples / totalSamples * 100, 2)
                    : 0m;

                return new ComplianceTrendItem
                {
                    Date = g.Key,
                    TotalSamples = totalSamples,
                    CompliantSamples = compliantSamples,
                    ComplianceRate = complianceRate
                };
            })
            .OrderBy(t => t.Date)
            .ToArray();
    }
}

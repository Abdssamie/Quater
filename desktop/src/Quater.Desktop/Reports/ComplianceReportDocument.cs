using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Quater.Desktop.Models;

namespace Quater.Desktop.Reports;

/// <summary>
/// QuestPDF document for compliance reports.
/// Generates professional PDF reports with summary, details, and trends.
/// </summary>
public sealed class ComplianceReportDocument(ComplianceReportData reportData) : IDocument
{
    private readonly ComplianceReportData _reportData = reportData ?? throw new ArgumentNullException(nameof(reportData));

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Water Quality Compliance Report")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("Report Period: ").SemiBold();
                    text.Span($"{_reportData.Parameters.StartDate:yyyy-MM-dd} to {_reportData.Parameters.EndDate:yyyy-MM-dd}");
                });

                column.Item().Text(text =>
                {
                    text.Span("Generated: ").SemiBold();
                    text.Span($"{_reportData.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(15);

            // Summary Section
            column.Item().Element(ComposeSummary);

            // Compliance Trends Section
            if (_reportData.Trends.Length > 0)
            {
                column.Item().Element(ComposeTrends);
            }

            // Detailed Results Section
            if (_reportData.Samples.Length > 0)
            {
                column.Item().Element(ComposeDetailedResults);
            }
            else
            {
                column.Item().Text("No samples found for the selected period.")
                    .FontSize(12)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private void ComposeSummary(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Summary Statistics")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5)
                        .Text("Metric").SemiBold();
                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5)
                        .Text("Value").SemiBold();
                });

                // Data rows
                var summary = _reportData.Summary;

                AddSummaryRow(table, "Total Samples", summary.TotalSamples.ToString());
                AddSummaryRow(table, "Compliant Samples", summary.CompliantSamples.ToString(), Colors.Green.Lighten3);
                AddSummaryRow(table, "Non-Compliant Samples", summary.NonCompliantSamples.ToString(), Colors.Red.Lighten3);
                AddSummaryRow(table, "Warning Samples", summary.WarningSamples.ToString(), Colors.Orange.Lighten3);
                AddSummaryRow(table, "Compliance Rate", $"{summary.ComplianceRate:F2}%", GetComplianceRateColor(summary.ComplianceRate));
                AddSummaryRow(table, "Total Tests Performed", summary.TotalTests.ToString());
                AddSummaryRow(table, "Passing Tests", summary.PassingTests.ToString());
                AddSummaryRow(table, "Failing Tests", summary.FailingTests.ToString());
            });
        });
    }

    private void ComposeTrends(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Compliance Trends")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5)
                        .Text("Date").SemiBold();
                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5)
                        .Text("Total Samples").SemiBold();
                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5)
                        .Text("Compliant").SemiBold();
                    header.Cell().Background(Colors.Blue.Lighten3).Padding(5)
                        .Text("Rate").SemiBold();
                });

                // Data rows
                foreach (var trend in _reportData.Trends)
                {
                    table.Cell().Padding(5).Text(trend.Date.ToString("yyyy-MM-dd"));
                    table.Cell().Padding(5).Text(trend.TotalSamples.ToString());
                    table.Cell().Padding(5).Text(trend.CompliantSamples.ToString());
                    table.Cell().Padding(5).Background(GetComplianceRateColor(trend.ComplianceRate))
                        .Text($"{trend.ComplianceRate:F1}%");
                }
            });
        });
    }

    private void ComposeDetailedResults(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Detailed Sample Results")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(10).Column(detailColumn =>
            {
                foreach (var sample in _reportData.Samples)
                {
                    detailColumn.Item().PaddingBottom(10).Element(c => ComposeSampleDetail(c, sample));
                }
            });
        });
    }

    private void ComposeSampleDetail(IContainer container, SampleReportItem sample)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            // Sample header
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Sample: ").SemiBold();
                    text.Span($"{sample.Type} - {sample.Location}");
                });

                row.AutoItem().Background(GetComplianceStatusColor(sample.ComplianceStatus))
                    .Padding(5)
                    .Text(sample.ComplianceStatus)
                    .FontSize(9)
                    .Bold();
            });

            column.Item().PaddingTop(5).Text(text =>
            {
                text.Span("Collected: ").SemiBold().FontSize(9);
                text.Span($"{sample.CollectionDate:yyyy-MM-dd HH:mm} by {sample.CollectorName}").FontSize(9);
            });

            // Test results table
            if (sample.TestResults.Length > 0)
            {
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("Parameter").FontSize(9).SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("Value").FontSize(9).SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("Unit").FontSize(9).SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("Status").FontSize(9).SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("Technician").FontSize(9).SemiBold();
                    });

                    // Data rows
                    foreach (var test in sample.TestResults)
                    {
                        table.Cell().Padding(3).Text(test.ParameterName).FontSize(9);
                        table.Cell().Padding(3).Text($"{test.Value:F2}").FontSize(9);
                        table.Cell().Padding(3).Text(test.Unit).FontSize(9);
                        table.Cell().Padding(3).Background(GetComplianceStatusColor(test.ComplianceStatus))
                            .Text(test.ComplianceStatus).FontSize(9);
                        table.Cell().Padding(3).Text(test.TechnicianName).FontSize(9);
                    }
                });
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
            text.Span(" ").FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private void AddSummaryRow(TableDescriptor table, string metric, string value, string? backgroundColor = null)
    {
        table.Cell().Padding(5).Text(metric);
        
        if (backgroundColor != null)
        {
            table.Cell().Padding(5).Background(backgroundColor).Text(value).SemiBold();
        }
        else
        {
            table.Cell().Padding(5).Text(value).SemiBold();
        }
    }

    private static string GetComplianceRateColor(decimal rate) => rate switch
    {
        >= 95 => Colors.Green.Lighten3,
        >= 80 => Colors.Yellow.Lighten3,
        >= 60 => Colors.Orange.Lighten3,
        _ => Colors.Red.Lighten3
    };

    private static string GetComplianceStatusColor(string status) => status.ToLowerInvariant() switch
    {
        "compliant" or "pass" => Colors.Green.Lighten3,
        "non-compliant" or "fail" => Colors.Red.Lighten3,
        "warning" => Colors.Orange.Lighten3,
        _ => Colors.Grey.Lighten3
    };
}

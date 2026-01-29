using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Quater.Desktop.Models;
using Quater.Desktop.Services;

namespace Quater.Desktop.ViewModels;

/// <summary>
/// ViewModel for compliance report generation and export.
/// Implements Quater-omf: Desktop Report Generation with QuestPDF
/// </summary>
public sealed partial class ReportViewModel(
    IReportService reportService,
    TimeProvider timeProvider,
    ILogger<ReportViewModel> logger) : ViewModelBase
{
    private readonly IReportService _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<ReportViewModel> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [ObservableProperty]
    private DateTimeOffset _startDate = DateTimeOffset.UtcNow.AddMonths(-1);

    [ObservableProperty]
    private DateTimeOffset _endDate = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private bool _completedOnly = true;

    [ObservableProperty]
    private bool _includeArchived = false;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _hasReportData;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ComplianceReportData? _reportData;

    [ObservableProperty]
    private string _generationTimeMs = string.Empty;

    /// <summary>
    /// Available sample types for filtering
    /// </summary>
    public ObservableCollection<SampleTypeFilter> SampleTypes { get; } =
    [
        new SampleTypeFilter { Name = "Drinking Water", Value = "DrinkingWater", IsSelected = false },
        new SampleTypeFilter { Name = "Wastewater", Value = "Wastewater", IsSelected = false },
        new SampleTypeFilter { Name = "Surface Water", Value = "SurfaceWater", IsSelected = false },
        new SampleTypeFilter { Name = "Groundwater", Value = "Groundwater", IsSelected = false },
        new SampleTypeFilter { Name = "Industrial Water", Value = "IndustrialWater", IsSelected = false }
    ];

    /// <summary>
    /// Generate compliance report based on current parameters
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private async Task GenerateReportAsync(CancellationToken ct)
    {
        try
        {
            IsGenerating = true;
            HasReportData = false;
            StatusMessage = "Generating report...";
            _logger.LogInformation("Starting report generation");

            var startTime = _timeProvider.GetTimestamp();

            // Build parameters
            var selectedTypes = SampleTypes
                .Where(st => st.IsSelected)
                .Select(st => st.Value)
                .ToArray();

            var parameters = new ReportParameters
            {
                StartDate = StartDate,
                EndDate = EndDate,
                SampleTypes = selectedTypes,
                LabId = null, // TODO: Get from current user context
                CompletedOnly = CompletedOnly,
                IncludeArchived = IncludeArchived
            };

            // Generate report
            ReportData = await _reportService.GenerateComplianceReportAsync(parameters, ct);

            var elapsed = _timeProvider.GetElapsedTime(startTime);
            GenerationTimeMs = $"{elapsed.TotalMilliseconds:F0}";

            HasReportData = true;
            StatusMessage = $"Report generated successfully in {GenerationTimeMs}ms. " +
                          $"Found {ReportData.Summary.TotalSamples} samples.";

            _logger.LogInformation(
                "Report generated: {SampleCount} samples, {TestCount} tests, {ElapsedMs}ms",
                ReportData.Summary.TotalSamples,
                ReportData.Summary.TotalTests,
                elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Report generation cancelled.";
            _logger.LogInformation("Report generation cancelled by user");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error generating report: {ex.Message}";
            _logger.LogError(ex, "Failed to generate report");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private bool CanGenerateReport() => !IsGenerating && StartDate <= EndDate;

    /// <summary>
    /// Export report to PDF file
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExportToPdf))]
    private async Task ExportToPdfAsync(CancellationToken ct)
    {
        if (ReportData is null)
        {
            StatusMessage = "No report data to export.";
            return;
        }

        try
        {
            IsGenerating = true;
            StatusMessage = "Exporting to PDF...";

            // Generate default filename
            var defaultFileName = $"WaterQualityReport_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.pdf";

            // In a real implementation, you would use Avalonia's file picker
            // For now, save to a default location
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, "QuaterReports", defaultFileName);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _logger.LogInformation("Exporting report to: {FilePath}", filePath);

            var startTime = _timeProvider.GetTimestamp();

            await _reportService.ExportToPdfAsync(ReportData, filePath, ct);

            var elapsed = _timeProvider.GetElapsedTime(startTime);

            StatusMessage = $"Report exported successfully to: {filePath} ({elapsed.TotalMilliseconds:F0}ms)";

            _logger.LogInformation(
                "Report exported: {FilePath}, {ElapsedMs}ms",
                filePath,
                elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Export cancelled.";
            _logger.LogInformation("Report export cancelled by user");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting report: {ex.Message}";
            _logger.LogError(ex, "Failed to export report to PDF");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private bool CanExportToPdf() => !IsGenerating && HasReportData && ReportData is not null;

    /// <summary>
    /// Clear current report data
    /// </summary>
    [RelayCommand]
    private void ClearReport()
    {
        ReportData = null;
        HasReportData = false;
        StatusMessage = "Report cleared.";
        GenerationTimeMs = string.Empty;
    }

    /// <summary>
    /// Reset filters to defaults
    /// </summary>
    [RelayCommand]
    private void ResetFilters()
    {
        StartDate = DateTimeOffset.UtcNow.AddMonths(-1);
        EndDate = DateTimeOffset.UtcNow;
        CompletedOnly = true;
        IncludeArchived = false;

        foreach (var sampleType in SampleTypes)
        {
            sampleType.IsSelected = false;
        }

        StatusMessage = "Filters reset to defaults.";
    }

    partial void OnStartDateChanged(DateTimeOffset value)
    {
        GenerateReportCommand.NotifyCanExecuteChanged();
    }

    partial void OnEndDateChanged(DateTimeOffset value)
    {
        GenerateReportCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsGeneratingChanged(bool value)
    {
        GenerateReportCommand.NotifyCanExecuteChanged();
        ExportToPdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasReportDataChanged(bool value)
    {
        ExportToPdfCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>
/// Sample type filter item for UI binding
/// </summary>
public sealed partial class SampleTypeFilter : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

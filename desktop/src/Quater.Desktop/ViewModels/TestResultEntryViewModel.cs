using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Quater.Desktop.Data;
using Quater.Desktop.Data.Models;

namespace Quater.Desktop.ViewModels;

/// <summary>
/// ViewModel for entering test results.
/// Implements Quater-2dw: Desktop Test Result Entry View
/// </summary>
public partial class TestResultEntryViewModel : ViewModelBase
{
    private readonly QuaterLocalContext _context;
    private Guid? _testResultId;
    private TestResult? _originalTestResult;
    private Parameter? _selectedParameter;

    [ObservableProperty]
    private Guid _sampleId;

    [ObservableProperty]
    private string _parameterName = string.Empty;

    [ObservableProperty]
    private double _value;

    [ObservableProperty]
    private string _unit = string.Empty;

    [ObservableProperty]
    private DateTime _testDate = DateTime.UtcNow;

    [ObservableProperty]
    private string _technicianName = string.Empty;

    [ObservableProperty]
    private string _testMethod = "Titration";

    [ObservableProperty]
    private string _complianceStatus = "Pass";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isNewTestResult = true;

    [ObservableProperty]
    private List<Parameter> _availableParameters = new();

    /// <summary>
    /// Available test methods
    /// </summary>
    public List<string> TestMethods { get; } = new()
    {
        "Titration",
        "Spectrophotometry",
        "Chromatography",
        "Microscopy",
        "Electrode",
        "Culture",
        "Other"
    };

    /// <summary>
    /// Compliance status options
    /// </summary>
    public List<string> ComplianceStatuses { get; } = new()
    {
        "Pass",
        "Fail",
        "Warning"
    };

    public TestResultEntryViewModel(QuaterLocalContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Initialize for a new test result
    /// </summary>
    public async Task InitializeNewAsync(Guid sampleId, string userId)
    {
        _testResultId = null;
        IsNewTestResult = true;
        _originalTestResult = null;
        SampleId = sampleId;

        // Set defaults
        ParameterName = string.Empty;
        Value = 0;
        Unit = string.Empty;
        TestDate = DateTime.UtcNow;
        TechnicianName = string.Empty;
        TestMethod = "Titration";
        ComplianceStatus = "Pass";
        ErrorMessage = string.Empty;

        // Load available parameters
        await LoadParametersAsync();
    }

    /// <summary>
    /// Load an existing test result for editing
    /// </summary>
    public async Task LoadAsync(Guid testResultId)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            _testResultId = testResultId;
            IsNewTestResult = false;

            var testResult = await _context.TestResults
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.Id == testResultId);

            if (testResult == null)
            {
                ErrorMessage = "Test result not found.";
                return;
            }

            // Store original for change tracking
            _originalTestResult = testResult;

            // Populate properties
            SampleId = testResult.SampleId;
            ParameterName = testResult.ParameterName;
            Value = testResult.Value;
            Unit = testResult.Unit;
            TestDate = testResult.TestDate;
            TechnicianName = testResult.TechnicianName;
            TestMethod = testResult.TestMethod;
            ComplianceStatus = testResult.ComplianceStatus;

            // Load parameters
            await LoadParametersAsync();

            // Load selected parameter
            _selectedParameter = AvailableParameters.FirstOrDefault(p => p.Name == ParameterName);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading test result: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Load available parameters from database
    /// </summary>
    private async Task LoadParametersAsync()
    {
        try
        {
            var parameters = await _context.Parameters
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            AvailableParameters = parameters;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading parameters: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle parameter selection change
    /// </summary>
    partial void OnParameterNameChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _selectedParameter = null;
            Unit = string.Empty;
            return;
        }

        _selectedParameter = AvailableParameters.FirstOrDefault(p => p.Name == value);
        if (_selectedParameter != null)
        {
            Unit = _selectedParameter.Unit;

            // Recalculate compliance status if value is set
            if (Value > 0)
            {
                CalculateComplianceStatus();
            }
        }
    }

    /// <summary>
    /// Handle value change
    /// </summary>
    partial void OnValueChanged(double value)
    {
        if (_selectedParameter != null && value > 0)
        {
            CalculateComplianceStatus();
        }
    }

    /// <summary>
    /// Auto-calculate compliance status based on parameter thresholds
    /// </summary>
    private void CalculateComplianceStatus()
    {
        if (_selectedParameter == null)
        {
            ComplianceStatus = "Pass";
            return;
        }

        // Check WHO threshold
        if (_selectedParameter.WhoThreshold.HasValue)
        {
            var threshold = _selectedParameter.WhoThreshold.Value;

            // For parameters like E. coli where 0 is the threshold
            if (threshold == 0)
            {
                ComplianceStatus = Value == 0 ? "Pass" : "Fail";
                return;
            }

            // For pH (range: 6.5-8.5)
            if (_selectedParameter.Name.Equals("pH", StringComparison.OrdinalIgnoreCase))
            {
                if (Value >= 6.5 && Value <= 8.5)
                {
                    ComplianceStatus = "Pass";
                }
                else if (Value >= 6.0 && Value < 6.5 || Value > 8.5 && Value <= 9.0)
                {
                    ComplianceStatus = "Warning";
                }
                else
                {
                    ComplianceStatus = "Fail";
                }
                return;
            }

            // For other parameters (threshold is maximum)
            if (Value <= threshold)
            {
                ComplianceStatus = "Pass";
            }
            else if (Value <= threshold * 1.2) // 20% tolerance for warning
            {
                ComplianceStatus = "Warning";
            }
            else
            {
                ComplianceStatus = "Fail";
            }
            return;
        }

        // Check Moroccan threshold if WHO not available
        if (_selectedParameter.MoroccanThreshold.HasValue)
        {
            var threshold = _selectedParameter.MoroccanThreshold.Value;

            if (Value <= threshold)
            {
                ComplianceStatus = "Pass";
            }
            else if (Value <= threshold * 1.2)
            {
                ComplianceStatus = "Warning";
            }
            else
            {
                ComplianceStatus = "Fail";
            }
            return;
        }

        // No threshold defined - default to Pass
        ComplianceStatus = "Pass";
    }

    /// <summary>
    /// Validate the test result data
    /// </summary>
    private bool ValidateTestResult(out string validationError)
    {
        validationError = string.Empty;

        // Validate SampleId
        if (SampleId == Guid.Empty)
        {
            validationError = "Sample ID is required.";
            return false;
        }

        // Validate ParameterName
        if (string.IsNullOrWhiteSpace(ParameterName))
        {
            validationError = "Parameter name is required.";
            return false;
        }

        if (ParameterName.Length > 100)
        {
            validationError = "Parameter name must not exceed 100 characters.";
            return false;
        }

        // Check if parameter exists
        if (!AvailableParameters.Any(p => p.Name == ParameterName))
        {
            validationError = "Selected parameter does not exist in the database.";
            return false;
        }

        // Validate Value
        if (Value < 0)
        {
            validationError = "Value must be greater than or equal to 0.";
            return false;
        }

        // Validate against parameter min/max if defined
        if (_selectedParameter != null)
        {
            if (_selectedParameter.MinValue.HasValue && Value < _selectedParameter.MinValue.Value)
            {
                validationError = $"Value must be at least {_selectedParameter.MinValue.Value}.";
                return false;
            }

            if (_selectedParameter.MaxValue.HasValue && Value > _selectedParameter.MaxValue.Value)
            {
                validationError = $"Value must not exceed {_selectedParameter.MaxValue.Value}.";
                return false;
            }
        }

        // Validate Unit
        if (string.IsNullOrWhiteSpace(Unit))
        {
            validationError = "Unit is required.";
            return false;
        }

        if (Unit.Length > 20)
        {
            validationError = "Unit must not exceed 20 characters.";
            return false;
        }

        // Validate TechnicianName
        if (string.IsNullOrWhiteSpace(TechnicianName))
        {
            validationError = "Technician name is required.";
            return false;
        }

        if (TechnicianName.Length > 100)
        {
            validationError = "Technician name must not exceed 100 characters.";
            return false;
        }

        // Validate TestMethod
        if (!TestMethods.Contains(TestMethod))
        {
            validationError = "Invalid test method.";
            return false;
        }

        // Validate ComplianceStatus
        if (!ComplianceStatuses.Contains(ComplianceStatus))
        {
            validationError = "Invalid compliance status.";
            return false;
        }

        // Validate TestDate
        if (TestDate > DateTime.UtcNow)
        {
            validationError = "Test date cannot be in the future.";
            return false;
        }

        // Validate TestDate >= Sample.CollectionDate
        var sample = _context.Samples.AsNoTracking().FirstOrDefault(s => s.Id == SampleId);
        if (sample != null && TestDate < sample.CollectionDate)
        {
            validationError = "Test date must be on or after the sample collection date.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Save the test result (create or update)
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // Validate
            if (!ValidateTestResult(out string validationError))
            {
                ErrorMessage = validationError;
                return;
            }

            // TODO: Get current user from authentication service
            var currentUserId = "system"; // Placeholder

            if (IsNewTestResult)
            {
                // Create new test result
                var testResult = new TestResult
                {
                    Id = Guid.NewGuid(),
                    SampleId = SampleId,
                    ParameterName = ParameterName,
                    Value = Value,
                    Unit = Unit,
                    TestDate = TestDate,
                    TechnicianName = TechnicianName,
                    TestMethod = TestMethod,
                    ComplianceStatus = ComplianceStatus,
                    Version = 1,
                    LastModified = DateTime.UtcNow,
                    LastModifiedBy = currentUserId,
                    IsDeleted = false,
                    IsSynced = false,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.TestResults.Add(testResult);
                await _context.SaveChangesAsync();

                _testResultId = testResult.Id;
                IsNewTestResult = false;
                _originalTestResult = testResult;

                ErrorMessage = "Test result created successfully.";
            }
            else
            {
                // Update existing test result
                if (_testResultId == null || _originalTestResult == null)
                {
                    ErrorMessage = "Cannot update: test result not loaded.";
                    return;
                }

                var testResult = await _context.TestResults.FindAsync(_testResultId.Value);
                if (testResult == null)
                {
                    ErrorMessage = "Test result not found.";
                    return;
                }

                // Check for concurrent modifications (optimistic locking)
                if (testResult.Version != _originalTestResult.Version ||
                    testResult.LastModified != _originalTestResult.LastModified)
                {
                    ErrorMessage = "Test result was modified by another user. Please reload and try again.";
                    return;
                }

                // Update properties
                testResult.SampleId = SampleId;
                testResult.ParameterName = ParameterName;
                testResult.Value = Value;
                testResult.Unit = Unit;
                testResult.TestDate = TestDate;
                testResult.TechnicianName = TechnicianName;
                testResult.TestMethod = TestMethod;
                testResult.ComplianceStatus = ComplianceStatus;
                testResult.Version++;
                testResult.LastModified = DateTime.UtcNow;
                testResult.LastModifiedBy = currentUserId;
                testResult.IsSynced = false;

                try
                {
                    await _context.SaveChangesAsync();
                    _originalTestResult = testResult;
                    ErrorMessage = "Test result updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    ErrorMessage = "Concurrency conflict detected. Please reload and try again.";
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving test result: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Cancel changes and revert to original values
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        if (_originalTestResult != null)
        {
            // Revert to original values
            SampleId = _originalTestResult.SampleId;
            ParameterName = _originalTestResult.ParameterName;
            Value = _originalTestResult.Value;
            Unit = _originalTestResult.Unit;
            TestDate = _originalTestResult.TestDate;
            TechnicianName = _originalTestResult.TechnicianName;
            TestMethod = _originalTestResult.TestMethod;
            ComplianceStatus = _originalTestResult.ComplianceStatus;
            ErrorMessage = "Changes cancelled.";
        }
        else
        {
            // Clear form for new test result
            ParameterName = string.Empty;
            Value = 0;
            Unit = string.Empty;
            TestDate = DateTime.UtcNow;
            TechnicianName = string.Empty;
            TestMethod = "Titration";
            ComplianceStatus = "Pass";
            ErrorMessage = string.Empty;
        }
    }
}

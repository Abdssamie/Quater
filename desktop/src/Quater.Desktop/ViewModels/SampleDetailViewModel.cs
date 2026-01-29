using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Quater.Desktop.Data;
using Quater.Desktop.Data.Models;

namespace Quater.Desktop.ViewModels;

/// <summary>
/// ViewModel for viewing and editing sample details.
/// Implements Quater-1js: Desktop Sample Detail View
/// </summary>
public partial class SampleDetailViewModel : ViewModelBase
{
    private readonly QuaterLocalContext _context;
    private Guid? _sampleId;
    private Sample? _originalSample;

    [ObservableProperty]
    private string _type = "DrinkingWater";

    [ObservableProperty]
    private double _locationLatitude;

    [ObservableProperty]
    private double _locationLongitude;

    [ObservableProperty]
    private string _locationDescription = string.Empty;

    [ObservableProperty]
    private string _locationHierarchy = string.Empty;

    [ObservableProperty]
    private DateTime _collectionDate = DateTime.UtcNow;

    [ObservableProperty]
    private string _collectorName = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _status = "Pending";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isNewSample = true;

    /// <summary>
    /// Available sample types
    /// </summary>
    public List<string> SampleTypes { get; } = new()
    {
        "DrinkingWater",
        "Wastewater",
        "SurfaceWater",
        "Groundwater",
        "IndustrialWater"
    };

    /// <summary>
    /// Available sample statuses
    /// </summary>
    public List<string> SampleStatuses { get; } = new()
    {
        "Pending",
        "Completed",
        "Archived"
    };

    public SampleDetailViewModel(QuaterLocalContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Load an existing sample for editing
    /// </summary>
    public async Task LoadAsync(Guid sampleId)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            _sampleId = sampleId;
            IsNewSample = false;

            var sample = await _context.Samples
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sampleId);

            if (sample == null)
            {
                ErrorMessage = "Sample not found.";
                return;
            }

            // Store original for change tracking
            _originalSample = sample;

            // Populate properties
            Type = sample.Type;
            LocationLatitude = sample.LocationLatitude;
            LocationLongitude = sample.LocationLongitude;
            LocationDescription = sample.LocationDescription ?? string.Empty;
            LocationHierarchy = sample.LocationHierarchy ?? string.Empty;
            CollectionDate = sample.CollectionDate;
            CollectorName = sample.CollectorName;
            Notes = sample.Notes ?? string.Empty;
            Status = sample.Status;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading sample: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Initialize a new sample
    /// </summary>
    public void InitializeNew(Guid labId, string userId)
    {
        _sampleId = null;
        IsNewSample = true;
        _originalSample = null;

        // Set defaults
        Type = "DrinkingWater";
        LocationLatitude = 0;
        LocationLongitude = 0;
        LocationDescription = string.Empty;
        LocationHierarchy = string.Empty;
        CollectionDate = DateTime.UtcNow;
        CollectorName = string.Empty;
        Notes = string.Empty;
        Status = "Pending";
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Validate the sample data
    /// </summary>
    private bool ValidateSample(out string validationError)
    {
        validationError = string.Empty;

        // Validate Type
        if (!SampleTypes.Contains(Type))
        {
            validationError = "Invalid sample type.";
            return false;
        }

        // Validate Latitude
        if (LocationLatitude < -90 || LocationLatitude > 90)
        {
            validationError = "Latitude must be between -90 and 90.";
            return false;
        }

        // Validate Longitude
        if (LocationLongitude < -180 || LocationLongitude > 180)
        {
            validationError = "Longitude must be between -180 and 180.";
            return false;
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(CollectorName))
        {
            validationError = "Collector name is required.";
            return false;
        }

        if (CollectorName.Length > 100)
        {
            validationError = "Collector name must not exceed 100 characters.";
            return false;
        }

        // Validate Status
        if (!SampleStatuses.Contains(Status))
        {
            validationError = "Invalid sample status.";
            return false;
        }

        // Validate max lengths
        if (!string.IsNullOrEmpty(LocationDescription) && LocationDescription.Length > 200)
        {
            validationError = "Location description must not exceed 200 characters.";
            return false;
        }

        if (!string.IsNullOrEmpty(LocationHierarchy) && LocationHierarchy.Length > 500)
        {
            validationError = "Location hierarchy must not exceed 500 characters.";
            return false;
        }

        if (!string.IsNullOrEmpty(Notes) && Notes.Length > 1000)
        {
            validationError = "Notes must not exceed 1000 characters.";
            return false;
        }

        // Validate collection date
        if (CollectionDate > DateTime.UtcNow)
        {
            validationError = "Collection date cannot be in the future.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Save the sample (create or update)
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // Validate
            if (!ValidateSample(out string validationError))
            {
                ErrorMessage = validationError;
                return;
            }

            // TODO: Get current user and lab from authentication service
            var currentUserId = "system"; // Placeholder
            var currentLabId = Guid.NewGuid(); // Placeholder

            if (IsNewSample)
            {
                // Create new sample
                var sample = new Sample
                {
                    Id = Guid.NewGuid(),
                    Type = Type,
                    LocationLatitude = LocationLatitude,
                    LocationLongitude = LocationLongitude,
                    LocationDescription = string.IsNullOrWhiteSpace(LocationDescription) ? null : LocationDescription,
                    LocationHierarchy = string.IsNullOrWhiteSpace(LocationHierarchy) ? null : LocationHierarchy,
                    CollectionDate = CollectionDate,
                    CollectorName = CollectorName,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                    Status = Status,
                    Version = 1,
                    LastModified = DateTime.UtcNow,
                    LastModifiedBy = currentUserId,
                    IsDeleted = false,
                    IsSynced = false,
                    LabId = currentLabId,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Samples.Add(sample);
                await _context.SaveChangesAsync();

                _sampleId = sample.Id;
                IsNewSample = false;
                _originalSample = sample;

                ErrorMessage = "Sample created successfully.";
            }
            else
            {
                // Update existing sample
                if (_sampleId == null || _originalSample == null)
                {
                    ErrorMessage = "Cannot update: sample not loaded.";
                    return;
                }

                var sample = await _context.Samples.FindAsync(_sampleId.Value);
                if (sample == null)
                {
                    ErrorMessage = "Sample not found.";
                    return;
                }

                // Check for concurrent modifications (optimistic locking)
                if (sample.Version != _originalSample.Version ||
                    sample.LastModified != _originalSample.LastModified)
                {
                    ErrorMessage = "Sample was modified by another user. Please reload and try again.";
                    return;
                }

                // Update properties
                sample.Type = Type;
                sample.LocationLatitude = LocationLatitude;
                sample.LocationLongitude = LocationLongitude;
                sample.LocationDescription = string.IsNullOrWhiteSpace(LocationDescription) ? null : LocationDescription;
                sample.LocationHierarchy = string.IsNullOrWhiteSpace(LocationHierarchy) ? null : LocationHierarchy;
                sample.CollectionDate = CollectionDate;
                sample.CollectorName = CollectorName;
                sample.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes;
                sample.Status = Status;
                sample.Version++;
                sample.LastModified = DateTime.UtcNow;
                sample.LastModifiedBy = currentUserId;
                sample.IsSynced = false;

                try
                {
                    await _context.SaveChangesAsync();
                    _originalSample = sample;
                    ErrorMessage = "Sample updated successfully.";
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
            ErrorMessage = $"Error saving sample: {ex.Message}";
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
        if (_originalSample != null)
        {
            // Revert to original values
            Type = _originalSample.Type;
            LocationLatitude = _originalSample.LocationLatitude;
            LocationLongitude = _originalSample.LocationLongitude;
            LocationDescription = _originalSample.LocationDescription ?? string.Empty;
            LocationHierarchy = _originalSample.LocationHierarchy ?? string.Empty;
            CollectionDate = _originalSample.CollectionDate;
            CollectorName = _originalSample.CollectorName;
            Notes = _originalSample.Notes ?? string.Empty;
            Status = _originalSample.Status;
            ErrorMessage = "Changes cancelled.";
        }
        else
        {
            // Clear form for new sample
            InitializeNew(Guid.Empty, "system");
        }
    }
}

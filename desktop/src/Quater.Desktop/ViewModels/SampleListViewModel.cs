using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Shared.Models;
using Quater.Shared.Enums;
using Quater.Desktop.Data.Repositories;

namespace Quater.Desktop.ViewModels;

public partial class SampleListViewModel : ObservableObject
{
    private readonly ISampleRepository _sampleRepository;

    [ObservableProperty]
    private ObservableCollection<Sample> _samples = [];

    [ObservableProperty]
    private Sample? _selectedSample;

    [ObservableProperty]
    private SampleStatus? _statusFilter;

    [ObservableProperty]
    private DateTime? _startDateFilter;

    [ObservableProperty]
    private DateTime? _endDateFilter;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalCount;

    public SampleListViewModel(ISampleRepository sampleRepository)
    {
        _sampleRepository = sampleRepository;
    }

    [RelayCommand]
    private async Task LoadSamplesAsync()
    {
        try
        {
            IsLoading = true;

            var samples = await _sampleRepository.GetFilteredAsync(
                StatusFilter,
                StartDateFilter,
                EndDateFilter);

            Samples.Clear();
            foreach (var sample in samples)
            {
                Samples.Add(sample);
            }

            TotalCount = await _sampleRepository.GetCountAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadSamplesAsync();
    }

    [RelayCommand]
    private void CreateSample()
    {
        // TODO: Navigate to SampleDetailView in create mode
        // This will be implemented when we add navigation service
    }

    [RelayCommand]
    private void EditSample(Sample? sample)
    {
        if (sample == null) return;
        
        // TODO: Navigate to SampleDetailView in edit mode
        // This will be implemented when we add navigation service
    }

    [RelayCommand]
    private async Task DeleteSampleAsync(Sample? sample)
    {
        if (sample == null) return;

        // TODO: Add confirmation dialog
        var deleted = await _sampleRepository.DeleteAsync(sample.Id);
        
        if (deleted)
        {
            Samples.Remove(sample);
            TotalCount--;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        StatusFilter = null;
        StartDateFilter = null;
        EndDateFilter = null;
        await LoadSamplesAsync();
    }

    public async Task InitializeAsync()
    {
        await LoadSamplesAsync();
    }
}

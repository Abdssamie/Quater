using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.State;
using Quater.Desktop.Data.Repositories;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Desktop.Features.Samples.List;

public sealed partial class SampleListViewModel : ViewModelBase
{
    private readonly ISampleRepository _sampleRepository;
    private readonly IDialogService _dialogService;
    private readonly AppState _appState;

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

    [ObservableProperty]
    private string _searchText = string.Empty;

    public SampleListViewModel(
        ISampleRepository sampleRepository,
        IDialogService dialogService,
        AppState appState)
    {
        _sampleRepository = sampleRepository;
        _dialogService = dialogService;
        _appState = appState;
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        await LoadSamplesCoreAsync(ct);
    }

    [RelayCommand]
    private async Task LoadSamples()
    {
        await LoadSamplesCoreAsync();
    }

    private async Task LoadSamplesCoreAsync(CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;

            var samples = await _sampleRepository.GetFilteredAsync(
                StatusFilter,
                StartDateFilter,
                EndDateFilter,
                ct);

            Samples.Clear();
            foreach (var sample in samples)
            {
                Samples.Add(sample);
            }

            TotalCount = await _sampleRepository.GetCountAsync(ct);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to load samples: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadSamplesCoreAsync();
    }

    [RelayCommand]
    private void CreateSample()
    {
    }

    [RelayCommand]
    private void EditSample(Sample? sample)
    {
        if (sample is null) return;
    }

    [RelayCommand]
    private async Task DeleteSample(Sample? sample)
    {
        if (sample is null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Sample",
            $"Are you sure you want to delete sample {sample.Id}?");

        if (!confirmed) return;

        var deleted = await _sampleRepository.DeleteAsync(sample.Id);
        if (deleted)
        {
            Samples.Remove(sample);
            TotalCount--;
            _dialogService.ShowSuccess("Sample deleted successfully");
        }
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        StatusFilter = null;
        StartDateFilter = null;
        EndDateFilter = null;
        SearchText = string.Empty;
        await LoadSamplesCoreAsync();
    }

    [RelayCommand]
    private async Task ApplyFilters()
    {
        await LoadSamplesCoreAsync();
    }
}

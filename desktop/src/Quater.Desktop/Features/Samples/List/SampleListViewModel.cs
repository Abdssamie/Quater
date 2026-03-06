using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.State;
using Quater.Desktop.Data.Repositories;
using Quater.Desktop.Features.Samples.Edit;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Quater.Shared.ValueObjects;

namespace Quater.Desktop.Features.Samples.List;

public sealed partial class SampleListViewModel : ViewModelBase
{
    private readonly ISampleRepository _sampleRepository;
    private readonly IApiClientFactory _apiClientFactory;
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

    [ObservableProperty]
    private bool _isEditorOpen;

    [ObservableProperty]
    private SampleEditorViewModel? _editor;

    [ObservableProperty]
    private Sample? _editingSample;

    public SampleListViewModel(
        ISampleRepository sampleRepository,
        IApiClientFactory apiClientFactory,
        IDialogService dialogService,
        AppState appState)
    {
        _sampleRepository = sampleRepository;
        _apiClientFactory = apiClientFactory;
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

            Guid? labId = _appState.CurrentLabId == Guid.Empty
                ? null
                : _appState.CurrentLabId;

            var query = new SampleQuery
            {
                Status = StatusFilter,
                StartDate = StartDateFilter,
                EndDate = EndDateFilter,
                SearchText = SearchText,
                LabId = labId
            };

            var samples = await _sampleRepository.GetFilteredAsync(query, ct);

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
        var editor = new SampleEditorViewModel();
        editor.InitializeForCreate();

        Editor = editor;
        EditingSample = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditSample(Sample? sample)
    {
        if (sample is null)
        {
            return;
        }

        var editor = new SampleEditorViewModel();
        editor.InitializeForEdit(sample);

        Editor = editor;
        EditingSample = sample;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void CancelEditor()
    {
        IsEditorOpen = false;
        Editor = null;
        EditingSample = null;
    }

    [RelayCommand]
    private async Task SaveEditor(CancellationToken ct = default)
    {
        if (Editor is null)
        {
            return;
        }

        try
        {
            var api = _apiClientFactory.GetSamplesApi();

            if (Editor.IsEditMode)
            {
                if (!Editor.EditingSampleId.HasValue || !Editor.TryBuildUpdateDto(out var updateDto) || updateDto is null)
                {
                    _dialogService.ShowError("Please fill all fields with valid values.");
                    return;
                }

                var updated = await api.ApiSamplesIdPutAsync(Editor.EditingSampleId.Value, updateSampleDto: updateDto, cancellationToken: ct);
                ApplyUpdatedSample(updated);
                _dialogService.ShowSuccess("Sample updated.");
            }
            else
            {
                if (!_appState.CurrentLabId.Equals(Guid.Empty) && Editor.TryBuildCreateDto(_appState.CurrentLabId, out var createDto) && createDto is not null)
                {
                    await api.ApiSamplesPostAsync(createSampleDto: createDto, cancellationToken: ct);
                    _dialogService.ShowSuccess("Sample created.");
                    await LoadSamplesCoreAsync(ct);
                }
                else
                {
                    _dialogService.ShowError("Please fill all fields with valid values.");
                    return;
                }
            }

            CancelEditor();
        }
        catch (ApiException ex)
        {
            _dialogService.ShowError($"Failed to save sample: {ex.Message}");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to save sample: {ex.Message}");
        }
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

    private void ApplyUpdatedSample(Quater.Desktop.Api.Model.SampleDto dto)
    {
        var target = EditingSample;
        if (target is null)
        {
            return;
        }

        target.Type = (SampleType)(dto.Type ?? Quater.Desktop.Api.Model.SampleType.NUMBER_0);
        target.Status = (SampleStatus)(dto.Status ?? Quater.Desktop.Api.Model.SampleStatus.NUMBER_0);
        target.CollectionDate = dto.CollectionDate;
        target.CollectorName = dto.CollectorName ?? string.Empty;
        target.Notes = dto.Notes;
        target.Location = new Location(
            dto.LocationLatitude,
            dto.LocationLongitude,
            dto.LocationDescription,
            dto.LocationHierarchy);
        target.RowVersion = BitConverter.GetBytes(dto.VarVersion);
    }
}

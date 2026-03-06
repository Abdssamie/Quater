using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Features.TestResults.Edit;

namespace Quater.Desktop.Features.TestResults.List;

public sealed partial class TestResultListViewModel(
    IApiClientFactory apiClientFactory,
    IDialogService dialogService) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<TestResultListItem> _testResults = [];

    [ObservableProperty]
    private TestResultListItem? _selectedResult;

    [ObservableProperty]
    private Guid? _selectedSampleId;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditorOpen;

    [ObservableProperty]
    private TestResultEditorViewModel? _editor;

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        await LoadResultsCoreAsync(ct);
    }

    [RelayCommand]
    private async Task LoadResults(CancellationToken ct = default)
    {
        await LoadResultsCoreAsync(ct);
    }

    [RelayCommand]
    private async Task Refresh(CancellationToken ct = default)
    {
        await LoadResultsCoreAsync(ct);
    }

    [RelayCommand]
    private void CreateResult()
    {
        if (!SelectedSampleId.HasValue || SelectedSampleId.Value == Guid.Empty)
        {
            dialogService.ShowError("Select a sample before creating a test result.");
            return;
        }

        var editor = new TestResultEditorViewModel();
        editor.InitializeForCreate(SelectedSampleId.Value);
        Editor = editor;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditResult(TestResultListItem? item)
    {
        if (item is null)
        {
            return;
        }

        var editor = new TestResultEditorViewModel();
        editor.InitializeForEdit(item);
        Editor = editor;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void CancelEditor()
    {
        IsEditorOpen = false;
        Editor = null;
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
            var api = apiClientFactory.GetTestResultsApi();

            if (Editor.IsEditMode)
            {
                if (!Editor.EditingResultId.HasValue || !Editor.TryBuildUpdateDto(out var updateDto) || updateDto is null)
                {
                    dialogService.ShowError("Please fill all fields with valid values.");
                    return;
                }

                var updated = await api.ApiTestResultsIdPutAsync(Editor.EditingResultId.Value, updateTestResultDto: updateDto, cancellationToken: ct);
                UpsertListItem(MapToListItem(updated));
                dialogService.ShowSuccess("Test result updated.");
            }
            else
            {
                if (!Editor.TryBuildCreateDto(out var createDto) || createDto is null)
                {
                    dialogService.ShowError("Please fill all fields with valid values.");
                    return;
                }

                await api.ApiTestResultsPostAsync(createTestResultDto: createDto, cancellationToken: ct);
                dialogService.ShowSuccess("Test result created.");
                await LoadResultsCoreAsync(ct);
            }

            IsEditorOpen = false;
            Editor = null;
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Failed to save test result: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteResult(TestResultListItem? item)
    {
        if (item is null)
        {
            return;
        }

        var confirmed = await dialogService.ShowConfirmationAsync(
            "Delete Test Result",
            $"Are you sure you want to delete test result '{item.ParameterName}'?");

        if (!confirmed)
        {
            return;
        }

        try
        {
            var api = apiClientFactory.GetTestResultsApi();
            await api.ApiTestResultsIdDeleteAsync(item.Id);

            TestResults.Remove(item);
            TotalCount = Math.Max(0, TotalCount - 1);
            dialogService.ShowSuccess("Test result deleted.");
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Failed to delete test result: {ex.Message}");
        }
    }

    private async Task LoadResultsCoreAsync(CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;

            var api = apiClientFactory.GetTestResultsApi();
            TestResultDtoPagedResult response;

            if (SelectedSampleId.HasValue && SelectedSampleId.Value != Guid.Empty)
            {
                response = await api.ApiTestResultsBySampleSampleIdGetAsync(SelectedSampleId.Value, cancellationToken: ct);
            }
            else
            {
                response = await api.ApiTestResultsGetAsync(cancellationToken: ct);
            }

            TestResults.Clear();
            foreach (var dto in response.Items ?? [])
            {
                TestResults.Add(MapToListItem(dto));
            }

            TotalCount = response.TotalCount;
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Failed to load test results: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpsertListItem(TestResultListItem item)
    {
        var existing = TestResults.FirstOrDefault(x => x.Id == item.Id);
        if (existing is null)
        {
            TestResults.Add(item);
            TotalCount++;
            return;
        }

        var index = TestResults.IndexOf(existing);
        if (index >= 0)
        {
            TestResults[index] = item;
        }
    }

    private static TestResultListItem MapToListItem(TestResultDto dto)
    {
        var compliance = dto.ComplianceStatus ?? ComplianceStatus.NUMBER_2;
        var complianceDisplay = compliance switch
        {
            ComplianceStatus.NUMBER_0 => "Pass",
            ComplianceStatus.NUMBER_1 => "Fail",
            _ => "Warning"
        };

        var complianceColor = compliance switch
        {
            ComplianceStatus.NUMBER_0 => "#16A34A",
            ComplianceStatus.NUMBER_1 => "#DC2626",
            _ => "#CA8A04"
        };

        return new TestResultListItem(
            dto.Id,
            dto.SampleId,
            dto.ParameterName,
            dto.Value,
            dto.Unit,
            dto.TestDate,
            dto.TechnicianName,
            dto.TestMethod ?? TestMethod.NUMBER_0,
            complianceDisplay,
            complianceColor,
            dto.VarVersion);
    }
}

public sealed record TestResultListItem(
    Guid Id,
    Guid SampleId,
    string ParameterName,
    double Value,
    string Unit,
    DateTime TestDate,
    string TechnicianName,
    TestMethod TestMethod,
    string ComplianceStatusDisplay,
    string ComplianceColor,
    int Version);

using Moq;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.State;
using Quater.Desktop.Data.Repositories;
using Quater.Desktop.Features.Samples.List;
using Quater.Shared.Models;
using Quater.Shared.ValueObjects;

namespace Quater.Desktop.Tests.Features.Samples;

using ApiSampleStatus = Quater.Desktop.Api.Model.SampleStatus;
using ApiSampleType = Quater.Desktop.Api.Model.SampleType;
using SharedSampleStatus = Quater.Shared.Enums.SampleStatus;
using SharedSampleType = Quater.Shared.Enums.SampleType;

public sealed class SampleListViewModelTests
{
    [Fact]
    public async Task InitializeAsync_ForwardsSearchFiltersAndLabToRepositoryQuery()
    {
        var sampleRepository = new Mock<ISampleRepository>();
        var apiFactory = new Mock<IApiClientFactory>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState { CurrentLabId = Guid.NewGuid() };

        sampleRepository.Setup(repository => repository.GetFilteredAsync(It.IsAny<SampleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        sampleRepository.Setup(repository => repository.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var viewModel = new SampleListViewModel(sampleRepository.Object, apiFactory.Object, dialogService.Object, appState)
        {
            StatusFilter = SharedSampleStatus.Pending,
            StartDateFilter = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc),
            EndDateFilter = new DateTime(2026, 02, 20, 0, 0, 0, DateTimeKind.Utc),
            SearchText = "collector-a"
        };

        await viewModel.InitializeAsync();

        sampleRepository.Verify(repository => repository.GetFilteredAsync(
            It.Is<SampleQuery>(query =>
                query.Status == SharedSampleStatus.Pending &&
                query.StartDate == new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc) &&
                query.EndDate == new DateTime(2026, 02, 20, 0, 0, 0, DateTimeKind.Utc) &&
                query.SearchText == "collector-a" &&
                query.LabId == appState.CurrentLabId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSampleCommand_WhenConfirmationRejected_DoesNotDeleteOrMutateCollection()
    {
        var sampleRepository = new Mock<ISampleRepository>();
        var apiFactory = new Mock<IApiClientFactory>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState();

        dialogService.Setup(service => service.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var sample = CreateSample();
        var viewModel = new SampleListViewModel(sampleRepository.Object, apiFactory.Object, dialogService.Object, appState);
        viewModel.Samples.Add(sample);
        viewModel.TotalCount = 1;

        await viewModel.DeleteSampleCommand.ExecuteAsync(sample);

        Assert.Single(viewModel.Samples);
        Assert.Equal(1, viewModel.TotalCount);
        sampleRepository.Verify(repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSampleCommand_WhenConfirmedAndDeleteSucceeds_RemovesItemAndDecrementsTotal()
    {
        var sampleRepository = new Mock<ISampleRepository>();
        var apiFactory = new Mock<IApiClientFactory>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState();

        dialogService.Setup(service => service.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        sampleRepository.Setup(repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sample = CreateSample();
        var viewModel = new SampleListViewModel(sampleRepository.Object, apiFactory.Object, dialogService.Object, appState);
        viewModel.Samples.Add(sample);
        viewModel.TotalCount = 1;

        await viewModel.DeleteSampleCommand.ExecuteAsync(sample);

        Assert.Empty(viewModel.Samples);
        Assert.Equal(0, viewModel.TotalCount);
        dialogService.Verify(service => service.ShowSuccess("Sample deleted successfully"), Times.Once);
    }

    [Fact]
    public async Task DeleteSampleCommand_WhenConfirmedAndDeleteFails_KeepsCollectionUnchanged()
    {
        var sampleRepository = new Mock<ISampleRepository>();
        var apiFactory = new Mock<IApiClientFactory>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState();

        dialogService.Setup(service => service.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        sampleRepository.Setup(repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sample = CreateSample();
        var viewModel = new SampleListViewModel(sampleRepository.Object, apiFactory.Object, dialogService.Object, appState);
        viewModel.Samples.Add(sample);
        viewModel.TotalCount = 1;

        await viewModel.DeleteSampleCommand.ExecuteAsync(sample);

        Assert.Single(viewModel.Samples);
        Assert.Equal(1, viewModel.TotalCount);
        dialogService.Verify(service => service.ShowSuccess(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateSampleCommand_SaveEditor_CallsCreateApiAndRefreshesList()
    {
        var sampleRepository = new Mock<ISampleRepository>();
        var sampleApi = new Mock<ISamplesApi>();
        var apiFactory = new Mock<IApiClientFactory>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState { CurrentLabId = Guid.NewGuid() };

        sampleRepository.Setup(repository => repository.GetFilteredAsync(It.IsAny<SampleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        sampleRepository.Setup(repository => repository.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        apiFactory.Setup(factory => factory.GetSamplesApi())
            .Returns(sampleApi.Object);

        sampleApi.Setup(api => api.ApiSamplesPostAsync(
                It.IsAny<string?>(),
                It.Is<CreateSampleDto>(dto =>
                    dto.LabId == appState.CurrentLabId &&
                    dto.CollectorName == "Field Collector"),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SampleDto(id: Guid.NewGuid(), collectorName: "Field Collector", labId: appState.CurrentLabId));

        var viewModel = new SampleListViewModel(sampleRepository.Object, apiFactory.Object, dialogService.Object, appState);
        await viewModel.InitializeAsync();

        viewModel.CreateSampleCommand.Execute(null);
        Assert.NotNull(viewModel.Editor);

        viewModel.Editor!.CollectorName = "Field Collector";
        viewModel.Editor.LocationLatitude = 33.6;
        viewModel.Editor.LocationLongitude = -7.6;

        await viewModel.SaveEditorCommand.ExecuteAsync(null);

        sampleApi.Verify(api => api.ApiSamplesPostAsync(It.IsAny<string?>(), It.IsAny<CreateSampleDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        sampleRepository.Verify(repository => repository.GetFilteredAsync(It.IsAny<SampleQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EditSampleCommand_SaveEditor_CallsUpdateApiAndUpdatesSelectedSample()
    {
        var sampleRepository = new Mock<ISampleRepository>();
        var sampleApi = new Mock<ISamplesApi>();
        var apiFactory = new Mock<IApiClientFactory>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState { CurrentLabId = Guid.NewGuid() };

        sampleRepository.Setup(repository => repository.GetFilteredAsync(It.IsAny<SampleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        sampleRepository.Setup(repository => repository.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        apiFactory.Setup(factory => factory.GetSamplesApi())
            .Returns(sampleApi.Object);

        var sample = CreateSample();
        sample.RowVersion = BitConverter.GetBytes(3);

        sampleApi.Setup(api => api.ApiSamplesIdPutAsync(
                sample.Id,
                It.IsAny<string?>(),
                It.Is<UpdateSampleDto>(dto =>
                    dto.CollectorName == "Updated Collector" &&
                    dto.VarVersion == 3),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SampleDto(
                id: sample.Id,
                type: ApiSampleType.NUMBER_2,
                locationLatitude: 35.1,
                locationLongitude: -6.9,
                locationDescription: "Updated Site",
                locationHierarchy: "Region/New",
                collectionDate: new DateTime(2026, 02, 15, 0, 0, 0, DateTimeKind.Utc),
                collectorName: "Updated Collector",
                notes: "Updated notes",
                status: ApiSampleStatus.NUMBER_1,
                varVersion: 4,
                labId: sample.LabId));

        var viewModel = new SampleListViewModel(sampleRepository.Object, apiFactory.Object, dialogService.Object, appState);
        viewModel.Samples.Add(sample);

        viewModel.EditSampleCommand.Execute(sample);
        Assert.NotNull(viewModel.Editor);

        viewModel.Editor!.CollectorName = "Updated Collector";
        await viewModel.SaveEditorCommand.ExecuteAsync(null);

        sampleApi.Verify(api => api.ApiSamplesIdPutAsync(sample.Id, It.IsAny<string?>(), It.IsAny<UpdateSampleDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Updated Collector", sample.CollectorName);
        Assert.Equal(SharedSampleStatus.Completed, sample.Status);
    }

    [Fact]
    public void CreateSampleCommand_WhenCurrentLabRoleIsViewer_DoesNotOpenEditor()
    {
        var labId = Guid.NewGuid();
        var sampleRepository = new Mock<ISampleRepository>();
        var apiFactory = new Mock<IApiClientFactory>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState
        {
            CurrentLabId = labId,
            AvailableLabs = [new UserLabDto(labId: labId, labName: "Lab A", role: UserRole.NUMBER_1)]
        };

        var viewModel = new SampleListViewModel(sampleRepository.Object, apiFactory.Object, dialogService.Object, appState);

        viewModel.CreateSampleCommand.Execute(null);

        Assert.Null(viewModel.Editor);
        Assert.False(viewModel.IsEditorOpen);
    }

    [Fact]
    public async Task DeleteSampleCommand_WhenCurrentLabRoleIsViewer_DoesNotConfirmOrDelete()
    {
        var labId = Guid.NewGuid();
        var sampleRepository = new Mock<ISampleRepository>();
        var apiFactory = new Mock<IApiClientFactory>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState
        {
            CurrentLabId = labId,
            AvailableLabs = [new UserLabDto(labId: labId, labName: "Lab A", role: UserRole.NUMBER_1)]
        };

        var sample = CreateSample();
        sample.LabId = labId;
        var viewModel = new SampleListViewModel(sampleRepository.Object, apiFactory.Object, dialogService.Object, appState);
        viewModel.Samples.Add(sample);

        await viewModel.DeleteSampleCommand.ExecuteAsync(sample);

        dialogService.Verify(service => service.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        sampleRepository.Verify(repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(viewModel.Samples);
    }

    private static Sample CreateSample()
    {
        return new Sample
        {
            Id = Guid.NewGuid(),
            LabId = Guid.NewGuid(),
            Type = SharedSampleType.DrinkingWater,
            Status = SharedSampleStatus.Pending,
            CollectionDate = new DateTime(2026, 02, 14, 0, 0, 0, DateTimeKind.Utc),
            CollectorName = "Collector",
            Location = new Location(34.0, -6.8, "North Site", "Region/District/Site")
        };
    }
}

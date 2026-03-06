using Moq;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.State;
using Quater.Desktop.Data.Repositories;
using Quater.Desktop.Features.Samples.List;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Quater.Shared.ValueObjects;

namespace Quater.Desktop.Tests.Features.Samples;

public sealed class SampleListViewModelTests
{
    [Fact]
    public async Task InitializeAsync_ForwardsSearchFiltersAndLabToRepositoryQuery()
    {
        var sampleRepository = new Mock<ISampleRepository>();
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState { CurrentLabId = Guid.NewGuid() };

        sampleRepository.Setup(repository => repository.GetFilteredAsync(It.IsAny<SampleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        sampleRepository.Setup(repository => repository.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var viewModel = new SampleListViewModel(sampleRepository.Object, dialogService.Object, appState)
        {
            StatusFilter = SampleStatus.Pending,
            StartDateFilter = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc),
            EndDateFilter = new DateTime(2026, 02, 20, 0, 0, 0, DateTimeKind.Utc),
            SearchText = "collector-a"
        };

        await viewModel.InitializeAsync();

        sampleRepository.Verify(repository => repository.GetFilteredAsync(
            It.Is<SampleQuery>(query =>
                query.Status == SampleStatus.Pending &&
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
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState();

        dialogService.Setup(service => service.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var sample = CreateSample();
        var viewModel = new SampleListViewModel(sampleRepository.Object, dialogService.Object, appState);
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
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState();

        dialogService.Setup(service => service.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        sampleRepository.Setup(repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sample = CreateSample();
        var viewModel = new SampleListViewModel(sampleRepository.Object, dialogService.Object, appState);
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
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState();

        dialogService.Setup(service => service.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        sampleRepository.Setup(repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sample = CreateSample();
        var viewModel = new SampleListViewModel(sampleRepository.Object, dialogService.Object, appState);
        viewModel.Samples.Add(sample);
        viewModel.TotalCount = 1;

        await viewModel.DeleteSampleCommand.ExecuteAsync(sample);

        Assert.Single(viewModel.Samples);
        Assert.Equal(1, viewModel.TotalCount);
        dialogService.Verify(service => service.ShowSuccess(It.IsAny<string>()), Times.Never);
    }

    private static Sample CreateSample()
    {
        return new Sample
        {
            Id = Guid.NewGuid(),
            LabId = Guid.NewGuid(),
            Type = SampleType.DrinkingWater,
            Status = SampleStatus.Pending,
            CollectionDate = new DateTime(2026, 02, 14, 0, 0, 0, DateTimeKind.Utc),
            CollectorName = "Collector",
            Location = new Location(34.0, -6.8, "North Site", "Region/District/Site")
        };
    }
}

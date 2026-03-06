using Moq;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.State;
using Quater.Desktop.Features.Dashboard;

namespace Quater.Desktop.Tests.Features.Dashboard;

public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsApiBackedMetricsAndSyncStatus()
    {
        var samplesApi = new Mock<ISamplesApi>(MockBehavior.Strict);
        var resultsApi = new Mock<ITestResultsApi>(MockBehavior.Strict);
        var apiFactory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var syncStatusService = new Mock<ISyncStatusService>(MockBehavior.Strict);
        var apiErrorFormatter = new Mock<IApiErrorFormatter>(MockBehavior.Strict);
        var appState = new AppState();

        samplesApi.Setup(api => api.ApiSamplesGetAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SampleDtoPagedResult(items: [], totalCount: 12, pageNumber: 1, pageSize: 50));

        resultsApi.Setup(api => api.ApiTestResultsGetAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResultDtoPagedResult(
                items:
                [
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_0),
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_0),
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_1),
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_2)
                ],
                totalCount: 4,
                pageNumber: 1,
                pageSize: 50));

        apiFactory.Setup(factory => factory.GetSamplesApi()).Returns(samplesApi.Object);
        apiFactory.Setup(factory => factory.GetTestResultsApi()).Returns(resultsApi.Object);
        syncStatusService.Setup(service => service.GetSummary(appState))
            .Returns(new SyncStatusSummary("Sync delayed", pendingCount: 2, failedCount: 1));

        var viewModel = new DashboardViewModel(apiFactory.Object, syncStatusService.Object, apiErrorFormatter.Object, appState);

        await viewModel.InitializeAsync();

        Assert.Equal("12", viewModel.SamplesThisWeek);
        Assert.Equal("50.0%", viewModel.ComplianceRate);
        Assert.Equal("2", viewModel.PendingAlerts);
        Assert.Equal("Sync delayed", viewModel.SyncIndicator);
        Assert.Equal(string.Empty, viewModel.WarningMessage);

        Assert.Equal("12", viewModel.Stats.Single(x => x.Title == "Total Samples").Value);
        Assert.Equal("50.0%", viewModel.Stats.Single(x => x.Title == "Compliance Rate").Value);
        Assert.Equal("2", viewModel.Stats.Single(x => x.Title == "Critical Alerts").Value);
    }

    [Fact]
    public async Task InitializeAsync_WhenSamplesCallFails_KeepsAvailableCardsAndSetsWarning()
    {
        var samplesApi = new Mock<ISamplesApi>(MockBehavior.Strict);
        var resultsApi = new Mock<ITestResultsApi>(MockBehavior.Strict);
        var apiFactory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var syncStatusService = new Mock<ISyncStatusService>(MockBehavior.Strict);
        var apiErrorFormatter = new Mock<IApiErrorFormatter>(MockBehavior.Strict);
        var appState = new AppState();

        samplesApi.SetupSequence(api => api.ApiSamplesGetAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SampleDtoPagedResult(items: [], totalCount: 10, pageNumber: 1, pageSize: 50))
            .ThrowsAsync(new InvalidOperationException("samples unavailable"));

        resultsApi.SetupSequence(api => api.ApiTestResultsGetAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResultDtoPagedResult(
                items:
                [
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_0),
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_0)
                ],
                totalCount: 2,
                pageNumber: 1,
                pageSize: 50))
            .ReturnsAsync(new TestResultDtoPagedResult(
                items:
                [
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_0),
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_1),
                    new TestResultDto(complianceStatus: ComplianceStatus.NUMBER_2)
                ],
                totalCount: 3,
                pageNumber: 1,
                pageSize: 50));

        apiFactory.Setup(factory => factory.GetSamplesApi()).Returns(samplesApi.Object);
        apiFactory.Setup(factory => factory.GetTestResultsApi()).Returns(resultsApi.Object);
        syncStatusService.Setup(service => service.GetSummary(appState))
            .Returns(new SyncStatusSummary("Up to date", pendingCount: 0, failedCount: 0));
        syncStatusService.Setup(service => service.GetSummary(appState))
            .Returns(new SyncStatusSummary("Up to date", pendingCount: 0, failedCount: 0));

        apiErrorFormatter.Setup(formatter => formatter.ToDisplayMessage(It.IsAny<InvalidOperationException>(), "Unable to load total samples."))
            .Returns("Sample service unavailable");

        var viewModel = new DashboardViewModel(apiFactory.Object, syncStatusService.Object, apiErrorFormatter.Object, appState);

        await viewModel.InitializeAsync();
        await viewModel.InitializeAsync();

        Assert.Equal("10", viewModel.SamplesThisWeek);
        Assert.Equal("33.3%", viewModel.ComplianceRate);
        Assert.Equal("2", viewModel.PendingAlerts);
        Assert.Equal("Sample service unavailable", viewModel.WarningMessage);

        Assert.Equal("10", viewModel.Stats.Single(x => x.Title == "Total Samples").Value);
        Assert.Equal("33.3%", viewModel.Stats.Single(x => x.Title == "Compliance Rate").Value);
        Assert.Equal("2", viewModel.Stats.Single(x => x.Title == "Critical Alerts").Value);
    }
}

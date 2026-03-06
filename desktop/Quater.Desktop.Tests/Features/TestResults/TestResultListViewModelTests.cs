using Moq;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Features.TestResults.List;

namespace Quater.Desktop.Tests.Features.TestResults;

using ApiComplianceStatus = Quater.Desktop.Api.Model.ComplianceStatus;
using ApiTestMethod = Quater.Desktop.Api.Model.TestMethod;

public sealed class TestResultListViewModelTests
{
    [Fact]
    public async Task LoadResults_WhenApiReturnsItems_MapsComplianceAndFields()
    {
        var api = new Mock<ITestResultsApi>(MockBehavior.Strict);
        var factory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);

        var sampleId = Guid.NewGuid();
        api.Setup(x => x.ApiTestResultsGetAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResultDtoPagedResult(
                items:
                [
                    new TestResultDto(
                        id: Guid.NewGuid(),
                        sampleId: sampleId,
                        parameterName: "pH",
                        value: 7.1,
                        unit: "pH",
                        testDate: DateTime.UtcNow,
                        technicianName: "Amine",
                        testMethod: ApiTestMethod.NUMBER_1,
                        complianceStatus: ApiComplianceStatus.NUMBER_0,
                        varVersion: 4)
                ],
                totalCount: 1,
                pageNumber: 1,
                pageSize: 50));

        factory.Setup(x => x.GetTestResultsApi()).Returns(api.Object);

        var vm = new TestResultListViewModel(factory.Object, dialogs.Object);

        await vm.LoadResultsCommand.ExecuteAsync(null);

        Assert.Single(vm.TestResults);
        Assert.Equal(1, vm.TotalCount);
        Assert.Equal("Pass", vm.TestResults[0].ComplianceStatusDisplay);
        Assert.Equal("pH", vm.TestResults[0].ParameterName);
        Assert.Equal("Amine", vm.TestResults[0].TechnicianName);

        factory.VerifyAll();
        api.VerifyAll();
    }

    [Fact]
    public async Task LoadResults_WhenSampleFilterSet_UsesBySampleEndpoint()
    {
        var api = new Mock<ITestResultsApi>(MockBehavior.Strict);
        var factory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);

        var sampleId = Guid.NewGuid();
        api.Setup(x => x.ApiTestResultsBySampleSampleIdGetAsync(sampleId, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResultDtoPagedResult(items: [], totalCount: 0, pageNumber: 1, pageSize: 50));

        factory.Setup(x => x.GetTestResultsApi()).Returns(api.Object);

        var vm = new TestResultListViewModel(factory.Object, dialogs.Object)
        {
            SelectedSampleId = sampleId
        };

        await vm.LoadResultsCommand.ExecuteAsync(null);

        api.Verify(x => x.ApiTestResultsBySampleSampleIdGetAsync(sampleId, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(x => x.ApiTestResultsGetAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        factory.VerifyAll();
    }

    [Fact]
    public async Task DeleteResult_WhenConfirmed_DeletesAndRemovesRow()
    {
        var api = new Mock<ITestResultsApi>(MockBehavior.Strict);
        var factory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);

        var id = Guid.NewGuid();
        factory.Setup(x => x.GetTestResultsApi()).Returns(api.Object);
        dialogs.Setup(x => x.ShowConfirmationAsync("Delete Test Result", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        dialogs.Setup(x => x.ShowSuccess(It.IsAny<string>()));
        api.Setup(x => x.ApiTestResultsIdDeleteAsync(id, It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var vm = new TestResultListViewModel(factory.Object, dialogs.Object)
        {
            TotalCount = 1
        };

        var row = new TestResultListItem(id, Guid.NewGuid(), "Turbidity", 1.2, "NTU", DateTime.UtcNow, "Nora", ApiTestMethod.NUMBER_2, "Warning", "#CA8A04", 2);
        vm.TestResults.Add(row);

        await vm.DeleteResultCommand.ExecuteAsync(row);

        Assert.Empty(vm.TestResults);
        Assert.Equal(0, vm.TotalCount);
        api.Verify(x => x.ApiTestResultsIdDeleteAsync(id, It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        dialogs.VerifyAll();
    }

    [Fact]
    public async Task CreateDialogSubmit_PostsResult_AndRefreshesList()
    {
        var api = new Mock<ITestResultsApi>(MockBehavior.Strict);
        var factory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);

        var sampleId = Guid.NewGuid();
        factory.Setup(x => x.GetTestResultsApi()).Returns(api.Object);
        dialogs.Setup(x => x.ShowSuccess(It.IsAny<string>()));

        api.Setup(x => x.ApiTestResultsPostAsync(
                It.IsAny<string?>(),
                It.Is<CreateTestResultDto>(dto => dto.SampleId == sampleId && dto.ParameterName == "Chlorine"),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResultDto(
                id: Guid.NewGuid(),
                sampleId: sampleId,
                parameterName: "Chlorine",
                value: 0.4,
                unit: "mg/L",
                testDate: DateTime.UtcNow,
                technicianName: "Alaa",
                testMethod: ApiTestMethod.NUMBER_0,
                complianceStatus: ApiComplianceStatus.NUMBER_0,
                varVersion: 1));

        api.Setup(x => x.ApiTestResultsBySampleSampleIdGetAsync(sampleId, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResultDtoPagedResult(
                items:
                [
                    new TestResultDto(
                        id: Guid.NewGuid(),
                        sampleId: sampleId,
                        parameterName: "Chlorine",
                        value: 0.4,
                        unit: "mg/L",
                        testDate: DateTime.UtcNow,
                        technicianName: "Alaa",
                        testMethod: ApiTestMethod.NUMBER_0,
                        complianceStatus: ApiComplianceStatus.NUMBER_0,
                        varVersion: 1)
                ],
                totalCount: 1,
                pageNumber: 1,
                pageSize: 50));

        var vm = new TestResultListViewModel(factory.Object, dialogs.Object)
        {
            SelectedSampleId = sampleId
        };

        vm.CreateResultCommand.Execute(null);
        Assert.NotNull(vm.Editor);
        vm.Editor!.ParameterName = "Chlorine";
        vm.Editor.Value = 0.4;
        vm.Editor.Unit = "mg/L";
        vm.Editor.TechnicianName = "Alaa";
        vm.Editor.TestMethod = ApiTestMethod.NUMBER_0;

        await vm.SaveEditorCommand.ExecuteAsync(null);

        api.Verify(x => x.ApiTestResultsPostAsync(It.IsAny<string?>(), It.IsAny<CreateTestResultDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(x => x.ApiTestResultsBySampleSampleIdGetAsync(sampleId, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(vm.TestResults);
        Assert.False(vm.IsEditorOpen);
    }

    [Fact]
    public async Task EditDialogSubmit_PutsResult_AndUpdatesExistingRow()
    {
        var api = new Mock<ITestResultsApi>(MockBehavior.Strict);
        var factory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);

        var id = Guid.NewGuid();
        var sampleId = Guid.NewGuid();

        factory.Setup(x => x.GetTestResultsApi()).Returns(api.Object);
        dialogs.Setup(x => x.ShowSuccess(It.IsAny<string>()));

        api.Setup(x => x.ApiTestResultsIdPutAsync(
                id,
                It.IsAny<string?>(),
                It.Is<UpdateTestResultDto>(dto => dto.ParameterName == "Nitrate" && dto.VarVersion == 3),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestResultDto(
                id: id,
                sampleId: sampleId,
                parameterName: "Nitrate",
                value: 12.3,
                unit: "mg/L",
                testDate: DateTime.UtcNow,
                technicianName: "Sara",
                testMethod: ApiTestMethod.NUMBER_3,
                complianceStatus: ApiComplianceStatus.NUMBER_1,
                varVersion: 4));

        var vm = new TestResultListViewModel(factory.Object, dialogs.Object)
        {
            TotalCount = 1
        };

        vm.TestResults.Add(new TestResultListItem(
            id,
            sampleId,
            "Nitrate",
            9.1,
            "mg/L",
            DateTime.UtcNow,
            "Sara",
            ApiTestMethod.NUMBER_3,
            "Pass",
            "#16A34A",
            3));

        vm.EditResultCommand.Execute(vm.TestResults[0]);
        Assert.NotNull(vm.Editor);
        vm.Editor!.Value = 12.3;

        await vm.SaveEditorCommand.ExecuteAsync(null);

        api.Verify(x => x.ApiTestResultsIdPutAsync(id, It.IsAny<string?>(), It.IsAny<UpdateTestResultDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(vm.TestResults);
        Assert.Equal(12.3, vm.TestResults[0].Value);
        Assert.Equal("Fail", vm.TestResults[0].ComplianceStatusDisplay);
        Assert.False(vm.IsEditorOpen);
    }

    [Fact]
    public async Task DeleteResult_WhenApiReturnsForbidden_ShowsPermissionErrorMessage()
    {
        var api = new Mock<ITestResultsApi>(MockBehavior.Strict);
        var factory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);

        var id = Guid.NewGuid();
        factory.Setup(x => x.GetTestResultsApi()).Returns(api.Object);
        dialogs.Setup(x => x.ShowConfirmationAsync("Delete Test Result", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        api.Setup(x => x.ApiTestResultsIdDeleteAsync(id, It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Quater.Desktop.Api.Client.ApiException(403, "Forbidden"));

        var vm = new TestResultListViewModel(factory.Object, dialogs.Object)
        {
            TotalCount = 1
        };

        var row = new TestResultListItem(id, Guid.NewGuid(), "Turbidity", 1.2, "NTU", DateTime.UtcNow, "Nora", ApiTestMethod.NUMBER_2, "Warning", "#CA8A04", 2);
        vm.TestResults.Add(row);

        await vm.DeleteResultCommand.ExecuteAsync(row);

        dialogs.Verify(x => x.ShowError("You do not have permission to delete test results."), Times.Once);
    }
}

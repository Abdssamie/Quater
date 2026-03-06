using Moq;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Features.Diagnostics;

namespace Quater.Desktop.Tests.Features.Diagnostics;

public sealed class DiagnosticsViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsBackendUrlAndRuntimeDiagnostics()
    {
        var apiFactory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var settingsStore = new Mock<ISettingsStore>(MockBehavior.Strict);
        var versionApi = new Mock<IVersionApi>(MockBehavior.Strict);

        settingsStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { BackendUrl = "https://lab.quater.local:7443" });
        apiFactory.Setup(factory => factory.GetVersionApi()).Returns(versionApi.Object);
        versionApi.Setup(api => api.ApiVersionGetAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = new DiagnosticsViewModel(settingsStore.Object, apiFactory.Object);

        await viewModel.InitializeAsync();

        Assert.Equal("https://lab.quater.local:7443", viewModel.BackendUrl);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.RuntimeVersion));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.OperatingSystem));
        Assert.Equal("Reachable", viewModel.BackendStatus);
    }

    [Fact]
    public async Task CheckBackendHealthCommand_WhenVersionProbeFails_SetsUnreachableStatus()
    {
        var apiFactory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var settingsStore = new Mock<ISettingsStore>(MockBehavior.Strict);
        var versionApi = new Mock<IVersionApi>(MockBehavior.Strict);

        settingsStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { BackendUrl = "https://cloud.quater.app" });
        apiFactory.Setup(factory => factory.GetVersionApi()).Returns(versionApi.Object);
        versionApi.Setup(api => api.ApiVersionGetAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Quater.Desktop.Api.Client.ApiException(503, "Unavailable"));

        var viewModel = new DiagnosticsViewModel(settingsStore.Object, apiFactory.Object);

        await viewModel.InitializeAsync();
        await viewModel.CheckBackendHealthCommand.ExecuteAsync(null);

        Assert.Equal("Unreachable", viewModel.BackendStatus);
    }
}

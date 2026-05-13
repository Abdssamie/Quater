using Moq;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Features.Diagnostics;

namespace Quater.Desktop.Tests.Features.Diagnostics;

public sealed class DiagnosticsViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsBackendUrlAndRuntimeDiagnostics()
    {
        var connectivityProbe = new Mock<IBackendConnectivityProbe>(MockBehavior.Strict);
        var settingsStore = new Mock<ISettingsStore>(MockBehavior.Strict);

        settingsStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { BackendUrl = "https://lab.quater.local:7443" });
        connectivityProbe.Setup(probe => probe.ProbeAsync("https://lab.quater.local:7443", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = new DiagnosticsViewModel(settingsStore.Object, connectivityProbe.Object);

        await viewModel.InitializeAsync();

        Assert.Equal("https://lab.quater.local:7443", viewModel.BackendUrl);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.RuntimeVersion));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.OperatingSystem));
        Assert.Equal("Reachable", viewModel.BackendStatus);
    }

    [Fact]
    public async Task CheckBackendHealthCommand_WhenHealthProbeFails_SetsUnreachableStatus()
    {
        var connectivityProbe = new Mock<IBackendConnectivityProbe>(MockBehavior.Strict);
        var settingsStore = new Mock<ISettingsStore>(MockBehavior.Strict);

        settingsStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { BackendUrl = "https://cloud.quater.app" });
        connectivityProbe.Setup(probe => probe.ProbeAsync("https://cloud.quater.app", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unavailable"));

        var viewModel = new DiagnosticsViewModel(settingsStore.Object, connectivityProbe.Object);

        await viewModel.InitializeAsync();
        await viewModel.CheckBackendHealthCommand.ExecuteAsync(null);

        Assert.Equal("Unreachable", viewModel.BackendStatus);
    }
}

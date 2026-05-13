using Moq;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Features.Onboarding;

namespace Quater.Desktop.Tests.Features.Onboarding;

public sealed class OnboardingViewModelTests
{
    [Fact]
    public async Task ContinueCommand_WithReachableCustomBackend_SavesSettingsAndCompletes()
    {
        var store = new Mock<ISettingsStore>(MockBehavior.Strict);
        var settings = new AppSettings();
        var updater = new SettingsUpdater(store.Object, settings);
        var probe = new Mock<IBackendConnectivityProbe>(MockBehavior.Strict);
        var completed = false;

        probe.Setup(p => p.ProbeAsync("http://127.0.0.1:5198", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        store.Setup(s => s.SaveAsync(It.Is<AppSettings>(value =>
                value.BackendUrl == "http://127.0.0.1:5198" &&
                !value.IsOnboarded),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        store.Setup(s => s.SaveAsync(It.Is<AppSettings>(value =>
                value.BackendUrl == "http://127.0.0.1:5198" &&
                value.IsOnboarded),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = new OnboardingViewModel(updater, probe.Object)
        {
            UseCloud = false,
            CustomUrl = "http://127.0.0.1:5198"
        };
        viewModel.OnboardingCompleted += (_, _) => completed = true;

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.True(completed);
        Assert.Equal("http://127.0.0.1:5198", settings.BackendUrl);
        Assert.True(settings.IsOnboarded);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
    }

    [Fact]
    public async Task ContinueCommand_WhenBackendProbeFails_DoesNotPersistOnboarding()
    {
        var store = new Mock<ISettingsStore>(MockBehavior.Strict);
        var settings = new AppSettings();
        var updater = new SettingsUpdater(store.Object, settings);
        var probe = new Mock<IBackendConnectivityProbe>(MockBehavior.Strict);

        probe.Setup(p => p.ProbeAsync("http://127.0.0.1:5198", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("offline"));

        var viewModel = new OnboardingViewModel(updater, probe.Object)
        {
            UseCloud = false,
            CustomUrl = "http://127.0.0.1:5198"
        };

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.False(settings.IsOnboarded);
        Assert.Equal(string.Empty, settings.BackendUrl);
        Assert.Equal("Could not reach the backend. Verify the URL and that the server is running.", viewModel.ErrorMessage);
    }
}

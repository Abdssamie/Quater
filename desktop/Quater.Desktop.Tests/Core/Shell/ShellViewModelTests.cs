using Microsoft.Extensions.DependencyInjection;
using Moq;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.Shell;
using Quater.Desktop.Core.State;
using Quater.Desktop.Features.Sync.Center;
using Quater.Desktop.Features.TestResults.List;
using SukiUI.Toasts;

namespace Quater.Desktop.Tests.Core.Shell;

public sealed class ShellViewModelTests
{
    [Fact]
    public void HasSelectedLab_WhenAuthenticatedWithoutLab_IsFalse()
    {
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true,
            CurrentLabId = Guid.Empty
        };

        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());

        var viewModel = CreateViewModel(navigationService.Object, appState);

        Assert.True(viewModel.IsAuthenticated);
        Assert.False(viewModel.HasSelectedLab);
        navigationService.Verify(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>(), Times.Once);
    }

    [Fact]
    public void NavigateToTestResults_WhenNoLabSelected_DoesNotNavigate()
    {
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true,
            CurrentLabId = Guid.Empty
        };

        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());

        var viewModel = CreateViewModel(navigationService.Object, appState);

        viewModel.NavigateToTestResultsCommand.Execute(null);

        navigationService.Verify(service => service.NavigateTo<TestResultListViewModel>(), Times.Never);
        navigationService.Verify(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>(), Times.Once);
    }

    [Fact]
    public void NavigateToTestResults_WhenLabSelected_NavigatesToRoute()
    {
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true,
            CurrentLabId = Guid.NewGuid()
        };

        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());
        navigationService.Setup(service => service.NavigateTo<TestResultListViewModel>());

        var viewModel = CreateViewModel(navigationService.Object, appState);

        viewModel.NavigateToTestResultsCommand.Execute(null);

        navigationService.Verify(service => service.NavigateTo<TestResultListViewModel>(), Times.Once);
    }

    [Fact]
    public void NavigateToSyncCenter_WhenAuthenticated_NavigatesToRoute()
    {
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true
        };

        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());
        navigationService.Setup(service => service.NavigateTo<SyncCenterViewModel>());

        var viewModel = CreateViewModel(navigationService.Object, appState);

        viewModel.NavigateToSyncCenterCommand.Execute(null);

        navigationService.Verify(service => service.NavigateTo<SyncCenterViewModel>(), Times.Once);
    }

    [Fact]
    public void SyncStatus_WhenAppStateChanges_UpdatesShellStatusText()
    {
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true,
            SyncStatusText = "Up to Date",
            PendingSyncCount = 0,
            FailedSyncCount = 0
        };

        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());

        var viewModel = CreateViewModel(navigationService.Object, appState);

        appState.SyncStatusText = "Retry scheduled";
        appState.PendingSyncCount = 3;
        appState.FailedSyncCount = 1;

        Assert.Equal("Retry scheduled (pending: 3, failed: 1)", viewModel.SyncStatus);
    }

    private static ShellViewModel CreateViewModel(INavigationService navigationService, AppState appState)
    {
        var serviceProvider = new ServiceCollection()
            .AddTransient<Quater.Desktop.Features.Auth.LoginViewModel>(_ => null!)
            .BuildServiceProvider();

        return new ShellViewModel(
            navigationService,
            appState,
            serviceProvider,
            settingsUpdater: null!,
            toastManager: Mock.Of<ISukiToastManager>(),
            settingsStore: Mock.Of<ISettingsStore>(),
            authSessionManager: null!);
    }
}

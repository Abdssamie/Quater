using Microsoft.Extensions.DependencyInjection;
using Moq;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.Shell;
using Quater.Desktop.Core.State;
using Quater.Desktop.Features.Audit.List;
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
    public void NavigationItems_WhenLabSelectedWithoutAuditPermission_HidesAuditItem()
    {
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true,
            CurrentLabId = Guid.NewGuid(),
            AvailableLabs =
            [
                new UserLabDto(Guid.NewGuid(), "Lab A", UserRole.NUMBER_2, DateTime.UtcNow)
            ],
            SyncStatusText = "Up to Date",
            PendingSyncCount = 0,
            FailedSyncCount = 0
        };

        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());
        navigationService.SetupGet(service => service.NavigationItems).Returns(CreateNavigationItems());

        var permissionService = new Mock<IPermissionService>(MockBehavior.Strict);
        permissionService.Setup(service => service.CanAccessAuditWorkflow(It.IsAny<UserLabDto?>())).Returns(false);

        var viewModel = CreateViewModel(navigationService.Object, appState, permissionService.Object);

        Assert.DoesNotContain(viewModel.NavigationItems, item => item.ViewModelType == typeof(AuditListViewModel));
    }

    [Fact]
    public void NavigationItems_WhenLabSelectedWithAuditPermission_ShowsAuditItem()
    {
        var selectedLab = new UserLabDto(Guid.NewGuid(), "Lab A", UserRole.NUMBER_3, DateTime.UtcNow);
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true,
            CurrentLabId = selectedLab.LabId,
            AvailableLabs = [selectedLab]
        };

        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());
        navigationService.SetupGet(service => service.NavigationItems).Returns(CreateNavigationItems());

        var permissionService = new Mock<IPermissionService>(MockBehavior.Strict);
        permissionService.Setup(service => service.CanAccessAuditWorkflow(It.IsAny<UserLabDto?>())).Returns(true);

        var viewModel = CreateViewModel(navigationService.Object, appState, permissionService.Object);

        Assert.Contains(viewModel.NavigationItems, item => item.ViewModelType == typeof(AuditListViewModel));
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

    private static IReadOnlyList<NavigationItem> CreateNavigationItems()
    {
        return
        [
            new NavigationItem("Dashboard", string.Empty, typeof(Quater.Desktop.Features.Dashboard.DashboardViewModel), 0),
            new NavigationItem("Samples", string.Empty, typeof(Quater.Desktop.Features.Samples.List.SampleListViewModel), 1),
            new NavigationItem("Test Results", string.Empty, typeof(TestResultListViewModel), 2),
            new NavigationItem("Audit", string.Empty, typeof(AuditListViewModel), 3),
            new NavigationItem("Sync Center", string.Empty, typeof(SyncCenterViewModel), 4)
        ];
    }

    private static ShellViewModel CreateViewModel(
        INavigationService navigationService,
        AppState appState,
        IPermissionService? permissionService = null)
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
            authSessionManager: null!,
            permissionService: permissionService ?? Mock.Of<IPermissionService>());
    }
}

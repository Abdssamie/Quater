using Microsoft.Extensions.DependencyInjection;
using Moq;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.Shell;
using Quater.Desktop.Core.State;
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
    public void NavigationItems_WhenCurrentLabRoleIsViewer_HidesLabScopedEntries()
    {
        var labId = Guid.NewGuid();
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true,
            CurrentLabId = labId,
            AvailableLabs =
            [
                new UserLabDto(labId: labId, labName: "Lab A", role: UserRole.NUMBER_1)
            ]
        };

        navigationService.SetupGet(service => service.NavigationItems)
            .Returns(
            [
                new NavigationItem("Dashboard", "icon", typeof(Quater.Desktop.Features.Dashboard.DashboardViewModel)),
                new NavigationItem("Samples", "icon", typeof(Quater.Desktop.Features.Samples.List.SampleListViewModel), 1),
                new NavigationItem("Test Results", "icon", typeof(TestResultListViewModel), 2)
            ]);
        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());

        var viewModel = CreateViewModel(navigationService.Object, appState);

        Assert.DoesNotContain(viewModel.NavigationItems, item => item.ViewModelType == typeof(Quater.Desktop.Features.Samples.List.SampleListViewModel));
        Assert.DoesNotContain(viewModel.NavigationItems, item => item.ViewModelType == typeof(TestResultListViewModel));
    }

    [Fact]
    public void NavigateToSamples_WhenCurrentLabRoleIsViewer_DoesNotNavigate()
    {
        var labId = Guid.NewGuid();
        var navigationService = new Mock<INavigationService>(MockBehavior.Strict);
        var appState = new AppState
        {
            IsAuthenticated = true,
            CurrentLabId = labId,
            AvailableLabs =
            [
                new UserLabDto(labId: labId, labName: "Lab A", role: UserRole.NUMBER_1)
            ]
        };

        navigationService.SetupGet(service => service.NavigationItems).Returns([]);
        navigationService.Setup(service => service.NavigateTo<Quater.Desktop.Features.Dashboard.DashboardViewModel>());

        var viewModel = CreateViewModel(navigationService.Object, appState);

        viewModel.NavigateToSamplesCommand.Execute(null);

        navigationService.Verify(service => service.NavigateTo<Quater.Desktop.Features.Samples.List.SampleListViewModel>(), Times.Never);
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

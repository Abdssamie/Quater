using CommunityToolkit.Mvvm.ComponentModel;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Core.Shell;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly AppState _appState;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _connectionStatus = "Connected";

    [ObservableProperty]
    private string _syncStatus = "Up to Date";

    public IReadOnlyList<NavigationItem> NavigationItems => _navigationService.NavigationItems;

    public ShellViewModel(INavigationService navigationService, AppState appState)
    {
        _navigationService = navigationService;
        _appState = appState;

        _navigationService.CurrentViewChanged += (_, vm) => CurrentView = vm;

        _navigationService.NavigateTo<Features.Dashboard.DashboardViewModel>();
    }

    public void NavigateTo(NavigationItem item)
    {
        _navigationService.NavigateTo(item);
    }
}

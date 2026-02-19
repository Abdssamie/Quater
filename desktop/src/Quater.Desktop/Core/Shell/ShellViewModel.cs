using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.State;
using Quater.Desktop.Features.Auth;

namespace Quater.Desktop.Core.Shell;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly AppState _appState;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _connectionStatus = "Connected";

    [ObservableProperty]
    private string _syncStatus = "Up to Date";

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _currentLabName = string.Empty;

    [ObservableProperty]
    private LabDto? _selectedLab;

    public IReadOnlyList<NavigationItem> NavigationItems => _navigationService.NavigationItems;

    public IReadOnlyList<LabDto> AvailableLabs => _appState.AvailableLabs;

    public ShellViewModel(INavigationService navigationService, AppState appState, IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _appState = appState;
        _serviceProvider = serviceProvider;

        _navigationService.CurrentViewChanged += (_, vm) => CurrentView = vm;
        _appState.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AppState.IsAuthenticated))
            {
                UpdateAuthState();
            }
            else if (args.PropertyName == nameof(AppState.CurrentLabName))
            {
                CurrentLabName = _appState.CurrentLabName;
            }
            else if (args.PropertyName == nameof(AppState.AvailableLabs))
            {
                OnPropertyChanged(nameof(AvailableLabs));
                if (_appState.AvailableLabs.Count == 1)
                {
                    SelectedLab = _appState.AvailableLabs[0];
                }
            }
            else if (args.PropertyName == nameof(AppState.CurrentLabId))
            {
                SelectedLab = _appState.AvailableLabs.FirstOrDefault(lab => lab.Id == _appState.CurrentLabId);
            }
        };

        CurrentLabName = _appState.CurrentLabName;
        SelectedLab = _appState.AvailableLabs.FirstOrDefault(lab => lab.Id == _appState.CurrentLabId);
        UpdateAuthState();
    }

    partial void OnSelectedLabChanged(LabDto? value)
    {
        if (value == null)
        {
            _appState.CurrentLabId = Guid.Empty;
            _appState.CurrentLabName = string.Empty;
            return;
        }

        if (_appState.CurrentLabId == value.Id && _appState.CurrentLabName == value.Name)
        {
            return;
        }

        _appState.CurrentLabId = value.Id;
        _appState.CurrentLabName = value.Name;
    }

    private void UpdateAuthState()
    {
        IsAuthenticated = _appState.IsAuthenticated;

        if (IsAuthenticated)
        {
            _navigationService.NavigateTo<Features.Dashboard.DashboardViewModel>();
        }
        else
        {
            CurrentView = _serviceProvider.GetRequiredService<LoginViewModel>();
        }
    }

    public void NavigateTo(NavigationItem item)
    {
        _navigationService.NavigateTo(item);
    }
}

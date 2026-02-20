using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.State;
using Quater.Desktop.Features.Auth;

namespace Quater.Desktop.Core.Shell;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly AppState _appState;
    private readonly IServiceProvider _serviceProvider;
    private readonly SettingsUpdater _settingsUpdater;

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
    private UserLabDto? _selectedLab;

    public IReadOnlyList<NavigationItem> NavigationItems => _navigationService.NavigationItems;

    public IReadOnlyList<UserLabDto> AvailableLabs => _appState.AvailableLabs;

    public bool HasSelectedLab => _appState.CurrentLabId != Guid.Empty;

    public bool IsLabSelectorVisible => _appState.AvailableLabs.Count > 1;

    public ShellViewModel(INavigationService navigationService, AppState appState, IServiceProvider serviceProvider, SettingsUpdater settingsUpdater)
    {
        _navigationService = navigationService;
        _appState = appState;
        _serviceProvider = serviceProvider;
        _settingsUpdater = settingsUpdater;

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
                OnPropertyChanged(nameof(IsLabSelectorVisible));
                if (_appState.CurrentLabId != Guid.Empty && _appState.AvailableLabs.All(lab => lab.LabId != _appState.CurrentLabId))
                {
                    _appState.CurrentLabId = Guid.Empty;
                    _appState.CurrentLabName = string.Empty;
                }
                if (_appState.AvailableLabs.Count == 1)
                {
                    SelectedLab = _appState.AvailableLabs[0];
                }
            }
            else if (args.PropertyName == nameof(AppState.CurrentLabId))
            {
                SelectedLab = _appState.AvailableLabs.FirstOrDefault(lab => lab.LabId == _appState.CurrentLabId);
                OnPropertyChanged(nameof(HasSelectedLab));
            }
        };

        CurrentLabName = _appState.CurrentLabName;
        SelectedLab = _appState.AvailableLabs.FirstOrDefault(lab => lab.LabId == _appState.CurrentLabId);
        UpdateAuthState();
    }

    partial void OnSelectedLabChanged(UserLabDto? value)
    {
        if (value is null)
        {
            _appState.CurrentLabId = Guid.Empty;
            _appState.CurrentLabName = string.Empty;
            OnPropertyChanged(nameof(HasSelectedLab));
            return;
        }

        if (_appState.CurrentLabId == value.LabId && _appState.CurrentLabName == value.LabName)
        {
            return;
        }

        _appState.CurrentLabId = value.LabId;
        _appState.CurrentLabName = value.LabName;
        _ = SaveSelectedLabAsync(value.LabId);
        OnPropertyChanged(nameof(HasSelectedLab));
    }

    private async Task SaveSelectedLabAsync(Guid labId)
    {
        if (_settingsUpdater.Current.LastUsedLabId == labId)
        {
            return;
        }

        _settingsUpdater.Current.LastUsedLabId = labId;
        await _settingsUpdater.SaveAsync();
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

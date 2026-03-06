using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.State;
using Quater.Desktop.Features.Sync.Center;
using Quater.Desktop.Features.Auth;
using Quater.Desktop.Features.Samples.List;
using Quater.Desktop.Features.TestResults.List;
using SukiUI.Toasts;

namespace Quater.Desktop.Core.Shell;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly AppState _appState;
    private readonly IServiceProvider _serviceProvider;
    private readonly SettingsUpdater _settingsUpdater;
    private readonly ISettingsStore _settingsStore;
    private readonly AuthSessionManager _authSessionManager;
    private bool _isSyncingSelectedNavigationItem;

    public ISukiToastManager ToastManager { get; }

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

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    public IReadOnlyList<UserLabDto> AvailableLabs => _appState.AvailableLabs;

    public IReadOnlyList<NavigationItem> NavigationItems =>
        [.. _navigationService.NavigationItems.Where(IsNavigationItemVisible)];

    public bool HasSelectedLab => _appState.CurrentLabId != Guid.Empty;

    public bool IsLabSelectorVisible => _appState.AvailableLabs.Count > 1;

    public ShellViewModel(
        INavigationService navigationService,
        AppState appState,
        IServiceProvider serviceProvider,
        SettingsUpdater settingsUpdater,
        ISukiToastManager toastManager,
        ISettingsStore settingsStore,
        AuthSessionManager authSessionManager)
    {
        _navigationService = navigationService;
        _appState = appState;
        _serviceProvider = serviceProvider;
        _settingsUpdater = settingsUpdater;
        ToastManager = toastManager;
        _settingsStore = settingsStore;
        _authSessionManager = authSessionManager;

        _navigationService.CurrentViewChanged += (_, vm) =>
        {
            CurrentView = vm;
            SyncSelectedNavigationItem();
        };
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
                OnPropertyChanged(nameof(NavigationItems));
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
                OnPropertyChanged(nameof(NavigationItems));
                EnsureLabScopedViewHasLabContext();
            }
            else if (args.PropertyName == nameof(AppState.SyncStatusText)
                || args.PropertyName == nameof(AppState.PendingSyncCount)
                || args.PropertyName == nameof(AppState.FailedSyncCount))
            {
                UpdateSyncStatus();
            }
        };

        CurrentLabName = _appState.CurrentLabName;
        SelectedLab = _appState.AvailableLabs.FirstOrDefault(lab => lab.LabId == _appState.CurrentLabId);
        UpdateAuthState();
        UpdateSyncStatus();
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        UpdateAuthState();
        await Task.CompletedTask;
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

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (_isSyncingSelectedNavigationItem || value is null)
        {
            return;
        }

        NavigateTo(value);
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

    private static bool IsLabScopedNavigationItem(NavigationItem item)
    {
        return item.ViewModelType == typeof(SampleListViewModel)
            || item.ViewModelType == typeof(TestResultListViewModel);
    }

    private bool IsNavigationItemVisible(NavigationItem item)
    {
        return !IsLabScopedNavigationItem(item) || HasSelectedLab;
    }

    private void EnsureLabScopedViewHasLabContext()
    {
        if (HasSelectedLab || CurrentView is null)
        {
            return;
        }

        var currentType = CurrentView.GetType();
        if (currentType == typeof(SampleListViewModel) || currentType == typeof(TestResultListViewModel))
        {
            _navigationService.NavigateTo<Features.Dashboard.DashboardViewModel>();
        }
    }

    private void SyncSelectedNavigationItem()
    {
        if (CurrentView is null)
        {
            return;
        }

        var currentType = CurrentView.GetType();
        var matchingItem = _navigationService.NavigationItems.FirstOrDefault(item => item.ViewModelType == currentType);

        _isSyncingSelectedNavigationItem = true;
        SelectedNavigationItem = matchingItem;
        _isSyncingSelectedNavigationItem = false;
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
        if ((item.ViewModelType == typeof(SampleListViewModel) || item.ViewModelType == typeof(TestResultListViewModel)) && !HasSelectedLab)
        {
            return;
        }

        _navigationService.NavigateTo(item);
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        _navigationService.NavigateTo<Features.Dashboard.DashboardViewModel>();
    }

    [RelayCommand]
    private void NavigateToSamples()
    {
        if (!HasSelectedLab)
        {
            return;
        }

        _navigationService.NavigateTo<SampleListViewModel>();
    }

    [RelayCommand]
    private void NavigateToTestResults()
    {
        if (!HasSelectedLab)
        {
            return;
        }

        _navigationService.NavigateTo<TestResultListViewModel>();
    }

    [RelayCommand]
    private void NavigateToSyncCenter()
    {
        _navigationService.NavigateTo<SyncCenterViewModel>();
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _authSessionManager.HandleLogoutAsync();
    }

    private void UpdateSyncStatus()
    {
        SyncStatus = $"{_appState.SyncStatusText} (pending: {_appState.PendingSyncCount}, failed: {_appState.FailedSyncCount})";
    }
}

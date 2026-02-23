using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Splash;
using SukiUI.Toasts;

namespace Quater.Desktop;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private ViewModelBase _currentContent;

    public ISukiToastManager ToastManager { get; }

    public MainWindowViewModel(
        ISukiToastManager toastManager,
        SplashViewModel splashVm,
        ILogger<MainWindowViewModel> logger)
    {
        ToastManager = toastManager;
        _currentContent = splashVm;
        _logger = logger;

        _logger.LogInformation("MainWindowViewModel created with SplashViewModel as initial content");

        // Listen for the switch
        splashVm.InitializationCompleted += (newVm) =>
        {
            _logger.LogInformation("InitializationCompleted event received, switching to {ViewModelType}", newVm.GetType().Name);
            CurrentContent = newVm;
            _logger.LogInformation("CurrentContent updated to {ViewModelType}", newVm.GetType().Name);
        };
    }
}

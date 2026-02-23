using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quater.Desktop.Core.Shell;
using Quater.Desktop.Core.Startup;
using Quater.Desktop.Features.Onboarding;

namespace Quater.Desktop.Core.Splash;

public sealed partial class SplashViewModel : ViewModelBase
{
    private readonly IApplicationStartupService _startupService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SplashViewModel> _logger;

    [ObservableProperty]
    private string _status = "Loading...";

    public event Action<ViewModelBase>? InitializationCompleted;

    public SplashViewModel(
        IApplicationStartupService startupService,
        IServiceProvider serviceProvider,
        ILogger<SplashViewModel> logger)
    {
        _startupService = startupService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            Status = "Initializing...";
            _logger.LogInformation("Starting application initialization");

            var result = await _startupService.InitializeAsync(ct);

            if (!result.IsSuccess)
            {
                Status = $"Error: {result.ErrorMessage}";
                _logger.LogError("Initialization failed: {Error}", result.ErrorMessage);
                return;
            }

            if (result.RequiresOnboarding)
            {
                _logger.LogInformation("Onboarding required");
                Status = "Starting onboarding...";
                
                var onboardingVm = _serviceProvider.GetRequiredService<OnboardingViewModel>();
                onboardingVm.OnboardingCompleted += OnOnboardingCompleted;
                InitializationCompleted?.Invoke(onboardingVm);
            }
            else
            {
                _logger.LogInformation("Initialization complete, showing shell");
                Status = "Ready!";
                
                var shellVm = _serviceProvider.GetRequiredService<ShellViewModel>();
                await shellVm.InitializeAsync(ct);
                InitializationCompleted?.Invoke(shellVm);
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            _logger.LogError(ex, "Initialization failed");
        }
    }

    private async void OnOnboardingCompleted(object? sender, EventArgs e)
    {
        if (sender is OnboardingViewModel onboardingVm)
        {
            onboardingVm.OnboardingCompleted -= OnOnboardingCompleted;
        }

        var result = await _startupService.InitializeAsync();
        if (result.IsSuccess && !result.RequiresOnboarding)
        {
            var shellVm = _serviceProvider.GetRequiredService<ShellViewModel>();
            await shellVm.InitializeAsync();
            InitializationCompleted?.Invoke(shellVm);
        }
    }
}

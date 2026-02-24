using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.Splash;
using Quater.Desktop.Core.Shell;
using Serilog;

namespace Quater.Desktop;

public class App : Application
{
    private IServiceProvider Services { get; set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Load settings BEFORE building service provider so API clients get correct configuration
        var settingsStore = new JsonSettingsStore();
        var settings = settingsStore.LoadAsync().GetAwaiter().GetResult();
        
        // Set GlobalConfiguration BEFORE API clients are created
        var backendUrl = settings.BackendUrl;
        if (!string.IsNullOrWhiteSpace(backendUrl))
        {
            var config = new Quater.Desktop.Api.Client.Configuration
            {
                BasePath = backendUrl
            };
            Quater.Desktop.Api.Client.GlobalConfiguration.Instance = config;
            Log.Information("Set GlobalConfiguration.Instance.BasePath to {BasePath}", config.BasePath);
        }

        var services = new ServiceCollection();

        services
            .AddQuaterLogging()
            .AddQuaterSettings()
            .AddQuaterAuth()
            .AddQuaterApiClients()
            .AddQuaterCore()
            .AddQuaterData()
            .AddQuaterFeatures()
            .AddQuaterStartup()
            .AddSingleton<SplashViewModel>()
            .AddSingleton<ShellViewModel>()
            .AddSingleton<MainWindowViewModel>();

        Services = services.BuildServiceProvider();
        Log.Information("Service provider built");

        Services.InitializeDatabase();
        Log.Information("Database initialized");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Log.Information("Desktop lifetime detected");
            DisableAvaloniaDataAnnotationValidation();

            Log.Information("Getting MainWindowViewModel");
            var mainVm = Services.GetRequiredService<MainWindowViewModel>();
            Log.Information("Creating MainWindow");
            var mainWindow = new MainWindow { DataContext = mainVm };
            
            desktop.MainWindow = mainWindow;
            Log.Information("Showing main window");
            mainWindow.Show();

            // Start initialization after window is shown
            Log.Information("Main window shown, starting initialization");
            var splashVm = Services.GetRequiredService<SplashViewModel>();
            
            // Run initialization on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                Log.Information("Calling SplashViewModel.InitializeAsync");
                await splashVm.InitializeAsync();
            });

            desktop.Exit += (_, _) => Log.CloseAndFlush();
        }
        else
        {
            Log.Warning("ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime");
        }

        Log.Information("Calling base.OnFrameworkInitializationCompleted");
        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}

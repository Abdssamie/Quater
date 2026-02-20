using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Shell;
using Quater.Desktop.Core.State;
using Serilog;

namespace Quater.Desktop;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services
            .AddQuaterLogging()
            .AddQuaterSettings()
            .AddQuaterAuth()
            .AddQuaterApiClients()
            .AddQuaterCore()
            .AddQuaterData()
            .AddQuaterFeatures()
            .AddSingleton<ShellViewModel>();

        Services = services.BuildServiceProvider();

        Services.InitializeDatabase();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            BindingPlugins.DataValidators.RemoveAt(0);

            Services.RegisterNavigation();

            var shellViewModel = Services.GetRequiredService<ShellViewModel>();
            var appState = Services.GetRequiredService<AppState>();
            appState.IsAuthenticated = false;

            desktop.MainWindow = new MainWindow
            {
                DataContext = shellViewModel
            };

            desktop.Exit += (_, _) => Log.CloseAndFlush();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quater.Desktop.Data;
using Quater.Desktop.ViewModels;
using Serilog;

namespace Quater.Desktop;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Configure services
        var services = new ServiceCollection();
        services.AddDbContext<QuaterLocalContext>(options =>
            options.UseSqlite("Data Source=quater.db"));
            
        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));

        // Register TimeProvider for testable time operations
        services.AddSingleton(TimeProvider.System);

        // Register application services
        services.AddScoped<Services.IReportService, Services.ReportService>();

        Services = services.BuildServiceProvider();

        // Migrate database
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<QuaterLocalContext>();
            try 
            {
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Migration failed");
            }
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validation from both Avalonia and CommunityToolkit
            BindingPlugins.DataValidators.RemoveAt(0);
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            
            desktop.Exit += (sender, e) => Log.CloseAndFlush();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

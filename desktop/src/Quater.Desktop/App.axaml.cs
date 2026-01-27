using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quater.Desktop.Data;
using Quater.Desktop.ViewModels;

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
        // Configure services
        var services = new ServiceCollection();
        services.AddDbContext<QuaterLocalContext>(options =>
            options.UseSqlite("Data Source=quater.db"));
            
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
                Console.WriteLine($"Migration failed: {ex.Message}");
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
        }

        base.OnFrameworkInitializationCompleted();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.State;
using Quater.Desktop.Data;
using Serilog;

namespace Quater.Desktop.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuaterCore(this IServiceCollection services)
    {
        services.AddSingleton<AppState>();
        services.AddSingleton<INavigationService, SukiNavigationService>();
        services.AddSingleton<IDialogService, SukiDialogService>();

        return services;
    }

    public static IServiceCollection AddQuaterData(this IServiceCollection services, string dbPath = "Data Source=quater.db")
    {
        services.AddDbContext<QuaterLocalContext>(options =>
            Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions.UseSqlite(options, dbPath));

        services.AddScoped<Data.Repositories.ISampleRepository, Data.Repositories.SampleRepository>();

        return services;
    }

    public static IServiceCollection AddQuaterFeatures(this IServiceCollection services)
    {
        services.AddTransient<Features.Dashboard.DashboardViewModel>();
        services.AddTransient<Features.Samples.List.SampleListViewModel>();

        return services;
    }

    public static IServiceCollection AddQuaterLogging(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: Serilog.RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        return services;
    }

    public static IServiceProvider InitializeDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuaterLocalContext>();

        try
        {
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration failed");
        }

        return services;
    }

    public static IServiceProvider RegisterNavigation(this IServiceProvider services)
    {
        var nav = services.GetRequiredService<INavigationService>();

        nav.RegisterRoute<Features.Dashboard.DashboardViewModel>(new(
            "Dashboard",
            "M13,3V9H21V3M13,21H21V11H13M3,21H11V15H3M3,13H11V3H3V13Z",
            typeof(Features.Dashboard.DashboardViewModel),
            0
        ));

        nav.RegisterRoute<Features.Samples.List.SampleListViewModel>(new(
            "Samples",
            "M18,17L21,22H3L6,17H18M18,17L14,5H10L6,17H18M15,4H9L8,4H9L12,1L15,4Z",
            typeof(Features.Samples.List.SampleListViewModel),
            1
        ));

        return services;
    }
}

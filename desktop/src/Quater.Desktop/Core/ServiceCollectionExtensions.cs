using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Auth.Storage;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.Startup;
using Quater.Desktop.Core.State;
using Quater.Desktop.Data;
using Serilog;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Quater.Desktop.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuaterCore(this IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddSerilog());
        services.AddSingleton<AppState>();
        services.AddSingleton<INavigationService, SukiNavigationService>();
        services.AddSingleton<ISukiToastManager, SukiToastManager>();
        services.AddSingleton<ISukiDialogManager, SukiDialogManager>();
        services.AddSingleton<IDialogService, SukiDialogService>();

        return services;
    }

    public static IServiceCollection AddQuaterData(this IServiceCollection services, string dbPath = "Data Source=quater.db")
    {
        services.AddDbContext<QuaterLocalContext>(options =>
            options.UseSqlite(dbPath));

        services.AddScoped<Data.Repositories.ISampleRepository, Data.Repositories.SampleRepository>();

        return services;
    }

    public static IServiceCollection AddQuaterFeatures(this IServiceCollection services)
    {
        services.AddTransient<Features.Dashboard.DashboardViewModel>();
        services.AddTransient<Features.Samples.List.SampleListViewModel>();
        services.AddTransient<Features.Auth.LoginViewModel>();
        services.AddTransient<Features.Onboarding.OnboardingViewModel>();

        return services;
    }

    public static IServiceCollection AddQuaterSettings(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<AppSettings>();
        services.AddSingleton<SettingsUpdater>();

        return services;
    }

    public static IServiceCollection AddQuaterAuth(this IServiceCollection services)
    {
        services.AddSingleton<ITokenStore, SecureFileTokenStore>();
        services.AddSingleton<OidcClientFactory>();

        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<AuthSessionManager>();

        return services;
    }

    public static IServiceCollection AddQuaterStartup(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationStartupService, ApplicationStartupService>();
        return services;
    }

    public static IServiceCollection AddQuaterApiClients(this IServiceCollection services)
    {
        services.AddSingleton<IAccessTokenCache, AccessTokenCache>();
        services.AddSingleton<IApiClientFactory, ApiClientFactory>();

        return services;
    }
    public static IServiceCollection AddQuaterLogging(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/quater-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        return services;
    }

    public static void InitializeDatabase(this IServiceProvider services)
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
    }
}

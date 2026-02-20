using Duende.IdentityModel.OidcClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Auth.Browser;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Auth.Storage;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.Settings;
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
        services.AddTransient<Features.Auth.LoginViewModel>();

        return services;
    }

    public static IServiceCollection AddQuaterSettings(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton(provider => provider.GetRequiredService<ISettingsStore>().LoadAsync().GetAwaiter().GetResult());
        services.AddSingleton<SettingsUpdater>();

        return services;
    }

    public static IServiceCollection AddQuaterAuth(this IServiceCollection services)
    {
        services.AddSingleton<ITokenStore, SecureFileTokenStore>();

        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<AppSettings>();
            var redirectUrl = "http://127.0.0.1:7890/callback";
            var browser = new LoopbackBrowser(redirectUrl);

            var options = new OidcClientOptions
            {
                Authority = settings.BackendUrl,
                ClientId = "quater-mobile-client",
                RedirectUri = redirectUrl,
                Scope = "openid profile email api offline_access",
                Browser = browser,
                Policy = new Policy
                {
                    RequireIdentityTokenSignature = false
                }
            };

            return new OidcClient(options);
        });

        services.AddSingleton<IAuthService, AuthService>();

        return services;
    }

    public static IServiceCollection AddQuaterApiClients(this IServiceCollection services)
    {
        services.AddSingleton<ApiHeaders>();

        services.AddSingleton(provider =>
        {
            var apiHeaders = provider.GetRequiredService<ApiHeaders>();
            var settings = provider.GetRequiredService<AppSettings>();

            Quater.Desktop.Api.Client.ApiClient.AccessTokenProvider = ct => apiHeaders.GetAccessTokenAsync(ct);
            Quater.Desktop.Api.Client.ApiClient.LabIdProvider = () => apiHeaders.GetLabId();

            var config = new Quater.Desktop.Api.Client.Configuration
            {
                BasePath = settings.BackendUrl
            };

            Quater.Desktop.Api.Client.GlobalConfiguration.Instance = config;
            return config;
        });

        services.AddSingleton(provider => new Quater.Desktop.Api.Api.AuthApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.UsersApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.SamplesApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.LabsApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.UserLabsApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.ParametersApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.TestResultsApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.AuditLogsApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.HealthApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.EmailVerificationApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.AuthorizationApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.PasswordApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));
        services.AddSingleton(provider => new Quater.Desktop.Api.Api.VersionApi(provider.GetRequiredService<Quater.Desktop.Api.Client.Configuration>()));

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

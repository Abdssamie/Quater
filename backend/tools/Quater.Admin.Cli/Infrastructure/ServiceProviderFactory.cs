using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quater.Admin.Cli.Services;
using Quater.Backend.Data;
using Quater.Shared.Models;

namespace Quater.Admin.Cli.Infrastructure;

/// <summary>
/// Factory for building the DI container with all required services.
/// </summary>
public static class ServiceProviderFactory
{
    public static ServiceProvider BuildServiceProvider()
    {
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();

        ConfigureServices(services, configuration);

        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Database connection string not found. Set ConnectionStrings__DefaultConnection environment variable.");

        services.AddDbContext<QuaterDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentityCore<User>(options =>
            {
                // Bind password settings from configuration
                var passwordConfig = configuration.GetSection("Identity:Password");
                options.Password.RequireDigit = passwordConfig.GetValue("RequireDigit", true);
                options.Password.RequireLowercase = passwordConfig.GetValue("RequireLowercase", true);
                options.Password.RequireUppercase = passwordConfig.GetValue("RequireUppercase", true);
                options.Password.RequireNonAlphanumeric = passwordConfig.GetValue("RequireNonAlphanumeric", true);
                options.Password.RequiredLength = passwordConfig.GetValue("RequiredLength", 8);

                // Bind user settings from configuration
                var userConfig = configuration.GetSection("Identity:User");
                options.User.RequireUniqueEmail = userConfig.GetValue("RequireUniqueEmail", true);

                // Bind sign-in settings from configuration
                var signInConfig = configuration.GetSection("Identity:SignIn");
                options.SignIn.RequireConfirmedEmail = signInConfig.GetValue("RequireConfirmedEmail", false);
                options.SignIn.RequireConfirmedAccount = signInConfig.GetValue("RequireConfirmedAccount", false);
            })
            .AddEntityFrameworkStores<QuaterDbContext>();

        // Application services
        services.AddScoped<UserManagementService>();
    }
}
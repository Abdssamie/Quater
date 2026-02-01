namespace Quater.Backend.Infrastructure.Email;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quater.Backend.Core.Interfaces;

/// <summary>
/// Extension methods for registering email services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add email infrastructure services to the DI container
    /// </summary>
    public static IServiceCollection AddEmailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register and validate configuration
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection(EmailSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register services
        services.AddSingleton<IEmailQueue, BackgroundEmailQueue>();
        services.AddSingleton<IEmailTemplateService, ScribanTemplateService>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>(); // Changed to Singleton to match EmailQueueProcessor lifetime

        // Register background service
        services.AddHostedService<EmailQueueProcessor>();

        return services;
    }
}

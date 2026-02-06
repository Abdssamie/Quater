namespace Quater.Backend.Api.Extensions;

public static class ConfigurationValidationExtensions
{
    public static void ValidateConfiguration(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var config = app.Configuration;

        logger.LogInformation("Validating configuration...");

        var errors = new List<string>();

        // Database
        if (string.IsNullOrEmpty(config.GetConnectionString("DefaultConnection")))
            errors.Add("Database connection string 'DefaultConnection' is not configured");

        // OpenIddict
        if (string.IsNullOrEmpty(config["OpenIddict:Issuer"]))
            errors.Add("OpenIddict:Issuer is not configured");
        if (string.IsNullOrEmpty(config["OpenIddict:Audience"]))
            errors.Add("OpenIddict:Audience is not configured");

        // Redis
        if (string.IsNullOrEmpty(config["Redis:ConnectionString"]))
            errors.Add("Redis:ConnectionString is not configured");

        // Email
        if (string.IsNullOrEmpty(config["Email:SmtpHost"]))
            errors.Add("Email:SmtpHost is not configured");
        if (string.IsNullOrEmpty(config["Email:FromAddress"]))
            errors.Add("Email:FromAddress is not configured");
        if (string.IsNullOrEmpty(config["Email:FrontendUrl"]))
            errors.Add("Email:FrontendUrl is not configured");

        // CORS (only in production - skip for Development and Testing)
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            var corsOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (corsOrigins == null || corsOrigins.Length == 0)
                errors.Add("Cors:AllowedOrigins is not configured for production");
        }

        if (errors.Any())
        {
            logger.LogCritical("Configuration validation failed:");
            foreach (var error in errors)
            {
                logger.LogCritical("  - {Error}", error);
            }
            throw new InvalidOperationException(
                $"Configuration validation failed with {errors.Count} error(s). See logs for details.");
        }

        logger.LogInformation("Configuration validation passed ✓");
    }
}

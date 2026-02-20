using Asp.Versioning;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.OpenApi.MicrosoftExtensions;
using Quater.Backend.Api.BackgroundServices;
using Quater.Backend.Api.Extensions;
using Quater.Backend.Api.Infrastructure;
using Quater.Backend.Api.Middleware;
using Quater.Backend.Api.Seeders;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Backend.Data.Seeders;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Models;

namespace Quater.Backend.Api;

/// <summary>
/// Configures services and the application's request pipeline.
/// </summary>
public sealed class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    private IConfiguration Configuration { get; } = configuration;
    private IWebHostEnvironment Environment { get; } = environment;

    /// <summary>
    /// Configures application services including authentication, database, API controllers, and Swagger.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // Add services to the container.
        services.AddControllers(options =>
        {
            // Use snake_case for route naming (e.g., /api/audit_logs instead of /api/AuditLogs)
            options.Conventions.Add(new Microsoft.AspNetCore.Mvc.ApplicationModels.RouteTokenTransformerConvention(
                new SlugifyParameterTransformer()));
        });
        services.AddRazorPages();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Quater Water Quality Management API",
                Version = "v1",
                Description = "REST API for managing water quality testing data, compliance calculations, and laboratory operations",
                Contact = new OpenApiContact
                {
                    Name = "Quater Development Team",
                    Email = "support@quater.app"
                }
            });

            // Enable XML comments for better API documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add security definition for Bearer token authentication
            var bearerScheme = new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    []
                }
            });
        });

        // Configure API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Register services using extension methods
        services.AddInfrastructureServices(Configuration, Environment);
        services.AddDatabaseServices(Configuration, Environment);
        services.AddAuthenticationServices(Configuration, Environment);
        services.AddApplicationServices();
        services.AddHostedService<InvitationExpirationService>();

        // NOTE: Quartz.NET and AuditLogArchivalJob removed - audit log archival will be implemented
        // in a future phase when archival strategy is finalized. For now, audit logs are retained
        // indefinitely. Consider implementing database partitioning or manual archival process.
    }

    /// <summary>
    /// Configures the HTTP request pipeline including middleware, routing, and database seeding.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <param name="appEnvironment">The hosting environment.</param>
    public void Configure(WebApplication app, IWebHostEnvironment appEnvironment)
    {
        app.ValidateConfiguration();

        // Add global exception handler (must be first in pipeline)
        app.UseGlobalExceptionHandler();
        app.UseForwardedHeaders();

        // HTTPS redirection and HSTS (before rate limiting to prevent HTTP bypass)
        app.UseHttpsRedirection();
        app.UseHsts();

        // Add security headers middleware
        app.UseSecurityHeaders();

        // Add rate limiting middleware (after HTTPS redirect)
        app.UseRateLimiting();

        // Configure CORS (must be before authentication/authorization)
        app.UseCors("QuaterCorsPolicy");

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapSwagger().AllowAnonymous();  // Allow anonymous access to Swagger endpoints
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseLabContext();
        app.UseAuthorization();

        app.MapControllers();
        app.MapRazorPages();

        // Email queue health check endpoint
        app.MapGet("/health/email", (IEmailQueue queue) =>
        {
            var bgQueue = (BackgroundEmailQueue)queue;
            return Results.Ok(new
            {
                queueSize = bgQueue.ApproximateCount,
                status = bgQueue.ApproximateCount < 90 ? "healthy" : "warning",
                timestamp = DateTime.UtcNow
            });
        }).RequireAuthorization();

        // Apply migrations and seed database on startup (skip in Testing environment)
        if (!appEnvironment.IsEnvironment("Testing"))
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<QuaterDbContext>();
                var userManager = services.GetRequiredService<UserManager<User>>();
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<Program>>();

                // Apply migrations
                context.Database.Migrate();

                // Seed database
                DatabaseSeeder.SeedAsync(context, userManager, configuration, logger).GetAwaiter().GetResult();

                // Seed OpenIddict client applications
                OpenIddictSeeder.SeedAsync(services).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating or seeding the database.");
            }
        }
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Quater.Backend.Api.Extensions;
using Quater.Backend.Api.Middleware;
using Quater.Backend.Api.Seeders;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Backend.Data.Seeders;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Models;
using Sentry;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog to read from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Sentry
builder.WebHost.UseSentry(o =>
{
    o.Dsn = "https://1bfe6017932565499080e1ff518bbb17@o4509589925527552.ingest.de.sentry.io/4510886764478545";
    // When configuring for the first time, to see what the SDK is doing:
    o.Debug = true;
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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

    // Add OAuth2 security definition for Swagger UI
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("/api/auth/authorize", UriKind.Relative),
                TokenUrl = new Uri("/api/auth/token", UriKind.Relative),
                Scopes = new Dictionary<string, string>
                {
                    { "api", "Access to Quater API" },
                    { "openid", "OpenID Connect" },
                    { "profile", "User profile" },
                    { "email", "User email" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "api" }
        }
    });
});

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register services using extension methods
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
builder.Services.AddAuthenticationServices(builder.Configuration, builder.Environment);
builder.Services.AddApplicationServices();

// NOTE: Quartz.NET and AuditLogArchivalJob removed - audit log archival will be implemented
// in a future phase when archival strategy is finalized. For now, audit logs are retained
// indefinitely. Consider implementing database partitioning or manual archival process.

var app = builder.Build();

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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("quater-swagger-client");
        options.OAuthUsePkce();
    });
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

// Sentry verification endpoint - use this to test Sentry integration
// Returns immediately, check your Sentry dashboard after calling this endpoint
app.MapGet("/sentry-test", () =>
{
    try
    {
        // Send test message with additional context
        SentrySdk.CaptureMessage("Hello Sentry - Quater API is online!", SentryLevel.Info);
        
        // Also log a test error to verify error capture works
        SentrySdk.CaptureException(new Exception("Test error - please ignore"), scope =>
        {
            scope.SetTag("test", "true");
            scope.SetExtra("timestamp", DateTime.UtcNow);
        });
        
        return Results.Ok(new 
        { 
            message = "Test events sent to Sentry",
            timestamp = DateTime.UtcNow,
            instruction = "Check your Sentry dashboard for events"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to send test event: {ex.Message}");
    }
});

// Sentry health check - shows if SDK is initialized
app.MapGet("/health/sentry", () =>
{
    var isEnabled = SentrySdk.IsEnabled;
    var lastEventId = SentrySdk.LastEventId.ToString();
    
    return Results.Ok(new
    {
        enabled = isEnabled,
        lastEventId = string.IsNullOrEmpty(lastEventId) ? null : lastEventId,
        dsnConfigured = true, // Hardcoded DSN is configured
        timestamp = DateTime.UtcNow
    });
});

// Apply migrations and seed database on startup (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<QuaterDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        // Apply migrations
        context.Database.Migrate();

        // Seed database
        await DatabaseSeeder.SeedAsync(context, userManager, configuration, logger);

        // Seed OpenIddict client applications
        await OpenIddictSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw; // Re-throw to fail fast in production
    }
}

app.Run();
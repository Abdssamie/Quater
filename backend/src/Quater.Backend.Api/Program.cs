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
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog to read from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

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

    // Add security definition for Bearer token authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
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
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<QuaterDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        // Apply migrations
        context.Database.Migrate();

        // Seed database
        await DatabaseSeeder.SeedAsync(context, userManager);

        // Seed OpenIddict client applications
        await OpenIddictSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
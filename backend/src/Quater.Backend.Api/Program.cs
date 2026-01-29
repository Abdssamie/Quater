using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using Quartz;
using Quater.Backend.Api.Jobs;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Core.Models;
using Quater.Backend.Data;
using Quater.Backend.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<QuaterDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection") ??
        "Host=localhost;Database=quater;Username=postgres;Password=postgres");

    // Register the entity sets needed by OpenIddict.
    options.UseOpenIddict();
});

// Add Identity with password requirements
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<QuaterDbContext>()
    .AddDefaultTokenProviders();

// Add OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<QuaterDbContext>();
    })
    .AddServer(options =>
    {
        // Enable the token endpoint
        options.SetTokenEndpointUris("/api/auth/token");

        // Enable the password and refresh token flows
        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        // Accept anonymous clients (no client authentication required)
        options.AcceptAnonymousClients();

        // Register signing and encryption credentials (RS256 for JWT)
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            // TODO: In production, use real certificates
            // options.AddEncryptionCertificate(...)
            //        .AddSigningCertificate(...);
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }

        // Configure token lifetimes
        options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Register TimeProvider
builder.Services.AddSingleton(TimeProvider.System);

// Register Services
builder.Services.AddScoped<ISampleService, SampleService>();

// Configure Quartz.NET
builder.Services.AddQuartz(q =>
{
    // Create a job key for the audit log archival job
    var jobKey = new JobKey("AuditLogArchivalJob");

    q.AddJob<AuditLogArchivalJob>(opts => opts.WithIdentity(jobKey));

    // Schedule the job to run nightly at 2 AM
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("AuditLogArchivalJob-trigger")
        .WithCronSchedule("0 0 2 * * ?") // Run at 2:00 AM every day
    );
});

// Add Quartz.NET hosted service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<QuaterDbContext>();
        context.Database.Migrate();

        // Seed initial data
        var userManager = services.GetRequiredService<UserManager<User>>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var seederLogger = loggerFactory.CreateLogger<Quater.Backend.Api.Services.DatabaseSeeder>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var seeder = new Quater.Backend.Api.Services.DatabaseSeeder(context, userManager, seederLogger, timeProvider, configuration);
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();

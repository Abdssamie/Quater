using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using Quartz;
using Quater.Backend.Api.Jobs;
using Quater.Backend.Api.Middleware;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;
using Quater.Backend.Data;
using Quater.Backend.Data.Interceptors;
using Quater.Backend.Data.Interfaces;
using Quater.Backend.Data.Repositories;
using Quater.Backend.Data.Seeders;
using Quater.Backend.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with Console, File, and PostgreSQL sinks
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Host=localhost;Database=quater;Username=postgres;Password=postgres";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.PostgreSQL(
        connectionString: connectionString,
        tableName: "Logs",
        needAutoCreateTable: true,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for desktop and mobile clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("QuaterCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",  // Desktop app
                "http://localhost:5001",  // Desktop app (alternate)
                "capacitor://localhost",  // Mobile app (Capacitor)
                "ionic://localhost",      // Mobile app (Ionic)
                "http://localhost",       // Mobile app (local dev)
                "http://localhost:8100"   // Mobile app (Ionic dev server)
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition"); // For file downloads
    });
});

// Register HttpContextAccessor for CurrentUserService
builder.Services.AddHttpContextAccessor();

// Register CurrentUserService for audit trail
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Register EF Core Interceptors
builder.Services.AddScoped<SoftDeleteInterceptor>();
builder.Services.AddScoped<AuditTrailInterceptor>(sp =>
{
    var currentUserService = sp.GetRequiredService<ICurrentUserService>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var ipAddress = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
    return new AuditTrailInterceptor(currentUserService, ipAddress);
});

// Add DbContext with interceptors
builder.Services.AddDbContext<QuaterDbContext>((sp, options) =>
{
    var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
    var auditTrailInterceptor = sp.GetRequiredService<AuditTrailInterceptor>();
    
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? 
        "Host=localhost;Database=quater;Username=postgres;Password=postgres");
    
    // Register the entity sets needed by OpenIddict.
    options.UseOpenIddict();
    
    // Add interceptors
    options.AddInterceptors(softDeleteInterceptor, auditTrailInterceptor);
});

// Configure ASP.NET Core Identity with lockout settings
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // Lockout settings - 5 failed attempts, 15 minute lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<QuaterDbContext>()
    .AddDefaultTokenProviders();

// Configure OpenIddict for OAuth2/OIDC authentication
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<QuaterDbContext>();
    })
    .AddServer(options =>
    {
        // Configure token endpoints
        options.SetTokenEndpointUris("/api/auth/token")
               .SetUserinfoEndpointUris("/api/auth/userinfo");

        // Enable OAuth2/OIDC flows
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();

        // Accept anonymous clients (no client_id/client_secret required)
        options.AcceptAnonymousClients();

        // Configure token lifetimes
        options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));

        // Register scopes
        options.RegisterScopes("api", "offline_access");

        // Use development certificates (replace with real certificates in production)
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Enable ASP.NET Core integration
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough()
               .DisableTransportSecurityRequirement(); // Only for development
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Register TimeProvider
builder.Services.AddSingleton(TimeProvider.System);

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Quater.Backend.Core.Validators.SampleValidator>();

// Configure role-based authorization policies
builder.Services.AddAuthorization(options =>
{
    // AdminOnly policy - requires Admin role
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "role" && c.Value == "Admin")));
    
    // TechnicianOrAbove policy - requires Technician or Admin role
    options.AddPolicy("TechnicianOrAbove", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "role" && 
                (c.Value == "Admin" || c.Value == "Technician"))));
    
    // ViewerOrAbove policy - requires any authenticated user (Viewer, Technician, or Admin)
    options.AddPolicy("ViewerOrAbove", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "role" && 
                (c.Value == "Admin" || c.Value == "Technician" || c.Value == "Viewer"))));
});

// Register Services
builder.Services.AddScoped<ISampleService, SampleService>();
builder.Services.AddScoped<ITestResultService, TestResultService>();
builder.Services.AddScoped<IParameterService, ParameterService>();

// TODO: Uncomment when Sync services are implemented by other agents
// builder.Services.AddScoped<ISyncService, Quater.Backend.Sync.SyncService>();
// builder.Services.AddScoped<ISyncLogService, Quater.Backend.Sync.SyncLogService>();
// builder.Services.AddScoped<IBackupService, Quater.Backend.Sync.BackupService>();
// builder.Services.AddScoped<IConflictResolver, Quater.Backend.Sync.ConflictResolver>();

// TODO: Uncomment when services are implemented by other agents
// builder.Services.AddScoped<ILabService, LabService>();
// builder.Services.AddScoped<IUserService, UserService>();
// builder.Services.AddScoped<IComplianceCalculator, ComplianceCalculator>();

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

// Add global exception handler (must be first in pipeline)
app.UseGlobalExceptionHandler();

// Configure CORS (must be before authentication/authorization)
app.UseCors("QuaterCorsPolicy");

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

// Apply migrations and seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<QuaterDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        
        // Apply migrations
        context.Database.Migrate();
        
        // Seed database
        await DatabaseSeeder.SeedAsync(context, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();

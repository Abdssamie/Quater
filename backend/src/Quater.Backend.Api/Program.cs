using FluentValidation;
using Microsoft.AspNetCore.Identity;
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

// Add Identity
builder.Services.AddIdentity<User, IdentityRole>()
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
        options.SetTokenEndpointUris("/connect/token");

        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();

        options.AcceptAnonymousClients();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

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

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Quater.Backend.Core.Validators.SampleValidator>();

// Register Services
builder.Services.AddScoped<ISampleService, SampleService>();
builder.Services.AddScoped<ITestResultService, TestResultService>();
builder.Services.AddScoped<IParameterService, ParameterService>();
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

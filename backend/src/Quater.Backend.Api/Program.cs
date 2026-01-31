using Asp.Versioning;
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
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog to read from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Quater Water Quality Management API",
        Version = "v1",
        Description = "REST API for managing water quality testing data, compliance calculations, and laboratory operations",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
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
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
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

// Register Redis connection for rate limiting
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString") 
        ?? "localhost:6379,abortConnect=false";
    return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
});

// Configure CORS for desktop and mobile clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("QuaterCorsPolicy", policy =>
    {
        // Get allowed origins from configuration
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:5000" }; // Fallback for development
        
        // In production, validate that all origins use HTTPS
        if (!builder.Environment.IsDevelopment())
        {
            var httpOrigins = allowedOrigins.Where(o => o.StartsWith("http://", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (httpOrigins.Any())
            {
                throw new InvalidOperationException(
                    $"Production environment does not allow HTTP origins. Found: {string.Join(", ", httpOrigins)}. " +
                    "All origins must use HTTPS for security.");
            }
        }
        
        policy.WithOrigins(allowedOrigins)
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
    // Bind password settings from configuration
    var passwordConfig = builder.Configuration.GetSection("Identity:Password");
    options.Password.RequireDigit = passwordConfig.GetValue("RequireDigit", true);
    options.Password.RequireLowercase = passwordConfig.GetValue("RequireLowercase", true);
    options.Password.RequireUppercase = passwordConfig.GetValue("RequireUppercase", true);
    options.Password.RequireNonAlphanumeric = passwordConfig.GetValue("RequireNonAlphanumeric", true);
    options.Password.RequiredLength = passwordConfig.GetValue("RequiredLength", 8);
    
    // Bind lockout settings from configuration
    var lockoutConfig = builder.Configuration.GetSection("Identity:Lockout");
    var lockoutTimeSpan = lockoutConfig.GetValue("DefaultLockoutTimeSpan", "00:15:00");
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Parse(lockoutTimeSpan);
    options.Lockout.MaxFailedAccessAttempts = lockoutConfig.GetValue("MaxFailedAccessAttempts", 5);
    options.Lockout.AllowedForNewUsers = lockoutConfig.GetValue("AllowedForNewUsers", true);
    
    // Bind user settings from configuration
    var userConfig = builder.Configuration.GetSection("Identity:User");
    options.User.RequireUniqueEmail = userConfig.GetValue("RequireUniqueEmail", true);
    
    // Bind sign-in settings from configuration
    var signInConfig = builder.Configuration.GetSection("Identity:SignIn");
    options.SignIn.RequireConfirmedEmail = signInConfig.GetValue("RequireConfirmedEmail", false);
    options.SignIn.RequireConfirmedAccount = signInConfig.GetValue("RequireConfirmedAccount", false);
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

        // Configure certificates based on environment
        if (builder.Environment.IsDevelopment())
        {
            // Use development certificates in development only
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            // In production, load certificates from configuration
            // Certificates should be stored in Azure Key Vault or secure certificate store
            var encryptionCertPath = builder.Configuration["OpenIddict:EncryptionCertificatePath"];
            var signingCertPath = builder.Configuration["OpenIddict:SigningCertificatePath"];
            var certPassword = builder.Configuration["OpenIddict:CertificatePassword"];
            
            if (string.IsNullOrEmpty(encryptionCertPath) || string.IsNullOrEmpty(signingCertPath))
            {
                throw new InvalidOperationException(
                    "Production environment requires OpenIddict:EncryptionCertificatePath and OpenIddict:SigningCertificatePath " +
                    "to be configured. Certificates should be stored securely (e.g., Azure Key Vault).");
            }
            
            // Load certificates from file system (in production, use Azure Key Vault or similar)
            options.AddEncryptionCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(
                encryptionCertPath, certPassword))
                   .AddSigningCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(
                signingCertPath, certPassword));
        }

        // Enable ASP.NET Core integration
        var aspNetCoreBuilder = options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough();
        
        // Only disable transport security in development (allows HTTP)
        // In production, HTTPS is required for OAuth2 security
        if (builder.Environment.IsDevelopment())
        {
            aspNetCoreBuilder.DisableTransportSecurityRequirement();
        }
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
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IComplianceCalculator, ComplianceCalculator>();

// TODO: Uncomment when Sync services are implemented by other agents
// builder.Services.AddScoped<ISyncService, Quater.Backend.Sync.SyncService>();
// builder.Services.AddScoped<ISyncLogService, Quater.Backend.Sync.SyncLogService>();
// builder.Services.AddScoped<IBackupService, Quater.Backend.Sync.BackupService>();
// builder.Services.AddScoped<IConflictResolver, Quater.Backend.Sync.ConflictResolver>();

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

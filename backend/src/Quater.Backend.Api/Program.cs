using System.Security.Cryptography.X509Certificates;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Quater.Backend.Api.Middleware;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Models;
using Quater.Backend.Data;
using Quater.Backend.Data.Interceptors;
using Quater.Backend.Data.Seeders;
using Quater.Backend.Services;
using Quater.Backend.Api.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// TODO: [MEDIUM PRIORITY] Extract service configuration into extension methods (Est: 2 hours)
// Program.cs is verbose (400+ lines) with all service registration inline.
// Consider creating extension methods in separate files:
//   - AddAuthenticationServices()
//   - AddDatabaseServices()
//   - AddApplicationServices()
//   - AddInfrastructureServices()
// This works fine but could improve readability.

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

    // In .NET 9, use KnownNetworks (in .NET 10+ it was renamed to KnownIPNetworks)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register Redis connection for rate limiting
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    // Throws error if not provided
    var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString")!;

    return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
});

// Configure CORS for desktop and mobile clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("QuaterCorsPolicy", policy =>
    {
        // Get allowed origins from configuration
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        // If not configured, use environment-specific defaults
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            if (builder.Environment.IsDevelopment())
            {
                // Allow localhost in development
                allowedOrigins = ["http://localhost:5000", "http://localhost:5173"];
            }
            else
            {
                // In production, CORS must be explicitly configured
                throw new InvalidOperationException(
                    "CORS:AllowedOrigins is not configured. " +
                    "Please set CORS_ORIGIN_1, CORS_ORIGIN_2, CORS_ORIGIN_3 environment variables.");
            }
        }

        // In production, validate that all origins use HTTPS
        if (!builder.Environment.IsDevelopment())
        {
            var httpOrigins = allowedOrigins.Where(o => o.StartsWith("http://", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (httpOrigins.Length != 0)
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
builder.Services.AddScoped<AuditInterceptor>(sp =>
{
    var currentUserService = sp.GetRequiredService<ICurrentUserService>();
    var timeProvider = sp.GetRequiredService<TimeProvider>();
    return new AuditInterceptor(currentUserService, timeProvider);
});
builder.Services.AddScoped<AuditTrailInterceptor>(sp =>
{
    var currentUserService = sp.GetRequiredService<ICurrentUserService>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    return new AuditTrailInterceptor(currentUserService, ipAddress);
});

// Add DbContext with interceptors
builder.Services.AddDbContext<QuaterDbContext>((sp, options) =>
{
    var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
    var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
    var auditTrailInterceptor = sp.GetRequiredService<AuditTrailInterceptor>();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Database connection string 'DefaultConnection' is not configured. " +
            "Please set the following environment variables: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD");

    options.UseNpgsql(connectionString);

    // Register the entity sets needed by OpenIddict.
    options.UseOpenIddict();

    // Add interceptors
    options.AddInterceptors(softDeleteInterceptor, auditInterceptor, auditTrailInterceptor);
});

// Configure ASP.NET Core Identity with lockout settings
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
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
               .SetUserinfoEndpointUris("/api/auth/userinfo")
               .SetRevocationEndpointUris("/api/auth/revoke");

        // Enable OAuth2/OIDC flows
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();

        // Read token configuration from OpenIddict section
        var openIddictConfig = builder.Configuration.GetSection("OpenIddict");
        var accessTokenLifetimeSeconds = openIddictConfig.GetValue("AccessTokenLifetime", 3600);
        var refreshTokenLifetimeSeconds = openIddictConfig.GetValue("RefreshTokenLifetime", 604800);
        var refreshTokenLeewaySeconds = openIddictConfig.GetValue("RefreshTokenReuseLeewaySeconds", 30);

        // Configure token lifetimes
        options.SetAccessTokenLifetime(TimeSpan.FromSeconds(accessTokenLifetimeSeconds));
        options.SetRefreshTokenLifetime(TimeSpan.FromSeconds(refreshTokenLifetimeSeconds));

        // Enable refresh token rotation (security best practice - RFC 6819)
        // When a refresh token is used, a new one is issued and the old one is invalidated
        // The leeway allows for concurrent requests during token refresh
        options.SetRefreshTokenReuseLeeway(TimeSpan.FromSeconds(refreshTokenLeewaySeconds));
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
            // In production, load certificates from file system
            // Use X509CertificateLoader.LoadPkcs12FromFile (modern .NET API)
            var encryptionCertPath = builder.Configuration["OpenIddict:EncryptionCertificatePath"]
                ?? throw new InvalidOperationException("OpenIddict:EncryptionCertificatePath is required in production");
            var encryptionCertPassword = builder.Configuration["OpenIddict:EncryptionCertificatePassword"];

            var signingCertPath = builder.Configuration["OpenIddict:SigningCertificatePath"]
                ?? throw new InvalidOperationException("OpenIddict:SigningCertificatePath is required in production");
            var signingCertPassword = builder.Configuration["OpenIddict:SigningCertificatePassword"];

            var encryptionCert = X509CertificateLoader.LoadPkcs12FromFile(
                encryptionCertPath,
                encryptionCertPassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            var signingCert = X509CertificateLoader.LoadPkcs12FromFile(
                signingCertPath,
                signingCertPassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            options.AddEncryptionCertificate(encryptionCert)
                   .AddSigningCertificate(signingCert);
        }

        // Enable ASP.NET Core integration
        var aspNetCoreBuilder = options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableUserinfoEndpointPassthrough();

        // Note: Revocation endpoint is handled automatically by OpenIddict when
        // SetRevocationEndpointUris is configured - no passthrough needed

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

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Quater.Backend.Core.Validators.SampleValidator>();
builder.Services.AddScoped<IValidator<Lab>, Quater.Backend.Core.Validators.LabValidator>();
builder.Services.AddScoped<IValidator<Parameter>, Quater.Backend.Core.Validators.ParameterValidator>();

// Configure role-based authorization policies
builder.Services.AddAuthorization(options =>
{
    // AdminOnly policy - requires Admin role
    options.AddPolicy(Policies.AdminOnly, policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == QuaterClaimTypes.Role && c.Value == Roles.Admin)));

    // TechnicianOrAbove policy - requires Technician or Admin role
    options.AddPolicy(Policies.TechnicianOrAbove, policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == QuaterClaimTypes.Role &&
                (c.Value == Roles.Admin || c.Value == Roles.Technician))));

    // ViewerOrAbove policy - requires any authenticated user (Viewer, Technician, or Admin)
    options.AddPolicy(Policies.ViewerOrAbove, policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == QuaterClaimTypes.Role &&
                (c.Value == Roles.Admin || c.Value == Roles.Technician || c.Value == Roles.Viewer))));
});

// Register Services
builder.Services.AddScoped<ISampleService, SampleService>();
builder.Services.AddScoped<ITestResultService, TestResultService>();
builder.Services.AddScoped<IParameterService, ParameterService>();
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IComplianceCalculator, ComplianceCalculator>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Register Email Infrastructure (from Infrastructure.Email project)
builder.Services.AddEmailInfrastructure(builder.Configuration);

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
app.UseAuthorization();

app.MapControllers();

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
using System.Security.Cryptography.X509Certificates;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Backend.Data.Constants;
using Quater.Backend.Data.Interceptors;
using Quater.Backend.Infrastructure.Email;
using Quater.Backend.Services;
using Quater.Shared.Models;

namespace Quater.Backend.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services including Redis, CORS, HttpContextAccessor, CurrentUserService, TimeProvider, and Email.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Register Redis connection for rate limiting
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            // Throws error if not provided
            var redisConnectionString = config.GetValue<string>("Redis:ConnectionString")!;

            return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
        });

        // Configure CORS for desktop and mobile clients
        services.AddCors(options =>
        {
            options.AddPolicy("QuaterCorsPolicy", policy =>
            {
                // Get allowed origins from configuration
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

                // If not configured, use environment-specific defaults
                if (allowedOrigins == null || allowedOrigins.Length == 0)
                {
                    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
                    {
                        // Allow localhost in development and testing
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
                if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
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
        services.AddHttpContextAccessor();

        // Register CurrentUserService for audit trail
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Register TimeProvider
        services.AddSingleton(TimeProvider.System);

        // Register Email Infrastructure (from Infrastructure.Email project)
        services.AddEmailInfrastructure(configuration);

        return services;
    }

    /// <summary>
    /// Adds database services including EF Core interceptors and DbContext.
    /// </summary>
    public static void AddDatabaseServices(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Register EF Core Interceptors
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<AuditInterceptor>(sp =>
        {
            var currentUserService = sp.GetRequiredService<ICurrentUserService>();
            var timeProvider = sp.GetRequiredService<TimeProvider>();
            return new AuditInterceptor(currentUserService, timeProvider);
        });
        services.AddScoped<AuditTrailInterceptor>(sp =>
        {
            var currentUserService = sp.GetRequiredService<ICurrentUserService>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            return new AuditTrailInterceptor(currentUserService, ipAddress);
        });

        // Add DbContext with factory pattern for RLS session variable management
        services.AddScoped<QuaterDbContext>(serviceProvider =>
        {
            var labContextAccessor = serviceProvider.GetRequiredService<ILabContextAccessor>();
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string 'DefaultConnection' is not configured. " +
                    "Please set the following environment variables: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD");

            // Create DbContextOptionsBuilder manually
            var optionsBuilder = new DbContextOptionsBuilder<QuaterDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            // Register the entity sets needed by OpenIddict
            optionsBuilder.UseOpenIddict();

            // Add interceptors
            var softDeleteInterceptor = serviceProvider.GetRequiredService<SoftDeleteInterceptor>();
            var auditInterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
            var auditTrailInterceptor = serviceProvider.GetRequiredService<AuditTrailInterceptor>();

            optionsBuilder.AddInterceptors(softDeleteInterceptor, auditInterceptor, auditTrailInterceptor);

            // Create the DbContext instance
            var context = new QuaterDbContext(optionsBuilder.Options);

            // Set RLS session variables immediately based on lab context
            // Only execute SQL if there's an actual context (system admin or lab context)
            // If no context is set, skip SQL execution (this is normal during initialization)
            if (labContextAccessor is { IsSystemAdmin: false, CurrentLabId: null }) return context;
            try
            {
                if (labContextAccessor.IsSystemAdmin)
                {
                    // System admin: bypass RLS
                    context.Database.ExecuteSqlRaw($"SELECT set_config('{RlsConstants.IsSystemAdminVariable}', {{0}}, false)", "true");
                    context.Database.ExecuteSqlRaw($"SELECT set_config('{RlsConstants.CurrentLabIdVariable}', {{0}}, false)", string.Empty);
                }
                else if (labContextAccessor.CurrentLabId.HasValue)
                {
                    // Lab context exists: set lab ID and clear system admin flag
                    var labId = labContextAccessor.CurrentLabId.Value;
                    context.Database.ExecuteSqlRaw($"SELECT set_config('{RlsConstants.CurrentLabIdVariable}', {{0}}, false)", labId);
                    context.Database.ExecuteSqlRaw($"SELECT set_config('{RlsConstants.IsSystemAdminVariable}', {{0}}, false)", string.Empty);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to set RLS session variables. Ensure PostgreSQL connection is healthy and user has permissions.",
                    ex);
            }

            return context;
        });
    }

    /// <summary>
    /// Adds authentication services including ASP.NET Core Identity, OpenIddict, and authorization policies.
    /// </summary>
    public static void AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Configure ASP.NET Core Identity with lockout settings
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            // Bind password settings from configuration
            var passwordConfig = configuration.GetSection("Identity:Password");
            options.Password.RequireDigit = passwordConfig.GetValue("RequireDigit", true);
            options.Password.RequireLowercase = passwordConfig.GetValue("RequireLowercase", true);
            options.Password.RequireUppercase = passwordConfig.GetValue("RequireUppercase", true);
            options.Password.RequireNonAlphanumeric = passwordConfig.GetValue("RequireNonAlphanumeric", true);
            options.Password.RequiredLength = passwordConfig.GetValue("RequiredLength", 8);

            // Bind lockout settings from configuration
            var lockoutConfig = configuration.GetSection("Identity:Lockout");
            var lockoutTimeSpan = lockoutConfig.GetValue("DefaultLockoutTimeSpan", "00:15:00");
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Parse(lockoutTimeSpan);
            options.Lockout.MaxFailedAccessAttempts = lockoutConfig.GetValue("MaxFailedAccessAttempts", 5);
            options.Lockout.AllowedForNewUsers = lockoutConfig.GetValue("AllowedForNewUsers", true);

            // Bind user settings from configuration
            var userConfig = configuration.GetSection("Identity:User");
            options.User.RequireUniqueEmail = userConfig.GetValue("RequireUniqueEmail", true);

            // Bind sign-in settings from configuration
            var signInConfig = configuration.GetSection("Identity:SignIn");
            options.SignIn.RequireConfirmedEmail = signInConfig.GetValue("RequireConfirmedEmail", false);
            options.SignIn.RequireConfirmedAccount = signInConfig.GetValue("RequireConfirmedAccount", false);
        })
            .AddEntityFrameworkStores<QuaterDbContext>()
            .AddDefaultTokenProviders();

        // Configure Identity application cookie for OAuth2 authorization code flow.
        // When the AuthorizationController challenges with IdentityConstants.ApplicationScheme,
        // unauthenticated users are redirected to the login page in the system browser.
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment() || environment.IsEnvironment("Testing")
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        // Configure authentication to use OpenIddict validation for Bearer tokens
        // This sets the default authentication scheme so [Authorize] attributes work with JWT tokens
        // Without this, AddIdentity() sets cookies as the default, causing Bearer token auth to fail
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        // Configure OpenIddict for OAuth2/OIDC authentication
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<QuaterDbContext>();
            })
            .AddServer(options =>
            {
                // Configure endpoints
                options.SetAuthorizationEndpointUris("/api/auth/authorize")
                       .SetTokenEndpointUris("/api/auth/token")
                       .SetUserInfoEndpointUris("/api/auth/userinfo")
                       .SetRevocationEndpointUris("/api/auth/revoke");

                // Enable OAuth2/OIDC flows
                options.AllowAuthorizationCodeFlow();
                options.AllowRefreshTokenFlow();

                // Accept anonymous clients (public clients like desktop/mobile apps)
                // Public clients use PKCE instead of client secrets for security
                options.AcceptAnonymousClients();

                // Read token configuration from OpenIddict section
                var openIddictConfig = configuration.GetSection("OpenIddict");
                var accessTokenLifetimeSeconds = openIddictConfig.GetValue("AccessTokenLifetime", 3600);
                var refreshTokenLifetimeSeconds = openIddictConfig.GetValue("RefreshTokenLifetime", 604800);
                var refreshTokenLeewaySeconds = openIddictConfig.GetValue("RefreshTokenReuseLeewaySeconds", 30);
                var authorizationCodeLifetimeSeconds = openIddictConfig.GetValue("AuthorizationCodeLifetime", 600);

                // Configure token lifetimes
                options.SetAccessTokenLifetime(TimeSpan.FromSeconds(accessTokenLifetimeSeconds));
                options.SetRefreshTokenLifetime(TimeSpan.FromSeconds(refreshTokenLifetimeSeconds));
                options.SetAuthorizationCodeLifetime(TimeSpan.FromSeconds(authorizationCodeLifetimeSeconds));

                // Enable refresh token rotation (security best practice - RFC 6819)
                // When a refresh token is used, a new one is issued and the old one is invalidated
                // The leeway allows for concurrent requests during token refresh
                options.SetRefreshTokenReuseLeeway(TimeSpan.FromSeconds(refreshTokenLeewaySeconds));
                // Register scopes
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.OfflineAccess,
                    "api");

                // Use JWT format for access tokens (instead of reference tokens)
                // Tokens are both signed (tamper-proof) and encrypted (confidential)
                // This ensures tokens cannot be read even if intercepted
                // Note: DisableAccessTokenEncryption() removed for production security

                // Configure certificates based on environment
                if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
                {
                    // Use development certificates in development and testing environments
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                }
                else
                {
                    // In production, load certificates from file system or environment variables
                    // Use X509CertificateLoader.LoadPkcs12FromFile (modern .NET API)
                    var encryptionCertPath = configuration["OpenIddict:EncryptionCertificatePath"]
                        ?? throw new InvalidOperationException("OpenIddict:EncryptionCertificatePath is required in production");
                    var encryptionCertPassword = configuration["OpenIddict:EncryptionCertificatePassword"];

                    var signingCertPath = configuration["OpenIddict:SigningCertificatePath"]
                        ?? throw new InvalidOperationException("OpenIddict:SigningCertificatePath is required in production");
                    var signingCertPassword = configuration["OpenIddict:SigningCertificatePassword"];

                    // Validate certificate paths exist
                    if (!File.Exists(encryptionCertPath))
                    {
                        throw new FileNotFoundException(
                            $"OpenIddict encryption certificate not found at: {encryptionCertPath}. " +
                            "Please ensure certificates are mounted correctly or set via environment variables.");
                    }

                    if (!File.Exists(signingCertPath))
                    {
                        throw new FileNotFoundException(
                            $"OpenIddict signing certificate not found at: {signingCertPath}. " +
                            "Please ensure certificates are mounted correctly or set via environment variables.");
                    }

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
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough();

                // Note: Revocation endpoint is handled automatically by OpenIddict when
                // SetRevocationEndpointUris is configured - no passthrough needed

                // Only disable transport security in development and testing (allows HTTP)
                // In production, HTTPS is required for OAuth2 security
                if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
                {
                    aspNetCoreBuilder.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Configure lab-context-aware authorization policies
        services.AddAuthorization(options =>
        {
            // Lab-context-aware policies - check user's role in the current lab
            options.AddPolicy(Policies.AdminOnly, policy =>
                policy.Requirements.Add(new Authorization.LabContextRoleRequirement(Shared.Enums.UserRole.Admin)));

            options.AddPolicy(Policies.TechnicianOrAbove, policy =>
                policy.Requirements.Add(new Authorization.LabContextRoleRequirement(Shared.Enums.UserRole.Technician)));

            options.AddPolicy(Policies.ViewerOrAbove, policy =>
                policy.Requirements.Add(new Authorization.LabContextRoleRequirement(Shared.Enums.UserRole.Viewer)));

            // Fallback policy: require authentication by default
            // Endpoints without [Authorize] attribute will require authentication
            // Endpoints must explicitly use [AllowAnonymous] to be public
            // This is a defense-in-depth security measure to prevent accidental exposure
            // 
            // IMPORTANT: Accept authentication from BOTH cookie (Razor Pages) and Bearer token (API) schemes
            // This allows the OAuth2 login page to work correctly while still protecting API endpoints
            options.FallbackPolicy = new AuthorizationPolicyBuilder(
                    IdentityConstants.ApplicationScheme,  // Cookie authentication for Razor Pages
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)  // Bearer tokens for API
                .RequireAuthenticatedUser()
                .Build();
        });

        // Register authorization handler
        services.AddScoped<IAuthorizationHandler, Authorization.LabContextAuthorizationHandler>();
    }

    /// <summary>
    /// Adds application services including business logic services and FluentValidation validators.
    /// </summary>
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssemblyContaining<Core.Validators.SampleValidator>();
        services.AddScoped<IValidator<Lab>, Core.Validators.LabValidator>();
        services.AddScoped<IValidator<Parameter>, Core.Validators.ParameterValidator>();

        // Register Services
        services.AddScoped<ISampleService, SampleService>();
        services.AddScoped<ITestResultService, TestResultService>();
        services.AddScoped<IParameterService, ParameterService>();
        services.AddScoped<ILabService, LabService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserLabService, UserLabService>();
        services.AddScoped<IComplianceCalculator, ComplianceCalculator>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Register Lab Context Accessor (scoped per request)
        services.AddScoped<ILabContextAccessor, LabContextAccessor>();
    }
}

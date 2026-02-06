using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Quater.Backend.Api.Tests.Fixtures;

/// <summary>
/// Shared test fixture for API integration tests.
/// Provides PostgreSQL and Redis test containers, and a configured WebApplicationFactory.
/// </summary>
public class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private RedisContainer _redisContainer = null!;
    
    public HttpClient Client { get; private set; } = null!;
    public IConnectionMultiplexer Redis { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("quater_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresContainer.StartAsync();

        // Start Redis container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Create HTTP client
        Client = CreateClient();
        
        // Get Redis connection
        Redis = Services.GetRequiredService<IConnectionMultiplexer>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration with all required settings
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Database (will be overridden with test container)
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                
                // OpenIddict
                ["OpenIddict:Issuer"] = "https://localhost:5001",
                ["OpenIddict:Audience"] = "quater-api",
                
                // Redis (will be overridden with test container)
                ["Redis:ConnectionString"] = "localhost:6379",
                
                // Email
                ["Email:SmtpHost"] = "localhost",
                ["Email:SmtpPort"] = "25",
                ["Email:FromAddress"] = "test@example.com",
                ["Email:FromName"] = "Test",
                ["Email:FrontendUrl"] = "http://localhost:3000",
                
                // Rate Limiting
                ["RateLimiting:AuthenticatedLimit"] = "1000",
                ["RateLimiting:AnonymousLimit"] = "1000",
                ["RateLimiting:WindowSeconds"] = "60",
                
                // Identity Password Settings (relaxed for testing)
                ["Identity:Password:RequireDigit"] = "true",
                ["Identity:Password:RequireLowercase"] = "true",
                ["Identity:Password:RequireUppercase"] = "true",
                ["Identity:Password:RequireNonAlphanumeric"] = "true",
                ["Identity:Password:RequiredLength"] = "12"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace PostgreSQL with test container
            services.RemoveAll<DbContextOptions<QuaterDbContext>>();
            services.AddDbContext<QuaterDbContext>(options =>
            {
                // Use test container connection string
                options.UseNpgsql(_postgresContainer.GetConnectionString());
                // Disable interceptors for tests to avoid audit trail issues
                // Interceptors will be tested separately in integration tests
            });

            // Replace Redis with test container
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));

            // Mock email service to prevent actual emails
            services.RemoveAll<IEmailQueue>();
            services.AddSingleton<IEmailQueue>(sp =>
            {
                var mock = new Moq.Mock<IEmailQueue>();
                mock.Setup(x => x.QueueAsync(
                    Moq.It.IsAny<Quater.Backend.Core.DTOs.EmailQueueItem>(),
                    Moq.It.IsAny<CancellationToken>()))
                    .Returns(ValueTask.CompletedTask);
                return mock.Object;
            });
        });

        // Use Testing environment to disable interceptors and avoid certificate requirements
        builder.UseEnvironment("Testing");
    }

    public new async Task DisposeAsync()
    {
        Client?.Dispose();
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Resets the database to a clean state for each test.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();
        
        // Ensure database is created with schema
        await context.Database.EnsureCreatedAsync();
        
        // Clear all data
        context.RemoveRange(context.Users);
        context.RemoveRange(context.Labs);
        context.RemoveRange(context.Samples);
        context.RemoveRange(context.Parameters);
        context.RemoveRange(context.TestResults);
        context.RemoveRange(context.AuditLogs);
        
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Clears all rate limiting keys from Redis.
    /// </summary>
    public async Task ClearRateLimitKeysAsync()
    {
        var db = Redis.GetDatabase();
    var server = Redis.GetServer(Redis.GetEndPoints().First());
        
        await foreach (var key in server.KeysAsync(pattern: "ratelimit:*"))
        {
            await db.KeyDeleteAsync(key);
        }
        
        await foreach (var key in server.KeysAsync(pattern: "endpoint-ratelimit:*"))
        {
            await db.KeyDeleteAsync(key);
        }
    }
}

using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Data;
using Quater.Backend.Data.Interceptors;
using Quater.Shared.Models;
using Quater.Shared.Enums;
using Testcontainers.PostgreSql;

namespace Quater.Backend.Core.Tests.Helpers;

/// <summary>
/// Provides PostgreSQL test containers for integration testing
/// </summary>
public class PostgresTestContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private string? _connectionString;

    public PostgresTestContainer()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("quater_test")
            .WithUsername("test")
            .WithPassword("test123")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _connectionString 
        ?? throw new InvalidOperationException("Container not initialized. Call InitializeAsync first.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Creates a new DbContext connected to the test container
    /// </summary>
    public QuaterDbContext CreateDbContext()
    {
        // Append Timezone=UTC to connection string to handle DateTime properly
        var connectionString = $"{ConnectionString};Timezone=UTC";
        
        var options = new DbContextOptionsBuilder<QuaterDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging()
            .AddInterceptors(
                new AuditTrailInterceptor(),
                new SoftDeleteInterceptor())
            .Options;

        var context = new QuaterDbContext(options);
        
        // Ensure database is created and migrations are applied
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Creates a new DbContext without interceptors (for seeding)
    /// </summary>
    private QuaterDbContext CreateDbContextWithoutInterceptors()
    {
        // Append Timezone=UTC to connection string to handle DateTime properly
        var connectionString = $"{ConnectionString};Timezone=UTC";
        
        var options = new DbContextOptionsBuilder<QuaterDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new QuaterDbContext(options);
        
        // Ensure database is created and migrations are applied
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Creates a new DbContext and seeds it with test data
    /// </summary>
    public QuaterDbContext CreateSeededDbContext()
    {
        // Seed data without interceptors to avoid foreign key issues
        using (var seedContext = CreateDbContextWithoutInterceptors())
        {
            SeedTestData(seedContext);
        }
        
        // Return a new context with interceptors for tests
        return CreateDbContext();
    }

    /// <summary>
    /// Resets the database to a clean state
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private static void SeedTestData(QuaterDbContext context)
    {
        // First, create a test lab for the System user
        var systemLab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "System Lab",
            Location = "System",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };
        context.Labs.Add(systemLab);
        context.SaveChanges();
        
        // Then create a "System" user for audit logs
        var systemUser = new User
        {
            Id = "System",
            Email = "system@quater.test",
            PasswordHash = "N/A",
            Role = UserRole.Admin,
            LabId = systemLab.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };
        context.Users.Add(systemUser);
        context.SaveChanges();
        
        var testData = MockDataFactory.CreateTestDataSet();
        
        context.Labs.AddRange(testData.Labs);
        context.SaveChanges();
        
        context.Parameters.AddRange(testData.Parameters);
        context.SaveChanges();
        
        context.Samples.AddRange(testData.Samples);
        context.SaveChanges();
        
        context.TestResults.AddRange(testData.TestResults);
        context.SaveChanges();
    }
}

/// <summary>
/// Shared test container fixture for xUnit collection
/// </summary>
public class PostgresTestContainerFixture : IAsyncLifetime
{
    public PostgresTestContainer Container { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new PostgresTestContainer();
        await Container.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (Container != null)
        {
            await Container.DisposeAsync();
        }
    }
}

/// <summary>
/// xUnit collection definition for sharing the container across tests
/// </summary>
[CollectionDefinition("PostgreSQL")]
public class PostgresCollection : ICollectionFixture<PostgresTestContainerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

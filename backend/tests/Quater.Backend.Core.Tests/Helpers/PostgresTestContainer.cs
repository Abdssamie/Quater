using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quater.Backend.Data;
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
        var context = CreateDbContext();
        SeedTestData(context);
        return context;
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

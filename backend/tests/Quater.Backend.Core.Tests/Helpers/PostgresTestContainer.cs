using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Data;
using Quater.Backend.Data.Interceptors;
using Quater.Shared.Models;
using Quater.Shared.Enums;
using Testcontainers.PostgreSql;

namespace Quater.Backend.Core.Tests.Helpers;

/// <summary>
/// [DEPRECATED] Use TestDbContextFactory instead.
/// This class is kept for backward compatibility but delegates to TestDbContextFactory.
/// </summary>
public class PostgresTestContainer : IAsyncLifetime
{
    private readonly TestDbContextFactory _factory;

    public PostgresTestContainer()
    {
        _factory = new TestDbContextFactory();
    }

    public string ConnectionString => _factory.ConnectionString;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Creates a new DbContext connected to the test container
    /// </summary>
    public QuaterDbContext CreateDbContext()
    {
        return _factory.CreateContext();
    }

    /// <summary>
    /// Creates a new DbContext without interceptors (for seeding)
    /// </summary>
    private QuaterDbContext CreateDbContextWithoutInterceptors()
    {
        return _factory.CreateContextWithoutInterceptors();
    }

    /// <summary>
    /// Creates a new DbContext and seeds it with test data
    /// </summary>
    public QuaterDbContext CreateSeededDbContext()
    {
        return _factory.CreateSeededContext();
    }

    /// <summary>
    /// Resets the database to a clean state
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await _factory.ResetDatabaseAsync();
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

using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Quater.Backend.Data.Interceptors;
using Testcontainers.PostgreSql;

namespace Quater.Backend.Core.Tests.Helpers;

/// <summary>
/// Factory for creating test database contexts using PostgreSQL TestContainers.
/// This replaces the InMemory database provider to properly handle:
/// - Foreign key constraints (required for AuditTrailInterceptor)
/// - Row version concurrency tokens
/// - Proper transactional behavior
/// </summary>
public class TestDbContextFactory : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private string? _connectionString;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public TestDbContextFactory()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("quater_test")
            .WithUsername("test")
            .WithPassword("test123")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => _connectionString
                                      ?? throw new InvalidOperationException(
                                          "Container not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Initializes the PostgreSQL container.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            Environment.SetEnvironmentVariable("SYSTEM_ADMIN_USER_ID", "eb4b0ebc-7a02-43ca-a858-656bd7e4357f");

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
            _isInitialized = true;

            // Create the database schema and seed the System user
            await InitializeDatabaseAsync();
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Disposes the PostgreSQL container.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Creates a new DbContext connected to the test database WITH interceptors.
    /// Use this for tests that need audit trail and soft delete functionality.
    /// </summary>
    public QuaterDbContext CreateContext()
    {
        EnsureInitialized();
        return CreateContextInternal(withInterceptors: true);
    }

    /// <summary>
    /// Creates a new DbContext connected to the test database WITHOUT interceptors.
    /// Use this for seeding data or when you need to bypass audit/soft-delete behavior.
    /// </summary>
    public QuaterDbContext CreateContextWithoutInterceptors()
    {
        EnsureInitialized();
        return CreateContextInternal(withInterceptors: false);
    }

    /// <summary>
    /// Creates a new DbContext and seeds it with test data.
    /// The System user is always seeded first to satisfy FK constraints.
    /// </summary>
    public QuaterDbContext CreateSeededContext()
    {
        EnsureInitialized();

        // Seed data using context WITHOUT interceptors to avoid chicken-and-egg problem
        using (var seedContext = CreateContextWithoutInterceptors())
        {
            SeedTestData(seedContext);
        }

        // Return a context WITH interceptors for the actual tests
        return CreateContext();
    }

    /// <summary>
    /// Resets the database to a clean state (drops and recreates all tables).
    /// Useful between tests that need full isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        EnsureInitialized();

        using var context = CreateContextWithoutInterceptors();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Re-seed the System user
        await SeedSystemUserAsync(context);
    }

    private QuaterDbContext CreateContextInternal(bool withInterceptors)
    {
        var connectionString = $"{ConnectionString};Include Error Detail=true";

        var optionsBuilder = new DbContextOptionsBuilder<QuaterDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging();

        if (withInterceptors)
        {
            // Create a mock user service that returns SystemUser.GetId()
            var mockUserService = new TestCurrentUserService();

            optionsBuilder.AddInterceptors(
                new SoftDeleteInterceptor(),
                new AuditInterceptor(mockUserService),
                new AuditTrailInterceptor(mockUserService));
        }

        var context = new QuaterDbContext(optionsBuilder.Options);

        // Ensure database is created
        context.Database.EnsureCreated();

        return context;
    }

    private async Task InitializeDatabaseAsync()
    {
        using var context = CreateContextWithoutInterceptors();

        // Ensure database schema is created
        await context.Database.EnsureCreatedAsync();

        // Seed the System user (required for AuditLog FK constraint)
        await SeedSystemUserAsync(context);
    }

    private async Task SeedSystemUserAsync(QuaterDbContext context)
    {
        // For tests, use a hardcoded system admin ID
        // In production, this comes from SYSTEM_ADMIN_USER_ID environment variable
        var systemUserId = new Guid("eb4b0ebc-7a02-43ca-a858-656bd7e4357f");
        var systemUserExists = await context.Users.AnyAsync(u => u.Id == systemUserId);
        if (systemUserExists) return;

        // First, create a System lab (required for User.LabId FK)
        var systemLab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "System Lab",
            Location = "System",
            IsActive = true,
        };
        context.Labs.Add(systemLab);
        await context.SaveChangesAsync();

        // Create the System user (required for AuditLog.UserId FK)
        var systemUser = new User
        {
            Id = systemUserId,
            UserName = "system",
            NormalizedUserName = "SYSTEM",
            Email = "system@quater.app",
            NormalizedEmail = "SYSTEM@QUATER.APP",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = systemLab.Id, Role = UserRole.Admin } ],
            IsActive = true,
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString()
        };
        context.Users.Add(systemUser);
        await context.SaveChangesAsync();
    }

    private void SeedTestData(QuaterDbContext context)
    {
        var testData = MockDataFactory.CreateTestDataSet();

        // 1. Save Labs first (needed for Users and Samples)
        context.Labs.AddRange(testData.Labs);
        context.SaveChanges();

        // 2. Add Parameters (no FK dependencies)
        context.Parameters.AddRange(testData.Parameters);
        context.SaveChanges();

        // 3. Add Samples (depends on Labs)
        context.Samples.AddRange(testData.Samples);
        context.SaveChanges();

        // 4. Add TestResults (depends on Samples and Parameters)
        context.TestResults.AddRange(testData.TestResults);
        context.SaveChanges();
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "TestDbContextFactory is not initialized. " +
                "Ensure your test class implements IAsyncLifetime and calls InitializeAsync(), " +
                "or use the shared fixture pattern with [Collection(\"PostgreSQL\")].");
        }
    }
}

/// <summary>
/// Shared test container fixture for xUnit collection.
/// Use this for tests that can share the same database container.
/// </summary>
public class TestDbContextFactoryFixture : IAsyncLifetime
{
    public TestDbContextFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new TestDbContextFactory();
        await Factory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

/// <summary>
/// xUnit collection definition for sharing the TestDbContextFactory across tests.
/// Tests using this collection will share a single PostgreSQL container.
/// </summary>
[CollectionDefinition("TestDatabase")]
public class TestDatabaseCollection : ICollectionFixture<TestDbContextFactoryFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Mock implementation of ICurrentUserService for testing.
/// Returns SystemUser.GetId() to simulate system operations.
/// </summary>
internal class TestCurrentUserService : ICurrentUserService
{
    public Guid GetCurrentUserId() => SystemUser.GetId();
}

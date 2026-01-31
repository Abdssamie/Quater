using Microsoft.EntityFrameworkCore;
using Quater.Backend.Data;

namespace Quater.Backend.Core.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory test database contexts
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory database context with a unique database name
    /// </summary>
    public static QuaterDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<QuaterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new QuaterDbContext(options);
    }

    /// <summary>
    /// Creates a new in-memory database context with a specific database name
    /// </summary>
    public static QuaterDbContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<QuaterDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        return new QuaterDbContext(options);
    }

    /// <summary>
    /// Creates a new in-memory database context and seeds it with test data
    /// </summary>
    public static QuaterDbContext CreateSeededContext()
    {
        var context = CreateInMemoryContext();
        SeedTestData(context);
        return context;
    }

    /// <summary>
    /// Seeds the context with basic test data
    /// </summary>
    private static void SeedTestData(QuaterDbContext context)
    {
        var testData = MockDataFactory.CreateTestDataSet();
        
        context.Labs.AddRange(testData.Labs);
        context.Parameters.AddRange(testData.Parameters);
        context.Samples.AddRange(testData.Samples);
        context.TestResults.AddRange(testData.TestResults);
        
        context.SaveChanges();
    }
}

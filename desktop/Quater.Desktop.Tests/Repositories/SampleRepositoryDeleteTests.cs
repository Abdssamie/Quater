using Microsoft.EntityFrameworkCore;
using Quater.Desktop.Data;
using Quater.Desktop.Data.Repositories;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Quater.Shared.ValueObjects;

namespace Quater.Desktop.Tests.Repositories;

/// <summary>
/// Integration tests for SampleRepository.DeleteAsync soft-delete behaviour.
/// Uses an in-memory SQLite database to verify that deleted rows are never
/// physically removed — they are only flagged with IsDeleted = true.
/// </summary>
public sealed class SampleRepositoryDeleteTests : IDisposable
{
    private readonly QuaterLocalContext _context;
    private readonly SampleRepository _repository;

    public SampleRepositoryDeleteTests()
    {
        var options = new DbContextOptionsBuilder<QuaterLocalContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new QuaterLocalContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _repository = new SampleRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Seeds a <see cref="Sample"/> directly via raw SQL to avoid EF Core's
    /// ValueGeneratedOnAddOrUpdate behaviour for the RowVersion column.
    /// Returns a Sample instance with the seeded Id so the repository can look it up.
    /// </summary>
    private async Task<Sample> SeedSampleAsync()
    {
        var labId = Guid.NewGuid();
        var sampleId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();

        // EF Core's SQLite provider stores GUIDs as uppercase TEXT.
        // Raw SQL string interpolation must use the same casing; otherwise the
        // case-sensitive TEXT comparison in EF's LINQ queries will not find the row.
        var labIdStr = labId.ToString("D").ToUpperInvariant();
        var sampleIdStr = sampleId.ToString("D").ToUpperInvariant();

        // Insert Lab via raw SQL to bypass EF Core ValueGeneratedOnAddOrUpdate on RowVersion.
        // EF1002 suppressed: these are test-only strings with no user input; SQL injection is not a concern.
#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Labs (Id, Name, IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES ('{labIdStr}', 'Test Lab', 1, '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        // Insert Sample via raw SQL for the same reason
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Samples (
                Id, Type, LocationLatitude, LocationLongitude, LocationDescription,
                CollectionDate, CollectorName, Status, LabId,
                CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES (
                '{sampleIdStr}', 'DrinkingWater', 34.0, -6.8, 'Test Site',
                '{now}', 'Test Collector', 'Pending', '{labIdStr}',
                '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        // Load the sample through EF Core so the repository can track it
        var sample = await _context.Samples
            .IgnoreQueryFilters()
            .FirstAsync(s => s.Id == sampleId);

        return sample;
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenSampleExists_ReturnsTrueAndSetsIsDeletedTrue()
    {
        // Arrange
        var sample = await SeedSampleAsync();

        // Act
        var result = await _repository.DeleteAsync(sample.Id);

        // Assert
        Assert.True(result, "DeleteAsync should return true for an existing sample.");

        // Reload from DB to confirm IsDeleted persisted
        await _context.Entry(sample).ReloadAsync();
        Assert.True(sample.IsDeleted, "IsDeleted should be set to true after deletion.");
    }

    [Fact]
    public async Task DeleteAsync_WhenSampleExists_SetsDeletedAtToUtcNow()
    {
        // Arrange
        var sample = await SeedSampleAsync();
        var before = DateTime.UtcNow;

        // Act
        await _repository.DeleteAsync(sample.Id);
        var after = DateTime.UtcNow;

        // Assert
        await _context.Entry(sample).ReloadAsync();
        Assert.NotNull(sample.DeletedAt);
        Assert.InRange(sample.DeletedAt!.Value, before, after);
    }

    [Fact]
    public async Task DeleteAsync_WhenSampleExists_RowRemainsInDatabase()
    {
        // Arrange
        var sampleId = (await SeedSampleAsync()).Id;

        // Act
        await _repository.DeleteAsync(sampleId);

        // Assert: query without the global filter to confirm row was NOT physically deleted
        var rowInDb = await _context.Samples
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == sampleId);

        Assert.NotNull(rowInDb);
        Assert.True(rowInDb.IsDeleted, "Row in DB should have IsDeleted = true.");
    }

    [Fact]
    public async Task DeleteAsync_WhenSampleExists_SampleIsExcludedFromNormalQueries()
    {
        // Arrange
        var sampleId = (await SeedSampleAsync()).Id;

        // Act
        await _repository.DeleteAsync(sampleId);

        // Assert: the global query filter (or the manual !IsDeleted filter) must hide the row
        var found = await _repository.GetByIdAsync(sampleId);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_WhenSampleDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result, "DeleteAsync should return false when the sample does not exist.");
    }
}

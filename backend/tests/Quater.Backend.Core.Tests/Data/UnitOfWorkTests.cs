using FluentAssertions;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Backend.Data.Repositories;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Tests for UnitOfWork using PostgreSQL TestContainers.
/// </summary>
[Collection("TestDatabase")]
public class UnitOfWorkTests : IAsyncLifetime
{
    private readonly TestDbContextFactoryFixture _fixture;
    private QuaterDbContext _context = null!;
    private UnitOfWork _unitOfWork = null!;

    public UnitOfWorkTests(TestDbContextFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset and seed database before each test
        await _fixture.Factory.ResetDatabaseAsync();

        // Seed test data
        await using (var seedContext = _fixture.Factory.CreateContextWithoutInterceptors())
        {
            SeedTestData(seedContext);
        }

        _context = _fixture.Factory.CreateContext();
        _unitOfWork = new UnitOfWork(_context);
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _context.DisposeAsync();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed by test
        }
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

    [Fact]
    public async Task SaveChangesAsync_CommitsAllChanges()
    {
        // Arrange
        var sample = MockDataFactory.CreateSample(_context.Labs.First().Id);
        _context.Samples.Add(sample);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);

        var savedSample = await _context.Samples.FindAsync(sample.Id);
        savedSample.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var sample = MockDataFactory.CreateSample(_context.Labs.First().Id);
        _context.Samples.Add(sample);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _unitOfWork.SaveChangesAsync(cts.Token));
    }

    [Fact]
    public async Task BeginTransactionAsync_CreatesTransaction()
    {
        // Act
        await _unitOfWork.BeginTransactionAsync();

        // Assert
        _context.Database.CurrentTransaction.Should().NotBeNull();
    }

    [Fact]
    public async Task CommitTransactionAsync_CommitsTransaction()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        var sample = MockDataFactory.CreateSample(_context.Labs.First().Id);
        _context.Samples.Add(sample);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        _context.Database.CurrentTransaction.Should().BeNull();

        var savedSample = await _context.Samples.FindAsync(sample.Id);
        savedSample.Should().NotBeNull();
    }

    [Fact]
    public async Task RollbackTransactionAsync_RollsBackChanges()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        var sample = MockDataFactory.CreateSample(_context.Labs.First().Id);
        var sampleId = sample.Id;
        _context.Samples.Add(sample);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        _context.Database.CurrentTransaction.Should().BeNull();

        // Create new context to verify rollback
        await using var newContext = _fixture.Factory.CreateContext();
        var rolledBackSample = await newContext.Samples.FindAsync(sampleId);
        rolledBackSample.Should().BeNull();
    }

    [Fact]
    public void Dispose_DisposesContext()
    {
        // Act
        _unitOfWork.Dispose();

        // Assert
        // If context is disposed, this should throw
        Assert.Throws<ObjectDisposedException>(() => _context.Samples.ToList());
    }
}

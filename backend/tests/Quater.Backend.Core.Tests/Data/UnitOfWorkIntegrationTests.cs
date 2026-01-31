using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Backend.Data.Repositories;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Integration tests for UnitOfWork using PostgreSQL test container
/// </summary>
[Collection("PostgreSQL")]
public class UnitOfWorkIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainerFixture _fixture;
    private QuaterDbContext _context = null!;
    private UnitOfWork _unitOfWork = null!;

    public UnitOfWorkIntegrationTests(PostgresTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.Container.ResetDatabaseAsync();
        _context = _fixture.Container.CreateSeededDbContext();
        _unitOfWork = new UnitOfWork(_context);
    }

    public async Task DisposeAsync()
    {
        _unitOfWork.Dispose();
        await _context.DisposeAsync();
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
        
        // Verify rollback - sample should not exist
        var rolledBackSample = await _context.Samples.FindAsync(sampleId);
        rolledBackSample.Should().BeNull();
    }

    [Fact]
    public async Task Transaction_WithException_RollsBackAutomatically()
    {
        // Arrange
        var initialCount = await _context.Samples.CountAsync();

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await _unitOfWork.BeginTransactionAsync();
            
            // Create sample with invalid lab ID (should fail foreign key constraint)
            var sample = MockDataFactory.CreateSample(Guid.NewGuid());
            _context.Samples.Add(sample);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        });

        // Verify no changes were committed
        var finalCount = await _context.Samples.CountAsync();
        finalCount.Should().Be(initialCount);
    }
}

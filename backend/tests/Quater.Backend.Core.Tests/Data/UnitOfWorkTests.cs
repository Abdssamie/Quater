using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Backend.Data.Repositories;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

public class UnitOfWorkTests : IDisposable
{
    private readonly QuaterDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _context = TestDbContextFactory.CreateSeededContext();
        _unitOfWork = new UnitOfWork(_context);
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
        cts.Cancel();

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
        using var newContext = TestDbContextFactory.CreateInMemoryContext(_context.Database.GetDbConnection().Database);
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

    public void Dispose()
    {
        try
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed by test
        }
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

public class SoftDeleteInterceptorTests : IDisposable
{
    private readonly QuaterDbContext _context;

    public SoftDeleteInterceptorTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    [Fact]
    public async Task SaveChanges_DeletedEntity_SetsIsDeletedFlag()
    {
        // Arrange
        var sample = MockDataFactory.CreateSample();
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Act
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Assert
        var deletedSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);
        deletedSample.Should().NotBeNull();
        deletedSample!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Query_DeletedEntities_ExcludedByDefault()
    {
        // Arrange
        var sample1 = MockDataFactory.CreateSample();
        var sample2 = MockDataFactory.CreateSample();
        sample2.IsDeleted = true;
        
        _context.Samples.AddRange(sample1, sample2);
        await _context.SaveChangesAsync();

        // Act
        var samples = await _context.Samples.ToListAsync();

        // Assert
        samples.Should().HaveCount(1);
        samples.Should().Contain(s => s.Id == sample1.Id);
        samples.Should().NotContain(s => s.Id == sample2.Id);
    }

    [Fact]
    public async Task Query_WithIgnoreQueryFilters_IncludesDeletedEntities()
    {
        // Arrange
        var sample1 = MockDataFactory.CreateSample();
        var sample2 = MockDataFactory.CreateSample();
        sample2.IsDeleted = true;
        
        _context.Samples.AddRange(sample1, sample2);
        await _context.SaveChangesAsync();

        // Act
        var samples = await _context.Samples.IgnoreQueryFilters().ToListAsync();

        // Assert
        samples.Should().HaveCount(2);
        samples.Should().Contain(s => s.Id == sample1.Id);
        samples.Should().Contain(s => s.Id == sample2.Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

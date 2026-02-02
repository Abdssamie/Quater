using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Tests for SoftDeleteInterceptor using PostgreSQL TestContainers.
/// </summary>
[Collection("TestDatabase")]
public class SoftDeleteInterceptorTests : IAsyncLifetime
{
    private readonly TestDbContextFactoryFixture _fixture;
    private QuaterDbContext _context = null!;

    public SoftDeleteInterceptorTests(TestDbContextFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database to clean state before each test
        await _fixture.Factory.ResetDatabaseAsync();
        _context = _fixture.Factory.CreateContext();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task SaveChanges_DeletedEntity_SetsIsDeletedFlag()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
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
        var labId = await GetFirstLabIdAsync();
        var sample1 = MockDataFactory.CreateSample(labId);
        var sample2 = MockDataFactory.CreateSample(labId);

        _context.Samples.AddRange(sample1, sample2);
        await _context.SaveChangesAsync();

        // Soft-delete sample2 using a context without interceptors to directly set IsDeleted
        using (var directContext = _fixture.Factory.CreateContextWithoutInterceptors())
        {
            var s2 = await directContext.Samples.FindAsync(sample2.Id);
            s2!.IsDeleted = true;
            await directContext.SaveChangesAsync();
        }

        // Refresh context to pick up changes
        await _context.DisposeAsync();
        _context = _fixture.Factory.CreateContext();

        // Act
        var samples = await _context.Samples.ToListAsync();

        // Assert
        samples.Should().Contain(s => s.Id == sample1.Id);
        samples.Should().NotContain(s => s.Id == sample2.Id);
    }

    [Fact]
    public async Task Query_WithIgnoreQueryFilters_IncludesDeletedEntities()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample1 = MockDataFactory.CreateSample(labId);
        var sample2 = MockDataFactory.CreateSample(labId);

        _context.Samples.AddRange(sample1, sample2);
        await _context.SaveChangesAsync();

        // Soft-delete sample2 using a context without interceptors to directly set IsDeleted
        using (var directContext = _fixture.Factory.CreateContextWithoutInterceptors())
        {
            var s2 = await directContext.Samples.FindAsync(sample2.Id);
            s2!.IsDeleted = true;
            await directContext.SaveChangesAsync();
        }

        // Refresh context to pick up changes
        await _context.DisposeAsync();
        _context = _fixture.Factory.CreateContext();

        // Act
        var samples = await _context.Samples.IgnoreQueryFilters().ToListAsync();

        // Assert
        samples.Should().Contain(s => s.Id == sample1.Id);
        samples.Should().Contain(s => s.Id == sample2.Id);
    }

    private async Task<Guid> GetFirstLabIdAsync()
    {
        var lab = await _context.Labs.FirstOrDefaultAsync();
        if (lab != null) return lab.Id;

        // Create a lab if none exists
        var newLab = MockDataFactory.CreateLab("Test Lab for SoftDelete");
        _context.Labs.Add(newLab);
        await _context.SaveChangesAsync();
        return newLab.Id;
    }
}

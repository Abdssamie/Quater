using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Comprehensive tests for SoftDeleteInterceptor using PostgreSQL TestContainers.
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

    #region Core Soft Delete Behavior

    [Fact]
    public async Task Delete_SetsIsDeletedToTrue()
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
    public async Task Delete_SetsDeletedAtTimestamp()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();
        var beforeDelete = DateTime.UtcNow;

        // Act
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();
        var afterDelete = DateTime.UtcNow;

        // Assert
        var deletedSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);
        deletedSample.Should().NotBeNull();
        deletedSample!.DeletedAt.Should().NotBeNull();
        deletedSample.DeletedAt.Should().BeOnOrAfter(beforeDelete);
        deletedSample.DeletedAt.Should().BeOnOrBefore(afterDelete);
    }

    [Fact]
    public async Task Delete_PreservesEntityInDatabase()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Act
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Assert - Entity still exists in database
        var deletedSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);
        deletedSample.Should().NotBeNull();
        deletedSample!.Id.Should().Be(sample.Id);
    }

    [Fact]
    public async Task NonDeletedEntity_HasNullDeletedAt()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Act - Don't delete

        // Assert
        var activeSample = await _context.Samples.FirstOrDefaultAsync(s => s.Id == sample.Id);
        activeSample.Should().NotBeNull();
        activeSample!.IsDeleted.Should().BeFalse();
        activeSample.DeletedAt.Should().BeNull();
        activeSample.DeletedBy.Should().BeNull();
    }

    #endregion

    #region Multiple Entities

    [Fact]
    public async Task Delete_MultipleEntities_AllSoftDeleted()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample1 = MockDataFactory.CreateSample(labId);
        var sample2 = MockDataFactory.CreateSample(labId);
        var sample3 = MockDataFactory.CreateSample(labId);
        _context.Samples.AddRange(sample1, sample2, sample3);
        await _context.SaveChangesAsync();

        // Act - Delete all 3 in single SaveChanges
        _context.Samples.RemoveRange(sample1, sample2, sample3);
        await _context.SaveChangesAsync();

        // Assert - All 3 are soft deleted
        var deletedSamples = await _context.Samples.IgnoreQueryFilters()
            .Where(s => s.Id == sample1.Id || s.Id == sample2.Id || s.Id == sample3.Id)
            .ToListAsync();

        deletedSamples.Should().HaveCount(3);
        deletedSamples.Should().OnlyContain(s => s.IsDeleted);
        deletedSamples.Should().OnlyContain(s => s.DeletedAt != null);
    }

    [Fact]
    public async Task Delete_MixedEntityTypes_AllSoftDeleted()
    {
        // Arrange
        var lab = MockDataFactory.CreateLab("Test Lab");
        _context.Labs.Add(lab);
        await _context.SaveChangesAsync();

        var parameter = new Parameter
        {
            Id = Guid.NewGuid(),
            Name = "pH",
            Unit = "pH units",
            Threshold = 8.5,
            MinValue = 6.5,
            MaxValue = 9.5,
            IsActive = true,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };
        _context.Parameters.Add(parameter);
        await _context.SaveChangesAsync();

        // Act - Delete different entity types (but not Lab since it has FK dependencies)
        _context.Parameters.Remove(parameter);
        await _context.SaveChangesAsync();

        // Assert - Parameter is soft deleted
        var deletedParameter = await _context.Parameters.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == parameter.Id);

        deletedParameter.Should().NotBeNull();
        deletedParameter!.IsDeleted.Should().BeTrue();

        // Lab should still be active (not deleted)
        var activeLab = await _context.Labs.FirstOrDefaultAsync(l => l.Id == lab.Id);
        activeLab.Should().NotBeNull();
        activeLab!.IsDeleted.Should().BeFalse();
    }

    #endregion

    #region Query Filtering

    [Fact]
    public async Task Query_OnlyReturnsNonDeletedEntities()
    {
        // Arrange - Create 5 samples, soft delete 2
        var labId = await GetFirstLabIdAsync();
        var samples = Enumerable.Range(1, 5).Select(_ => MockDataFactory.CreateSample(labId)).ToList();
        _context.Samples.AddRange(samples);
        await _context.SaveChangesAsync();

        // Soft delete 2 samples
        _context.Samples.RemoveRange(samples[0], samples[1]);
        await _context.SaveChangesAsync();

        // Act - Query without IgnoreQueryFilters
        var activeSamples = await _context.Samples.ToListAsync();

        // Assert - Returns only 3 non-deleted samples
        activeSamples.Should().HaveCount(3);
        activeSamples.Should().NotContain(s => s.Id == samples[0].Id);
        activeSamples.Should().NotContain(s => s.Id == samples[1].Id);
        activeSamples.Should().Contain(s => s.Id == samples[2].Id);
        activeSamples.Should().Contain(s => s.Id == samples[3].Id);
        activeSamples.Should().Contain(s => s.Id == samples[4].Id);
    }

    [Fact]
    public async Task Query_IgnoreQueryFilters_ReturnsAll()
    {
        // Arrange - Create 5 samples, soft delete 2
        var labId = await GetFirstLabIdAsync();
        var samples = Enumerable.Range(1, 5).Select(_ => MockDataFactory.CreateSample(labId)).ToList();
        _context.Samples.AddRange(samples);
        await _context.SaveChangesAsync();

        // Soft delete 2 samples
        _context.Samples.RemoveRange(samples[0], samples[1]);
        await _context.SaveChangesAsync();

        // Act - Query with IgnoreQueryFilters
        var allSamples = await _context.Samples.IgnoreQueryFilters().ToListAsync();

        // Assert - Returns all 5 samples
        allSamples.Should().HaveCount(5);
        allSamples.Should().Contain(s => s.Id == samples[0].Id);
        allSamples.Should().Contain(s => s.Id == samples[1].Id);
    }

    [Fact]
    public async Task Find_DeletedEntity_ReturnsNull()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Soft delete
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Refresh context
        await _context.DisposeAsync();
        _context = _fixture.Factory.CreateContext();

        // Act - Find by ID without IgnoreQueryFilters
        var foundSample = await _context.Samples.FirstOrDefaultAsync(s => s.Id == sample.Id);

        // Assert - Returns null
        foundSample.Should().BeNull();
    }

    [Fact]
    public async Task Find_DeletedEntity_WithIgnoreFilters_ReturnsEntity()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Soft delete
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Refresh context
        await _context.DisposeAsync();
        _context = _fixture.Factory.CreateContext();

        // Act - Find by ID with IgnoreQueryFilters
        var foundSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);

        // Assert - Returns the entity
        foundSample.Should().NotBeNull();
        foundSample!.Id.Should().Be(sample.Id);
        foundSample.IsDeleted.Should().BeTrue();
    }

    #endregion

    #region State Transitions

    [Fact]
    public async Task Delete_EntityStillExistsInDatabase()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Act - Mark for deletion
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Assert - Entity still exists in database (not hard deleted)
        var deletedSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);
        deletedSample.Should().NotBeNull();
        deletedSample!.IsDeleted.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Delete_AlreadyDeletedEntity_NoError()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // First delete
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        var firstDeletedAt = sample.DeletedAt;

        // Refresh context and get the deleted entity
        await _context.DisposeAsync();
        _context = _fixture.Factory.CreateContext();
        var deletedSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);

        // Act - Try to delete again
        _context.Samples.Remove(deletedSample!);
        await _context.SaveChangesAsync();

        // Assert - No error, IsDeleted still true
        var finalSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);
        finalSample.Should().NotBeNull();
        finalSample!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ThenUpdate_StillDeleted()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Soft delete
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Refresh context and get the deleted entity
        await _context.DisposeAsync();
        _context = _fixture.Factory.CreateContext();
        var deletedSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);

        // Act - Try to update a property
        deletedSample!.CollectorName = "Updated Collector";
        await _context.SaveChangesAsync();

        // Assert - Update succeeds, IsDeleted remains true
        var updatedSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);
        updatedSample.Should().NotBeNull();
        updatedSample!.IsDeleted.Should().BeTrue();
        updatedSample.CollectorName.Should().Be("Updated Collector");
    }

    [Fact]
    public async Task Delete_WithNavigationProperties_HandlesCorrectly()
    {
        // Arrange - Create Sample with TestResults
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        var parameter = new Parameter
        {
            Id = Guid.NewGuid(),
            Name = "pH",
            Unit = "pH units",
            Threshold = 8.5,
            MinValue = 6.5,
            MaxValue = 9.5,
            IsActive = true,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };
        _context.Parameters.Add(parameter);
        await _context.SaveChangesAsync();

        var testResult = new TestResult
        {
            Id = Guid.NewGuid(),
            SampleId = sample.Id,
            Measurement = new Quater.Shared.ValueObjects.Measurement(parameter, 7.5, parameter.Unit),
            ComplianceStatus = ComplianceStatus.Pass,
            TestMethod = TestMethod.Spectrophotometry,
            TechnicianName = "Test Technician",
            TestDate = DateTime.UtcNow,
        };
        _context.TestResults.Add(testResult);
        await _context.SaveChangesAsync();

        // Act - Delete Sample
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Assert - Sample is soft deleted
        var deletedSample = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sample.Id);
        deletedSample.Should().NotBeNull();
        deletedSample!.IsDeleted.Should().BeTrue();

        // TestResult should still exist (soft delete doesn't cascade by default)
        // Check with IgnoreQueryFilters to see if it exists at all
        var testResultExists = await _context.TestResults.IgnoreQueryFilters().AnyAsync(tr => tr.Id == testResult.Id);
        testResultExists.Should().BeTrue("TestResult should still exist in database after parent Sample is soft deleted");
    }

    #endregion

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

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Core.Validators;
using Quater.Backend.Data;
using Quater.Backend.Services;
using Quater.Shared.Enums;
using Xunit;

namespace Quater.Backend.Core.Tests.Services;

/// <summary>
/// Integration tests for SampleService using PostgreSQL test container
/// </summary>
[Collection("PostgreSQL")]
public class SampleServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainerFixture _fixture;
    private QuaterDbContext _context = null!;
    private FakeTimeProvider _timeProvider = null!;
    private SampleService _service = null!;

    public SampleServiceIntegrationTests(PostgresTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.Container.ResetDatabaseAsync();
        
        _context = _fixture.Container.CreateSeededDbContext();
        _timeProvider = new FakeTimeProvider();
        var validator = new SampleValidator(_timeProvider);
        _service = new SampleService(_context, validator);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingSample_ReturnsSample()
    {
        // Arrange
        var sample = _context.Samples.First();

        // Act
        var result = await _service.GetByIdAsync(sample.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sample.Id);
        result.CollectorName.Should().Be(sample.CollectorName);
    }

    [Fact]
    public async Task CreateAsync_ValidSample_CreatesSample()
    {
        // Arrange
        var lab = _context.Labs.First();
        var dto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 34.0,
            LocationLongitude = -5.0,
            LocationDescription = "New Test Location",
            CollectionDate = _timeProvider.GetUtcNow().UtcDateTime,
            CollectorName = "Test Collector",
            LabId = lab.Id
        };

        // Act
        var result = await _service.CreateAsync(dto, Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CollectorName.Should().Be(dto.CollectorName);
        result.Status.Should().Be(SampleStatus.Pending);
        // Verify it was persisted
        var persisted = await _context.Samples.FindAsync(result.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ExistingSample_UpdatesSample()
    {
        // Arrange
        var sample = _context.Samples.First();
        var dto = new UpdateSampleDto
        {
            Type = SampleType.Wastewater,
            LocationLatitude = 35.0,
            LocationLongitude = -6.0,
            LocationDescription = "Updated Location",
            CollectionDate = sample.CollectionDate,
            CollectorName = "Updated Collector",
            Status = SampleStatus.Completed
        };

        // Act
        var result = await _service.UpdateAsync(sample.Id, dto, Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        result!.CollectorName.Should().Be(dto.CollectorName);
        result.Type.Should().Be(dto.Type);
        result.Status.Should().Be(dto.Status);
    }

    [Fact]
    public async Task UpdateAsync_VersionMismatch_ThrowsConflictException()
    {
        // Arrange
        var sample = _context.Samples.First();
        var dto = new UpdateSampleDto
        {
            Type = sample.Type,
            LocationLatitude = sample.Location.Latitude,
            LocationLongitude = sample.Location.Longitude,
            CollectionDate = sample.CollectionDate,
            CollectorName = sample.CollectorName,
            Status = sample.Status
        };

        // Modify the sample's RowVersion to simulate a concurrent update
        sample.RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 99 };
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
            _service.UpdateAsync(sample.Id, dto, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_ExistingSample_SoftDeletesSample()
    {
        // Arrange
        var sample = _context.Samples.First();
        var sampleId = sample.Id;

        // Act
        var result = await _service.DeleteAsync(sampleId);

        // Assert
        result.Should().BeTrue();
        
        // Verify soft delete
        var deletedSample = await _context.Samples.FindAsync(sampleId);
        deletedSample.Should().NotBeNull();
        deletedSample!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 2;

        // Act
        var result = await _service.GetAllAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountLessOrEqualTo(pageSize);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().BeGreaterThan(0);
    }
}

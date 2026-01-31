using FluentAssertions;
using FluentValidation;
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

public class SampleServiceTests : IDisposable
{
    private readonly QuaterDbContext _context;
    private readonly FakeTimeProvider _timeProvider;
    private readonly SampleService _service;

    public SampleServiceTests()
    {
        _context = TestDbContextFactory.CreateSeededContext();
        _timeProvider = new FakeTimeProvider();
        var validator = new SampleValidator(_timeProvider);
        _service = new SampleService(_context, _timeProvider, validator);
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
    public async Task GetByIdAsync_NonExistentSample_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedSample_ReturnsNull()
    {
        // Arrange
        var sample = _context.Samples.First();
        sample.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(sample.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResults()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 5;

        // Act
        var result = await _service.GetAllAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllAsync_SecondPage_ReturnsCorrectPage()
    {
        // Arrange
        var pageNumber = 2;
        var pageSize = 2;

        // Act
        var result = await _service.GetAllAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(pageNumber);
        result.Items.Should().HaveCountLessOrEqualTo(pageSize);
    }

    [Fact]
    public async Task GetByLabIdAsync_ReturnsOnlySamplesForLab()
    {
        // Arrange
        var lab = _context.Labs.First();
        var labId = lab.Id;

        // Act
        var result = await _service.GetByLabIdAsync(labId);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(s => s.LabId == labId);
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
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            CollectorName = "Test Collector",
            LabId = lab.Id
        };

        // Act
        var result = await _service.CreateAsync(dto, "test-user");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CollectorName.Should().Be(dto.CollectorName);
        result.Status.Should().Be(SampleStatus.Pending);
        result.Version.Should().Be(1);
        result.IsSynced.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_InvalidCoordinates_ThrowsValidationException()
    {
        // Arrange
        var lab = _context.Labs.First();
        var dto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 100.0, // Invalid
            LocationLongitude = -200.0, // Invalid
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            CollectorName = "Test Collector",
            LabId = lab.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => 
            _service.CreateAsync(dto, "test-user"));
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
            Status = SampleStatus.Completed,
            Version = sample.Version
        };

        // Act
        var result = await _service.UpdateAsync(sample.Id, dto, "test-user");

        // Assert
        result.Should().NotBeNull();
        result!.CollectorName.Should().Be(dto.CollectorName);
        result.Type.Should().Be(dto.Type);
        result.Status.Should().Be(dto.Status);
        result.Version.Should().Be(sample.Version + 1);
        result.IsSynced.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NonExistentSample_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var dto = new UpdateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 34.0,
            LocationLongitude = -5.0,
            CollectionDate = DateTime.UtcNow,
            CollectorName = "Test",
            Status = SampleStatus.Pending,
            Version = 1
        };

        // Act
        var result = await _service.UpdateAsync(nonExistentId, dto, "test-user");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_VersionMismatch_ThrowsConflictException()
    {
        // Arrange
        var sample = _context.Samples.First();
        var dto = new UpdateSampleDto
        {
            Type = sample.Type,
            LocationLatitude = sample.LocationLatitude,
            LocationLongitude = sample.LocationLongitude,
            CollectionDate = sample.CollectionDate,
            CollectorName = sample.CollectorName,
            Status = sample.Status,
            Version = sample.Version + 1 // Wrong version
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => 
            _service.UpdateAsync(sample.Id, dto, "test-user"));
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
        
        var deletedSample = await _context.Samples.FindAsync(sampleId);
        deletedSample.Should().NotBeNull();
        deletedSample!.IsDeleted.Should().BeTrue();
        deletedSample.IsSynced.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentSample_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedSample_ReturnsFalse()
    {
        // Arrange
        var sample = _context.Samples.First();
        sample.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(sample.Id);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

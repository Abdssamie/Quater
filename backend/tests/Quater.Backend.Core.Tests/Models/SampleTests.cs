using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Quater.Backend.Core.Validators;
using Xunit;

namespace Quater.Backend.Core.Tests.Models;

public class SampleTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly SampleValidator _validator;

    public SampleTests()
    {
        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _validator = new SampleValidator(_timeProvider);
    }

    [Fact]
    public void CreateSample_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var sample = new Sample
        {
            Id = Guid.NewGuid(),
            CollectorName = "John Doe",
            LocationLatitude = 34.0,
            LocationLongitude = -5.0,
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            LabId = Guid.NewGuid(),
            Status = SampleStatus.Pending,
            Type = SampleType.DrinkingWater,
            CreatedBy = "System",
            CreatedDate = _timeProvider.GetUtcNow().DateTime,
            LastModified = _timeProvider.GetUtcNow().DateTime,
            LastModifiedBy = "System",
            Version = 1
        };

        // Act
        var result = _validator.Validate(sample);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateSample_WithInvalidCoordinates_ShouldFailValidation()
    {
        // Arrange
        var sample = new Sample
        {
            LocationLatitude = 100.0, // Invalid: > 90
            LocationLongitude = -200.0, // Invalid: < -180
            CollectorName = "Jane Doe",
            LabId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(sample);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Sample.LocationLatitude));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Sample.LocationLongitude));
    }

    [Fact]
    public void CreateSample_WithFutureDate_ShouldFailValidation()
    {
        // Arrange
        var futureDate = _timeProvider.GetUtcNow().AddDays(1).DateTime;
        var sample = new Sample
        {
            CollectorName = "Time Traveler",
            CollectionDate = futureDate, // Future date
            LabId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(sample);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Sample.CollectionDate));
    }
}

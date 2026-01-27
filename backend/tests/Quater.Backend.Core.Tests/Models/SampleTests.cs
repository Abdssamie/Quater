using FluentAssertions;
using Quater.Backend.Core.Enums;
using Quater.Backend.Core.Models;
using Quater.Backend.Core.Validators;
using Xunit;

namespace Quater.Backend.Core.Tests.Models;

public class SampleTests
{
    private readonly SampleValidator _validator = new();

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
            CollectionDate = DateTime.UtcNow,
            LabId = Guid.NewGuid(),
            Status = SampleStatus.Pending,
            Type = SampleType.DrinkingWater,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
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
        var sample = new Sample
        {
            CollectorName = "Time Traveler",
            CollectionDate = DateTime.UtcNow.AddDays(1), // Future date
            LabId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(sample);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Sample.CollectionDate));
    }
}

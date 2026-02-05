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
            Location = new Quater.Shared.ValueObjects.Location(34.0, -5.0),
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            LabId = Guid.NewGuid(),
            Status = SampleStatus.Pending,
            Type = SampleType.DrinkingWater,
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
        // Note: Location ValueObject validates coordinates at construction, so invalid coordinates will throw
        // This test validates that the validator catches missing location
        var sample = new Sample
        {
            CollectorName = "Jane Doe",
            LabId = Guid.NewGuid()
            // Location is intentionally missing to test validation
        };

        // Act
        var result = _validator.Validate(sample);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Sample.Location));
    }

    [Fact]
    public void CreateSample_WithFutureDate_ShouldFailValidation()
    {
        // Arrange
        var futureDate = _timeProvider.GetUtcNow().AddDays(1).DateTime;
        var sample = new Sample
        {
            CollectorName = "Time Traveler",
            Location = new Quater.Shared.ValueObjects.Location(34.0, -5.0),
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

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
            CreatedBy = "System",
            CreatedAt = _timeProvider.GetUtcNow().DateTime,
            UpdatedAt = _timeProvider.GetUtcNow().DateTime,
            UpdatedBy = "System"
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
        // This test now validates that the validator catches the issue
        // TODO: Fix after data models refactoring - Sample requires Location ValueObject
        // Need to add: Location = new Quater.Shared.ValueObjects.Location(lat, lng)
        var sample = new Sample
        {
            CollectorName = "Jane Doe",
            LabId = Guid.NewGuid()
            // Location is required, so missing it should fail validation
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
        // TODO: Fix after data models refactoring - Sample requires Location ValueObject
        // Need to add: Location = new Quater.Shared.ValueObjects.Location(34.0, -5.0)
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

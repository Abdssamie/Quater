using FluentAssertions;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Core.Validators;
using Quater.Shared.Enums;
using Xunit;

namespace Quater.Backend.Core.Tests.Validators;

public class CreateSampleDtoValidatorTests
{
    private readonly CreateSampleDtoValidator _validator;
    private readonly FakeTimeProvider _timeProvider;

    public CreateSampleDtoValidatorTests()
    {
        _timeProvider = new FakeTimeProvider();
        _validator = new CreateSampleDtoValidator(_timeProvider);
    }

    [Fact]
    public void Validate_ValidDto_PassesValidation()
    {
        // Arrange
        var dto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 34.0,
            LocationLongitude = -5.0,
            LocationDescription = "Test Location",
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            CollectorName = "John Doe",
            LabId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCollectorName_FailsValidation()
    {
        // Arrange
        var dto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 34.0,
            LocationLongitude = -5.0,
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            CollectorName = "", // Empty
            LabId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSampleDto.CollectorName));
    }

    [Fact]
    public void Validate_InvalidLatitude_FailsValidation()
    {
        // Arrange
        var dto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 100.0, // Invalid: > 90
            LocationLongitude = -5.0,
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            CollectorName = "John Doe",
            LabId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSampleDto.LocationLatitude));
    }

    [Fact]
    public void Validate_InvalidLongitude_FailsValidation()
    {
        // Arrange
        var dto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 34.0,
            LocationLongitude = -200.0, // Invalid: < -180
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            CollectorName = "John Doe",
            LabId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSampleDto.LocationLongitude));
    }

    [Fact]
    public void Validate_FutureCollectionDate_FailsValidation()
    {
        // Arrange
        var dto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 34.0,
            LocationLongitude = -5.0,
            CollectionDate = _timeProvider.GetUtcNow().AddDays(1).DateTime, // Future date
            CollectorName = "John Doe",
            LabId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSampleDto.CollectionDate));
    }

    [Fact]
    public void Validate_EmptyLabId_FailsValidation()
    {
        // Arrange
        var dto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 34.0,
            LocationLongitude = -5.0,
            CollectionDate = _timeProvider.GetUtcNow().DateTime,
            CollectorName = "John Doe",
            LabId = Guid.Empty // Empty GUID
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSampleDto.LabId));
    }
}

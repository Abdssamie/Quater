using FluentAssertions;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Validators;
using Xunit;

namespace Quater.Backend.Core.Tests.Validators;

public class CreateParameterDtoValidatorTests
{
    private readonly CreateParameterDtoValidator _validator;

    public CreateParameterDtoValidatorTests()
    {
        _validator = new CreateParameterDtoValidator();
    }

    [Fact]
    public void Validate_ValidDto_PassesValidation()
    {
        // Arrange
        var dto = new CreateParameterDto
        {
            Name = "pH",
            Unit = "pH units",
            WhoThreshold = 8.5,
            MinValue = 6.5,
            MaxValue = 9.5
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_FailsValidation()
    {
        // Arrange
        var dto = new CreateParameterDto
        {
            Name = "", // Empty
            Unit = "pH units",
            WhoThreshold = 8.5
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParameterDto.Name));
    }

    [Fact]
    public void Validate_EmptyUnit_FailsValidation()
    {
        // Arrange
        var dto = new CreateParameterDto
        {
            Name = "pH",
            Unit = "", // Empty
            WhoThreshold = 8.5
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParameterDto.Unit));
    }

    [Fact]
    public void Validate_NegativeWhoThreshold_FailsValidation()
    {
        // Arrange
        var dto = new CreateParameterDto
        {
            Name = "pH",
            Unit = "pH units",
            WhoThreshold = -1.0 // Negative
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParameterDto.WhoThreshold));
    }

    [Fact]
    public void Validate_MinValueGreaterThanMaxValue_FailsValidation()
    {
        // Arrange
        var dto = new CreateParameterDto
        {
            Name = "pH",
            Unit = "pH units",
            MinValue = 10.0,
            MaxValue = 5.0 // Max < Min
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParameterDto.MaxValue));
    }

    [Fact]
    public void Validate_NullThresholds_PassesValidation()
    {
        // Arrange
        var dto = new CreateParameterDto
        {
            Name = "pH",
            Unit = "pH units",
            WhoThreshold = null,
            MinValue = null,
            MaxValue = null
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

using FluentAssertions;
using Quater.Shared.Models;
using Quater.Shared.ValueObjects;

namespace Quater.Backend.Core.Tests.ValueObjects;

/// <summary>
/// Unit tests for Measurement value object validation.
/// Verifies parameter/unit validation per SC-012.
/// </summary>
public sealed class MeasurementTests
{
    private static Parameter CreateTestParameter(string name = "pH", string unit = "pH", double? minValue = 0, double? maxValue = 14)
    {
        return new Parameter
        {
            Id = Guid.NewGuid(),
            Name = name,
            Unit = unit,
            MinValue = minValue,
            MaxValue = maxValue,
            IsActive = true,
            RowVersion = new byte[8]
        };
    }

    [Fact]
    public void Constructor_WithValidParameterAndValue_ShouldCreateMeasurement()
    {
        // Arrange
        var parameter = CreateTestParameter("pH", "pH", 0, 14);
        const double value = 7.0;
        const string unit = "pH";

        // Act
        var measurement = new Measurement(parameter, value, unit);

        // Assert
        measurement.Should().NotBeNull();
        measurement.ParameterId.Should().Be(parameter.Id);
        measurement.Value.Should().Be(value);
        measurement.Unit.Should().Be(unit);
    }

    [Fact]
    public void Constructor_WithMatchingUnitCaseInsensitive_ShouldCreateMeasurement()
    {
        // Arrange
        var parameter = CreateTestParameter("Temperature", "°C", -10, 50);
        const double value = 25.0;
        const string unit = "°c"; // Different case

        // Act
        var measurement = new Measurement(parameter, value, unit);

        // Assert
        measurement.Should().NotBeNull();
        measurement.Unit.Should().Be(unit);
    }

    [Fact]
    public void Constructor_WithNullParameter_ShouldThrowArgumentNullException()
    {
        // Arrange
        Parameter? parameter = null;
        const double value = 7.0;
        const string unit = "pH";

        // Act
        var act = () => new Measurement(parameter!, value, unit);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("parameter");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhiteSpaceUnit_ShouldThrowArgumentException(string? unit)
    {
        // Arrange
        var parameter = CreateTestParameter();
        const double value = 7.0;

        // Act
        var act = () => new Measurement(parameter, value, unit!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("unit");
    }

    [Fact]
    public void Constructor_WithMismatchedUnit_ShouldThrowArgumentException()
    {
        // Arrange
        var parameter = CreateTestParameter("pH", "pH", 0, 14);
        const double value = 7.0;
        const string wrongUnit = "mg/L"; // Wrong unit

        // Act
        var act = () => new Measurement(parameter, value, wrongUnit);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("unit")
            .WithMessage("*does not match parameter*");
    }

    [Fact]
    public void Constructor_WithValueBelowMinimum_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var parameter = CreateTestParameter("pH", "pH", 0, 14);
        const double value = -1.0; // Below minimum
        const string unit = "pH";

        // Act
        var act = () => new Measurement(parameter, value, unit);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value")
     .WithMessage("*below minimum*");
    }

    [Fact]
    public void Constructor_WithValueAboveMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var parameter = CreateTestParameter("pH", "pH", 0, 14);
        const double value = 15.0; // Above maximum
        const string unit = "pH";

        // Act
        var act = () => new Measurement(parameter, value, unit);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("*exceeds maximum*");
    }

    [Theory]
    [InlineData(0)]    // Minimum value
    [InlineData(14)]   // Maximum value
    [InlineData(7)]    // Middle value
    public void Constructor_WithValueAtBoundaries_ShouldCreateMeasurement(double value)
    {
        // Arrange
        var parameter = CreateTestParameter("pH", "pH", 0, 14);
        const string unit = "pH";

        // Act
        var measurement = new Measurement(parameter, value, unit);

        // Assert
        measurement.Should().NotBeNull();
        measurement.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_WithNoMinMaxConstraints_ShouldAcceptAnyValue()
    {
        // Arrange
        var parameter = CreateTestParameter("Turbidity", "NTU", null, null);
        const double value = 999999.0;
        const string unit = "NTU";

        // Act
        var measurement = new Measurement(parameter, value, unit);

        // Assert
        measurement.Should().NotBeNull();
        measurement.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_DeserializationOverload_ShouldCreateMeasurementWithoutValidation()
    {
        // Arrange
        var parameterId = Guid.NewGuid();
        const double value = 7.0;
        const string unit = "pH";

        // Act
        var measurement = new Measurement(parameterId, value, unit);

        // Assert
        measurement.Should().NotBeNull();
        measurement.ParameterId.Should().Be(parameterId);
        measurement.Value.Should().Be(value);
        measurement.Unit.Should().Be(unit);
    }

    [Fact]
    public void Constructor_DeserializationOverload_WithNullUnit_ShouldThrowArgumentNullException()
    {
        // Arrange
        var parameterId = Guid.NewGuid();
        const double value = 7.0;
        string? unit = null;

        // Act
        var act = () => new Measurement(parameterId, value, unit!);
        
        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unit");
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var parameterId = Guid.NewGuid();
        var measurement1 = new Measurement(parameterId, 7.0, "pH");
        var measurement2 = new Measurement(parameterId, 7.0, "pH");

        // Act & Assert
        measurement1.Should().Be(measurement2);
        (measurement1 == measurement2).Should().BeTrue();
        measurement1.GetHashCode().Should().Be(measurement2.GetHashCode());
    }
    
    [Fact]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var parameterId = Guid.NewGuid();
        var measurement1 = new Measurement(parameterId, 7.0, "pH");
        var measurement2 = new Measurement(parameterId, 8.0, "pH");

        // Act & Assert
        measurement1.Should().NotBe(measurement2);
        (measurement1 == measurement2).Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_WithDifferentParameterId_ShouldNotBeEqual()
    {
        // Arrange
        var measurement1 = new Measurement(Guid.NewGuid(), 7.0, "pH");
        var measurement2 = new Measurement(Guid.NewGuid(), 7.0, "pH");

        // Act & Assert
        measurement1.Should().NotBe(measurement2);
    }
}

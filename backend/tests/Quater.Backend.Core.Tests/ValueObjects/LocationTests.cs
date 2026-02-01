using FluentAssertions;
using Quater.Shared.ValueObjects;

namespace Quater.Backend.Core.Tests.ValueObjects;

/// <summary>
/// Unit tests for Location value object validation.
/// Verifies GPS coordinate validation per SC-010.
/// </summary>
public sealed class LocationTests
{
    [Fact]
    public void Constructor_WithValidCoordinates_ShouldCreateLocation()
    {
        // Arrange
        const double latitude = 45.5;
        const double longitude = -73.6;
        const string description = "Municipal Well #3";
        const string hierarchy = "Region/District/Site";

        // Act
        var location = new Location(latitude, longitude, description, hierarchy);

        // Assert
        location.Should().NotBeNull();
        location.Latitude.Should().Be(latitude);
        location.Longitude.Should().Be(longitude);
        location.Description.Should().Be(description);
        location.Hierarchy.Should().Be(hierarchy);
    }

    [Fact]
    public void Constructor_WithValidCoordinatesOnly_ShouldCreateLocation()
    {
        // Arrange
        const double latitude = 0;
        const double longitude = 0;

        // Act
        var location = new Location(latitude, longitude);

        // Assert
        location.Should().NotBeNull();
        location.Latitude.Should().Be(latitude);
        location.Longitude.Should().Be(longitude);
        location.Description.Should().BeNull();
        location.Hierarchy.Should().BeNull();
    }

    [Theory]
    [InlineData(-90, 0)]    // Minimum valid latitude
    [InlineData(90, 0)]     // Maximum valid latitude
    [InlineData(0, -180)]   // Minimum valid longitude
    [InlineData(0, 180)]    // Maximum valid longitude
    [InlineData(-90, -180)] // Both minimum
    [InlineData(90, 180)]   // Both maximum
    public void Constructor_WithBoundaryCoordinates_ShouldCreateLocation(double latitude, double longitude)
    {
        // Act
        var location = new Location(latitude, longitude);

        // Assert
        location.Should().NotBeNull();
        location.Latitude.Should().Be(latitude);
        location.Longitude.Should().Be(longitude);
    }

    [Theory]
    [InlineData(-90.1, 0)]   // Below minimum latitude
    [InlineData(-91, 0)]     // Below minimum latitude
    [InlineData(90.1, 0)]    // Above maximum latitude
    [InlineData(91, 0)]      // Above maximum latitude
    [InlineData(-100, 0)]    // Far below minimum latitude
    [InlineData(100, 0)]     // Far above maximum latitude
    public void Constructor_WithInvalidLatitude_ShouldThrowArgumentOutOfRangeException(double latitude, double longitude)
    {
        // Act
        var act = () => new Location(latitude, longitude);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("latitude")
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Theory]
    [InlineData(0, -180.1)]  // Below minimum longitude
    [InlineData(0, -181)]    // Below minimum longitude
    [InlineData(0, 180.1)]   // Above maximum longitude
    [InlineData(0, 181)]     // Above maximum longitude
    [InlineData(0, -200)]    // Far below minimum longitude
    [InlineData(0, 200)]     // Far above maximum longitude
    public void Constructor_WithInvalidLongitude_ShouldThrowArgumentOutOfRangeException(double latitude, double longitude)
    {
        // Act
        var act = () => new Location(latitude, longitude);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("longitude")
            .WithMessage("*Longitude must be between -180 and 180*");
    }

    [Fact]
    public void Constructor_WithBothInvalidCoordinates_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const double invalidLatitude = 100;
        const double invalidLongitude = 200;

        // Act
        var act = () => new Location(invalidLatitude, invalidLongitude);

        // Assert - latitude is checked first
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("latitude");
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var location1 = new Location(45.5, -73.6, "Well #1", "Region/District");
        var location2 = new Location(45.5, -73.6, "Well #1", "Region/District");

        // Act & Assert
        location1.Should().Be(location2);
        (location1 == location2).Should().BeTrue();
        location1.GetHashCode().Should().Be(location2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var location1 = new Location(45.5, -73.6, "Well #1", "Region/District");
        var location2 = new Location(45.6, -73.6, "Well #1", "Region/District");

        // Act & Assert
        location1.Should().NotBe(location2);
        (location1 == location2).Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_WithNullDescription_ShouldBeEqual()
    {
        // Arrange
        var location1 = new Location(45.5, -73.6);
        var location2 = new Location(45.5, -73.6, null, null);

        // Act & Assert
        location1.Should().Be(location2);
    }
}

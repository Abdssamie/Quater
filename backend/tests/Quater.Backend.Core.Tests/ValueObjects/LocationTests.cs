using FluentAssertions;
using Quater.Shared.ValueObjects;

namespace Quater.Backend.Core.Tests.ValueObjects;

public class LocationTests
{
    [Fact]
    public void Constructor_Longitude180_ShouldNotThrow()
    {
        // Arrange & Act
        var act = () => new Location(0, 180);
        
        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_LongitudeGreaterThan180_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new Location(0, 180.1);
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*longitude*");
    }

    [Fact]
    public void Constructor_LongitudeMinus180_ShouldNotThrow()
    {
        // Arrange & Act
        var act = () => new Location(0, -180);
        
        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_LongitudeLessThanMinus180_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new Location(0, -180.1);
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*longitude*");
    }

    [Fact]
    public void Constructor_ValidCoordinates_ShouldSetProperties()
    {
        // Arrange & Act
        var location = new Location(45.5, -122.6, "Portland", "USA/Oregon/Portland");
        
        // Assert
        location.Latitude.Should().Be(45.5);
        location.Longitude.Should().Be(-122.6);
        location.Description.Should().Be("Portland");
        location.Hierarchy.Should().Be("USA/Oregon/Portland");
    }

    [Fact]
    public void Constructor_LatitudeGreaterThan90_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new Location(90.1, 0);
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*latitude*");
    }

    [Fact]
    public void Constructor_LatitudeLessThanMinus90_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new Location(-90.1, 0);
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*latitude*");
    }
}

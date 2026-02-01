namespace Quater.Shared.ValueObjects;

/// <summary>
/// Represents a geographic location with GPS coordinates and descriptive information.
/// Validates coordinate ranges at construction to prevent invalid location data.
/// </summary>
public sealed record Location
{
    /// <summary>
    /// Latitude coordinate (-90 to 90 degrees)
    /// </summary>
    public double Latitude { get; init; }
    
    /// <summary>
    /// Longitude coordinate (-180 to 180 degrees)
    /// </summary>
    public double Longitude { get; init; }
    
    /// <summary>
    /// Human-readable location description (e.g., "Municipal Well #3")
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Hierarchical location path for reporting (e.g., "Region/District/Site")
    /// </summary>
    public string? Hierarchy { get; init; }
    
    /// <summary>
    /// Creates a new Location with validated coordinates.
    /// </summary>
    /// <param name="latitude">Latitude coordinate (-90 to 90 degrees)</param>
    /// <param name="longitude">Longitude coordinate (-180 to 180 degrees)</param>
    /// <param name="description">Optional human-readable location description</param>
    /// <param name="hierarchy">Optional hierarchical location path</param>
    /// <exception cref="ArgumentOutOfRangeException">If coordinates are invalid</exception>
    public Location(double latitude, double longitude, string? description = null, string? hierarchy = null)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90");
        
        if (longitude is < -180 or 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180");
        
        Latitude = latitude;
        Longitude = longitude;
        Description = description;
        Hierarchy = hierarchy;
    }
}

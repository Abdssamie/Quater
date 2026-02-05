namespace Quater.Backend.Api.Attributes;

/// <summary>
/// Specifies how to track rate limits for an endpoint.
/// </summary>
public enum RateLimitTrackBy
{
    /// <summary>
    /// Track rate limit by IP address (default for anonymous endpoints).
    /// </summary>
    IpAddress,

    /// <summary>
    /// Track rate limit by authenticated user ID.
    /// </summary>
    UserId,

    /// <summary>
    /// Track rate limit by email address from request body.
    /// Requires reading and parsing the request body.
    /// </summary>
    Email
}

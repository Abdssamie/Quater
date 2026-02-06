namespace Quater.Backend.Api.Attributes;

/// <summary>
/// Applies per-endpoint rate limiting with configurable tracking strategy.
/// When applied, overrides global rate limiting for the specific endpoint.
/// </summary>
/// <example>
/// [EndpointRateLimit(Requests = 10, WindowMinutes = 60, TrackBy = RateLimitTrackBy.IpAddress)]
/// public async Task&lt;IActionResult&gt; Register([FromBody] RegisterRequest request) { ... }
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EndpointRateLimitAttribute : Attribute
{
    /// <summary>
    /// Maximum number of requests allowed within the time window.
    /// </summary>
    public int Requests { get; }

    /// <summary>
    /// Time window in minutes for the rate limit.
    /// </summary>
    public int WindowMinutes { get; }

    /// <summary>
    /// Strategy for tracking rate limits (IP address, User ID, or Email).
    /// </summary>
    public RateLimitTrackBy TrackBy { get; }

    /// <summary>
    /// Creates a new endpoint rate limit configuration.
    /// </summary>
    /// <param name="requests">Maximum number of requests allowed</param>
    /// <param name="windowMinutes">Time window in minutes</param>
    /// <param name="trackBy">Tracking strategy</param>
    public EndpointRateLimitAttribute(int requests, int windowMinutes, RateLimitTrackBy trackBy)
    {
        Requests = requests;
        WindowMinutes = windowMinutes;
        TrackBy = trackBy;
    }
}

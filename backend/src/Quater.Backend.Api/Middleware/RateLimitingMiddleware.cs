using System.Text.Json;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Attributes;
using StackExchange.Redis;

namespace Quater.Backend.Api.Middleware;

/// <summary>
/// Redis-backed rate limiting middleware with authentication-aware limits and per-endpoint rate limiting.
/// Uses distributed Redis counters to support horizontal scaling.
/// Implements atomic increment+expire using Lua script to prevent race conditions.
/// 
/// Global rate limits (production defaults):
/// - Authenticated users: 60 requests per minute (configurable)
/// - Anonymous users: 10 requests per minute (configurable)
/// 
/// Per-endpoint rate limits:
/// - Configured via [EndpointRateLimit] attribute
/// - Supports tracking by IP address, User ID, or Email
/// 
/// Tracks by:
/// - User ID for authenticated requests
/// - IP address for anonymous requests
/// - Email for endpoints with Email tracking (reads from request body)
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly int _authenticatedLimit;
    private readonly int _anonymousLimit;
    private readonly int _windowSeconds;
    private const string RateLimitKeyPrefix = "ratelimit:";
    private const string EndpointRateLimitKeyPrefix = "endpoint-ratelimit:";

    /// <summary>
    /// Lua script to atomically increment counter and set TTL if it's a new key.
    /// Returns [current_count, ttl_seconds] in a single Redis operation.
    /// This prevents the race condition where a key could exist without expiration.
    /// </summary>
    private static readonly LuaScript RateLimitScript = LuaScript.Prepare(@"
        local current = redis.call('INCR', @key)
        if current == 1 then
            redis.call('EXPIRE', @key, @expiry)
        end
        local ttl = redis.call('TTL', @key)
        return {current, ttl}
    ");

    public RateLimitingMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _redis = redis;
        _logger = logger;

        // Load configuration from appsettings.json or environment variables
        _authenticatedLimit = configuration.GetValue("RateLimiting:AuthenticatedLimit", 100);
        _anonymousLimit = configuration.GetValue("RateLimiting:AnonymousLimit", 20);
        _windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 60);

        _logger.LogInformation(
            "Rate limiting configured - Authenticated: {AuthLimit} req/{AuthWindow}s," +
            " Anonymous: {AnonLimit} req/{AnonWindow}s",
            _authenticatedLimit, _windowSeconds, _anonymousLimit, _windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for endpoint-specific rate limit attribute
        var endpoint = context.GetEndpoint();
        var endpointRateLimit = endpoint?.Metadata.GetMetadata<EndpointRateLimitAttribute>();

        if (endpointRateLimit != null)
        {
            // Apply endpoint-specific rate limiting
            await ApplyEndpointRateLimitAsync(context, endpointRateLimit);
        }
        else
        {
            // Apply global rate limiting
            await ApplyGlobalRateLimitAsync(context);
        }
    }

    /// <summary>
    /// Applies global rate limiting based on authentication status.
    /// </summary>
    private async Task ApplyGlobalRateLimitAsync(HttpContext context)
    {
        // Get client identifier (user ID if authenticated, IP address otherwise)
        var clientId = GetClientIdentifier(context);
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
        var limit = isAuthenticated ? _authenticatedLimit : _anonymousLimit;
        var redisKey = $"{RateLimitKeyPrefix}{clientId}";

        try
        {
            var db = _redis.GetDatabase();

            // Execute Lua script atomically
            var scriptResult = await db.ScriptEvaluateAsync(RateLimitScript, new
            {
                key = (RedisKey)redisKey,
                expiry = _windowSeconds
            });

            // Handle potential null result from Redis script
            if (scriptResult.IsNull)
            {
                _logger.LogWarning(
                    "Redis script returned invalid result for client {ClientId}; failing open.",
                    clientId);
                await _next(context);
                return;
            }

            var result = (RedisResult[]?)scriptResult;
            if (result == null || result.Length < 2)
            {
                _logger.LogWarning(
                    "Redis script returned invalid result array for client {ClientId}; failing open.",
                    clientId);
                await _next(context);
                return;
            }

            var currentCount = (long)result[0];
            var ttlSeconds = (long)result[1];
            var resetTime = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds).ToUnixTimeSeconds();

            // Check if limit exceeded
            if (currentCount > limit)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for client {ClientId} (authenticated: {IsAuthenticated}). Count: {Count}/{Max}",
                    clientId, isAuthenticated, currentCount, limit);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ttlSeconds.ToString();
                AddRateLimitHeaders(context.Response, limit, 0, resetTime);

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = $"Too many requests. Maximum {limit} requests per minute allowed.",
                    retryAfter = ttlSeconds
                });
                return;
            }

            // Add headers to the successful response
            var remaining = limit - currentCount;
            context.Response.OnStarting(() =>
            {
                AddRateLimitHeaders(context.Response, limit, remaining, resetTime);
                return Task.CompletedTask;
            });
        }
        catch (RedisConnectionException ex)
        {
            // TODO: MEDIUM - Rate limiting fails open when Redis is down. Risk: No brute force protection during Redis outage.
            // Consider failing closed for auth endpoints, or adding in-memory fallback rate limiting.
            // Fail open: allow request to proceed if Redis is unavailable
            // This prevents Redis outages from taking down the entire API
            _logger.LogError(ex,
                "Redis connection error for client {ClientId}; failing open.",
                clientId);
        }
        catch (RedisTimeoutException ex)
        {
            // Fail open on timeout
            _logger.LogError(ex,
                "Redis timeout for client {ClientId}; failing open.",
                clientId);
        }
        catch (Exception ex)
        {
            // Fail open for any other errors
            _logger.LogError(ex,
                "Rate limiting error for client {ClientId}; failing open.",
                clientId);
        }

        await _next(context);
    }

    /// <summary>
    /// Applies endpoint-specific rate limiting based on the EndpointRateLimit attribute.
    /// </summary>
    private async Task ApplyEndpointRateLimitAsync(HttpContext context, EndpointRateLimitAttribute rateLimitConfig)
    {
        string? trackingIdentifier;

        try
        {
            // Get tracking identifier based on strategy
            trackingIdentifier = rateLimitConfig.TrackBy switch
            {
                RateLimitTrackBy.IpAddress => GetIpAddress(context),
                RateLimitTrackBy.UserId => GetUserId(context),
                RateLimitTrackBy.Email => await GetEmailFromRequestBodyAsync(context),
                _ => GetIpAddress(context)
            };

            if (string.IsNullOrEmpty(trackingIdentifier))
            {
                _logger.LogWarning(
                    "Could not determine tracking identifier for endpoint rate limit (TrackBy: {TrackBy}); failing open.",
                    rateLimitConfig.TrackBy);
                await _next(context);
                return;
            }

            var endpoint = context.GetEndpoint();
            var endpointName = endpoint?.DisplayName ?? context.Request.Path.ToString();
            var redisKey = $"{EndpointRateLimitKeyPrefix}{endpointName}:{trackingIdentifier}";
            var windowSeconds = rateLimitConfig.WindowMinutes * 60;

            var db = _redis.GetDatabase();

            // Execute Lua script atomically
            var scriptResult = await db.ScriptEvaluateAsync(RateLimitScript, new
            {
                key = (RedisKey)redisKey,
                expiry = windowSeconds
            });

            // Handle potential null result from Redis script
            if (scriptResult.IsNull)
            {
                _logger.LogWarning(
                    "Redis script returned invalid result for endpoint rate limit; failing open.");
                await _next(context);
                return;
            }

            var result = (RedisResult[]?)scriptResult;
            if (result == null || result.Length < 2)
            {
                _logger.LogWarning(
                    "Redis script returned invalid result array for endpoint rate limit; failing open.");
                await _next(context);
                return;
            }

            var currentCount = (long)result[0];
            var ttlSeconds = (long)result[1];
            var resetTime = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds).ToUnixTimeSeconds();

            // Check if limit exceeded
            if (currentCount > rateLimitConfig.Requests)
            {
                _logger.LogWarning(
                    "Endpoint rate limit exceeded for {Endpoint} (TrackBy: {TrackBy}, Identifier: {Identifier}). Count: {Count}/{Max}",
                    endpointName, rateLimitConfig.TrackBy, trackingIdentifier, currentCount, rateLimitConfig.Requests);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ttlSeconds.ToString();
                AddRateLimitHeaders(context.Response, rateLimitConfig.Requests, 0, resetTime);

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = $"Too many requests. Maximum {rateLimitConfig.Requests} requests per {rateLimitConfig.WindowMinutes} minute(s) allowed.",
                    retryAfter = ttlSeconds
                });
                return;
            }

            // Add headers to the successful response
            var remaining = rateLimitConfig.Requests - currentCount;
            context.Response.OnStarting(() =>
            {
                AddRateLimitHeaders(context.Response, rateLimitConfig.Requests, remaining, resetTime);
                return Task.CompletedTask;
            });
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex,
                "Redis connection error for endpoint rate limit; failing open.");
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogError(ex,
                "Redis timeout for endpoint rate limit; failing open.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Endpoint rate limiting error; failing open.");
        }

        await _next(context);
    }

    /// <summary>
    /// Gets the IP address from the HTTP context.
    /// </summary>
    private static string GetIpAddress(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    /// <summary>
    /// Gets the user ID from the authenticated user claims.
    /// </summary>
    private static string? GetUserId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = context.User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;

        return string.IsNullOrEmpty(userId) ? null : $"user:{userId}";
    }

    /// <summary>
    /// Extracts email address from the request body for rate limiting.
    /// Enables request buffering to allow reading the body multiple times.
    /// </summary>
    private async Task<string?> GetEmailFromRequestBodyAsync(HttpContext context)
    {
        try
        {
            // Enable buffering to allow reading the body multiple times
            context.Request.EnableBuffering();

            // Read the request body
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            // Reset the stream position for the next middleware
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            // Parse JSON and extract email field
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("email", out var emailElement))
            {
                var email = emailElement.GetString();
                return string.IsNullOrEmpty(email) ? null : $"email:{email.ToLowerInvariant()}";
            }

            // Also check for "Email" with capital E
            if (document.RootElement.TryGetProperty("Email", out var emailElementCapital))
            {
                var email = emailElementCapital.GetString();
                return string.IsNullOrEmpty(email) ? null : $"email:{email.ToLowerInvariant()}";
            }

            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse request body as JSON for email extraction");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract email from request body");
            return null;
        }
    }

    /// <summary>
    /// Gets the client identifier for rate limiting.
    /// Uses user ID for authenticated users, IP address for anonymous users.
    /// </summary>
    private static string GetClientIdentifier(HttpContext context)
    {
        // Use user ID if authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        // Fall back to IP address for anonymous users
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    /// <summary>
    /// Adds standard rate limit headers to the response.
    /// </summary>
    private static void AddRateLimitHeaders(HttpResponse response, long limit, long remaining, long reset)
    {
        response.Headers["X-RateLimit-Limit"] = limit.ToString();
        response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        response.Headers["X-RateLimit-Reset"] = reset.ToString();
    }
}

/// <summary>
/// Extension method to register RateLimitingMiddleware
/// </summary>
//
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}

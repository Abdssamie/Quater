using System.Security.Claims;
using StackExchange.Redis;

namespace Quater.Backend.Api.Middleware;

/// <summary>
/// Redis-backed rate limiting middleware with authentication-aware limits.
/// Uses distributed Redis counters to support horizontal scaling.
/// Implements atomic increment+expire using Lua script to prevent race conditions.
/// 
/// Rate limits:
/// - Authenticated users: 100 requests per minute (configurable)
/// - Anonymous users: 20 requests per minute (configurable)
/// 
/// Tracks by:
/// - User ID for authenticated requests
/// - IP address for anonymous requests
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
        _authenticatedLimit = configuration.GetValue<int>("RateLimiting:AuthenticatedLimit", 100);
        _anonymousLimit = configuration.GetValue<int>("RateLimiting:AnonymousLimit", 20);
        _windowSeconds = configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);
        
        _logger.LogInformation(
            "Rate limiting configured - Authenticated: {AuthLimit} req/{Window}s, Anonymous: {AnonLimit} req/{Window}s",
            _authenticatedLimit, _windowSeconds, _anonymousLimit, _windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
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
    /// Gets the client identifier for rate limiting.
    /// Uses user ID for authenticated users, IP address for anonymous users.
    /// </summary>
    private static string GetClientIdentifier(HttpContext context)
    {
        // Use user ID if authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? context.User.FindFirst("sub")?.Value;
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
//
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}

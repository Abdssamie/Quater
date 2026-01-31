namespace Quater.Backend.Api.Middleware;

/// <summary>
/// Middleware that adds security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Content Security Policy - restricts resource loading
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'");

        // Prevent clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // Control referrer information
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Enable XSS protection (legacy browsers)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Disable feature policy for sensitive features
        context.Response.Headers.Append("Permissions-Policy", 
            "geolocation=(), microphone=(), camera=()");

        await _next(context);
    }
}

/// <summary>
/// Extension method to register SecurityHeadersMiddleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

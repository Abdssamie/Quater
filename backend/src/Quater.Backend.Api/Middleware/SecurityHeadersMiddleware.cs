namespace Quater.Backend.Api.Middleware;

/// <summary>
/// Middleware that adds security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use OnStarting to ensure headers are added before response starts
        // This prevents "Headers are read-only, response has already started" errors
        context.Response.OnStarting(() =>
        {
            if (context.Response.HasStarted) return Task.CompletedTask;
            // Content Security Policy - restricts resource loading
            // Development: Relaxed policy for Swagger UI compatibility
            // Production: Stricter policy without unsafe-inline/unsafe-eval
            var cspPolicy = _environment.IsDevelopment()
                // Development Policy
                ? "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data: https:; " +
                  "font-src 'self' data:; " +
                  "connect-src 'self'; " +
                  "frame-ancestors 'none'"
                // Production Policy
                : "default-src 'self'; " +
                  "script-src 'self'; " +
                  "style-src 'self'; " +
                  "img-src 'self' data: https:; " +
                  "font-src 'self' data:; " +
                  "connect-src 'self'; " +
                  "frame-ancestors 'none'; " +
                  "base-uri 'self'; " +
                  "form-action 'self'";
                
            context.Response.Headers.Append("Content-Security-Policy", cspPolicy);

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

            return Task.CompletedTask;
        });

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

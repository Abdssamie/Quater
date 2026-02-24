using System.Security.Claims;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Enums;

namespace Quater.Backend.Api.Middleware;

/// <summary>
/// Middleware that intercepts requests, reads the X-Lab-Id header, and sets the lab context for the request.
/// Lab membership validation is performed by LabContextAuthorizationHandler.
/// </summary>
public sealed class LabContextMiddleware(
    RequestDelegate next,
    ILogger<LabContextMiddleware> logger)
{
    /// <summary>
    /// Processes the HTTP request and sets the lab context if X-Lab-Id header is present.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ILabContextAccessor labContext)
    {
        var userId = context.User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        var path = context.Request.Path.Value;
        
        // Always write to console for visibility
        Console.WriteLine($"[LabContextMiddleware] Request: {context.Request.Method} {path}, UserId: {userId ?? "NULL"}, IsAuthenticated: {context.User.Identity?.IsAuthenticated ?? false}");
        logger.LogInformation("[LabContextMiddleware] Request: {Method} {Path}, UserId: {UserId}, IsAuthenticated: {IsAuth}", 
            context.Request.Method, path, userId ?? "NULL", context.User.Identity?.IsAuthenticated ?? false);
        
        // Detect system admin — bypasses lab membership check and RLS
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            if (userGuid == Quater.Backend.Core.Constants.System.GetId())
            {
                labContext.SetSystemAdmin();

                logger.LogDebug(
                    "System admin detected: UserId={UserId} — RLS bypass enabled",
                    userGuid);

                await next(context);
                return;
            }

            var labIdHeader = context.Request.Headers["X-Lab-Id"].ToString();

            if (!string.IsNullOrEmpty(labIdHeader) && Guid.TryParse(labIdHeader, out var labId))
            {
                // Set context with default role - actual role will be validated by authorization handler
                labContext.SetContext(labId, UserRole.Viewer);
                
                logger.LogDebug(
                    "Lab context set: UserId={UserId}, LabId={LabId}",
                    userGuid,
                    labId);
            }
        }

        await next(context);
    }
}

/// <summary>
/// Extension methods for registering the lab context middleware.
/// </summary>
public static class LabContextMiddlewareExtensions
{
    /// <summary>
    /// Adds the lab context middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseLabContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LabContextMiddleware>();
    }
}

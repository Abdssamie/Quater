using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Models;

namespace Quater.Backend.Api.Middleware;

/// <summary>
/// Middleware that intercepts requests, reads the X-Lab-Id header, validates user access via UserLab,
/// and sets the lab context for the request.
/// </summary>
public sealed class LabContextMiddleware(
    RequestDelegate next,
    ILogger<LabContextMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<LabContextMiddleware> _logger = logger;

    /// <summary>
    /// Processes the HTTP request and sets the lab context if X-Lab-Id header is present.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ILabContextAccessor labContext,
        QuaterDbContext db,
        UserManager<User> userManager)
    {
        var labIdHeader = context.Request.Headers["X-Lab-Id"].ToString();
        var userId = context.User.FindFirstValue(OpenIddictConstants.Claims.Subject);

        if (!string.IsNullOrEmpty(labIdHeader)
            && Guid.TryParse(labIdHeader, out var labId)
            && !string.IsNullOrEmpty(userId)
            && Guid.TryParse(userId, out var userGuid))
        {
            // Check UserLab table for membership and role
            var userLab = await db.UserLabs
                .AsNoTracking()
                .FirstOrDefaultAsync(ul => ul.UserId == userGuid && ul.LabId == labId, context.RequestAborted);

            if (userLab != null)
            {
                labContext.SetContext(labId, userLab.Role);
                _logger.LogDebug(
                    "Lab context set: UserId={UserId}, LabId={LabId}, Role={Role}",
                    userGuid,
                    labId,
                    userLab.Role);
            }
            else
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access Lab {LabId} without membership",
                    userGuid,
                    labId);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(
                    new { error = "User is not a member of the requested lab" },
                    context.RequestAborted);
                return;
            }
        }

        await _next(context);
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

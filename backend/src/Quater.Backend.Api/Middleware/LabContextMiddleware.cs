using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Exceptions;
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
        QuaterDbContext db)
    {
        var userId = context.User.FindFirstValue(OpenIddictConstants.Claims.Subject);

        // Detect system admin — bypasses lab membership check and RLS
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            if (userGuid == SystemUser.GetId())
            {
                labContext.SetSystemAdmin();

                _logger.LogDebug(
                    "System admin detected: UserId={UserId} — RLS bypass enabled",
                    userGuid);

                await _next(context);
                return;
            }

            var labIdHeader = context.Request.Headers["X-Lab-Id"].ToString();

            if (!string.IsNullOrEmpty(labIdHeader) && Guid.TryParse(labIdHeader, out var labId))
            {
                // Check UserLab table for membership and role
                var userLab = await db.UserLabs
                    .Include(ul => ul.Lab)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ul => 
                        ul.UserId == userGuid && 
                        ul.LabId == labId && 
                        !ul.Lab.IsDeleted, 
                        context.RequestAborted);

                if (userLab is null)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to access Lab {LabId} without membership",
                        userGuid,
                        labId);
                    throw new ForbiddenException(ErrorMessages.UserNotLabMember);
                }

                labContext.SetContext(labId, userLab.Role);
                
                // Set PostgreSQL session variables for RLS policies
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT set_config('app.current_lab_id', {0}, true)",
                    labId.ToString(),
                    context.RequestAborted);
                
                _logger.LogDebug(
                    "Lab context set: UserId={UserId}, LabId={LabId}, Role={Role}",
                    userGuid,
                    labId,
                    userLab.Role);
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

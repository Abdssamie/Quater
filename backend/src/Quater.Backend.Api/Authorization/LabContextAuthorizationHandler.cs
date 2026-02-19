using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Enums;

namespace Quater.Backend.Api.Authorization;

/// <summary>
/// Authorization requirement that checks if the user has a specific role in the current lab context.
/// </summary>
public class LabContextRoleRequirement(UserRole minimumRole) : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the minimum required role for authorization.
    /// </summary>
    public UserRole MinimumRole { get; } = minimumRole;
}

/// <summary>
/// Authorization handler that validates user role in the current lab context.
/// Uses injected DbContext to validate membership and update ILabContextAccessor with actual role.
/// </summary>
public class LabContextAuthorizationHandler(
    ILabContextAccessor labContext,
    QuaterDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LabContextAuthorizationHandler> logger)
    : AuthorizationHandler<LabContextRoleRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        LabContextRoleRequirement requirement)
    {
        // System admins bypass all lab-specific authorization
        if (labContext.IsSystemAdmin)
        {
            logger.LogDebug("Authorization succeeded: System admin bypass");
            context.Succeed(requirement);
            return;
        }

        // If no lab context is set, fail with message
        if (!labContext.CurrentLabId.HasValue)
        {
            logger.LogWarning("Authorization failed: No lab context provided");
            context.Fail(new AuthorizationFailureReason(this, ErrorMessages.LabContextRequired));
            return;
        }

        // Get user ID from claims
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            logger.LogWarning("Authorization failed: HttpContext is null");
            context.Fail(new AuthorizationFailureReason(this, "HttpContext is not available"));
            return;
        }

        var userId = httpContext.User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            logger.LogWarning("Authorization failed: Invalid or missing user ID in claims");
            context.Fail(new AuthorizationFailureReason(this, "Invalid user ID"));
            return;
        }

        // Query UserLabs table to validate membership and get actual role
        var userLab = await dbContext.UserLabs
            .AsNoTracking()
            .FirstOrDefaultAsync(ul => ul.UserId == userGuid && ul.LabId == labContext.CurrentLabId.Value);

        // If not a member, log warning and fail
        if (userLab == null)
        {
            logger.LogWarning(
                "Authorization failed: User {UserId} is not a member of lab {LabId}",
                userGuid,
                labContext.CurrentLabId.Value);
            context.Fail(new AuthorizationFailureReason(this, ErrorMessages.UserNotLabMember));
            return;
        }

        // Update ILabContextAccessor with actual role from database
        labContext.SetContext(labContext.CurrentLabId.Value, userLab.Role);

        // Check if user's role meets requirement (userLab.Role >= requirement.MinimumRole)
        var hasRequiredRole = (int)userLab.Role >= (int)requirement.MinimumRole;

        if (hasRequiredRole)
        {
            logger.LogDebug(
                "Authorization succeeded: User {UserId} has role {Role} in lab {LabId}, required {RequiredRole}",
                userGuid,
                userLab.Role,
                labContext.CurrentLabId.Value,
                requirement.MinimumRole);
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning(
                "Authorization failed: User {UserId} has role {Role} in lab {LabId}, but requires {RequiredRole}",
                userGuid,
                userLab.Role,
                labContext.CurrentLabId.Value,
                requirement.MinimumRole);
            context.Fail(new AuthorizationFailureReason(this, ErrorMessages.InsufficientLabPermissions));
        }
    }
}

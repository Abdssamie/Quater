using Microsoft.AspNetCore.Authorization;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Enums;

namespace Quater.Backend.Api.Authorization;

/// <summary>
/// Authorization requirement that checks if the user has a specific role in the current lab context.
/// </summary>
public class LabContextRoleRequirement(UserRole minimumRole) : IAuthorizationRequirement
{
    public UserRole MinimumRole { get; } = minimumRole;
}

/// <summary>
/// Authorization handler that validates user role in the current lab context.
/// Uses ILabContextAccessor to get the user's role in the lab specified by X-Lab-Id header.
/// </summary>
public class LabContextAuthorizationHandler(ILabContextAccessor labContext)
    : AuthorizationHandler<LabContextRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        LabContextRoleRequirement requirement)
    {
        // System admins bypass all lab-specific authorization
        if (labContext.IsSystemAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // If no lab context is set, fail with message
        if (!labContext.CurrentRole.HasValue)
        {
            // TODO: Add Sentry logging for authorization failures (security auditing)
            // Example: _logger.LogWarning("Authorization failed: No lab context provided for user {UserId}", userId);
            context.Fail(new AuthorizationFailureReason(this, ErrorMessages.LabContextRequired));
            return Task.CompletedTask;
        }

        // Check if user's role in current lab meets minimum requirement
        // Uses explicit enum values: Viewer (1) < Technician (2) < Admin (3)
        var hasRequiredRole = (int)labContext.CurrentRole.Value >= (int)requirement.MinimumRole;

        if (hasRequiredRole)
        {
            context.Succeed(requirement);
        }
        else
        {
            // TODO: Add Sentry logging for authorization failures (security auditing)
            // Example: _logger.LogWarning("Authorization failed: User requires {RequiredRole} but has {CurrentRole} in lab {LabId}",
            //     requirement.MinimumRole, labContext.CurrentRole, labContext.CurrentLabId);
            context.Fail(new AuthorizationFailureReason(this, ErrorMessages.InsufficientLabPermissions));
        }

        return Task.CompletedTask;
    }
}

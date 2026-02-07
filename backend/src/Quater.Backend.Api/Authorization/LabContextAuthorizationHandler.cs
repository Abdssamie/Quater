using Microsoft.AspNetCore.Authorization;
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

        // If no lab context is set, fail (user must provide X-Lab-Id header)
        if (!labContext.CurrentRole.HasValue)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Check if user's role in current lab meets minimum requirement
        var hasRequiredRole = requirement.MinimumRole switch
        {
            UserRole.Viewer => labContext.CurrentRole == UserRole.Viewer ||
                               labContext.CurrentRole == UserRole.Technician ||
                               labContext.CurrentRole == UserRole.Admin,
            UserRole.Technician => labContext.CurrentRole == UserRole.Technician ||
                                   labContext.CurrentRole == UserRole.Admin,
            UserRole.Admin => labContext.CurrentRole == UserRole.Admin,
            _ => false
        };

        if (hasRequiredRole)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}

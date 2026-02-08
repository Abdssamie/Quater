using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public UserRole MinimumRole { get; } = minimumRole;
}

/// <summary>
/// Authorization handler that validates user role in the current lab context.
/// Creates temporary DbContext to validate membership and update ILabContextAccessor with actual role.
/// </summary>
public class LabContextAuthorizationHandler : AuthorizationHandler<LabContextRoleRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LabContextAuthorizationHandler> _logger;

    public LabContextAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<LabContextAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        LabContextRoleRequirement requirement)
    {
        // Get ILabContextAccessor from HttpContext.RequestServices
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("Authorization failed: HttpContext is null");
            context.Fail(new AuthorizationFailureReason(this, "HttpContext is not available"));
            return;
        }

        var labContext = httpContext.RequestServices.GetRequiredService<ILabContextAccessor>();

        // System admins bypass all lab-specific authorization
        if (labContext.IsSystemAdmin)
        {
            _logger.LogDebug("Authorization succeeded: System admin bypass");
            context.Succeed(requirement);
            return;
        }

        // If no lab context is set, fail with message
        if (!labContext.CurrentLabId.HasValue)
        {
            _logger.LogWarning("Authorization failed: No lab context provided");
            context.Fail(new AuthorizationFailureReason(this, ErrorMessages.LabContextRequired));
            return;
        }

        // Get user ID from claims
        var userId = httpContext.User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning("Authorization failed: Invalid or missing user ID in claims");
            context.Fail(new AuthorizationFailureReason(this, "Invalid user ID"));
            return;
        }

        // Create temporary DbContext manually using connection string from IConfiguration
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Authorization failed: Connection string not found");
            context.Fail(new AuthorizationFailureReason(this, "Database configuration error"));
            return;
        }

        var optionsBuilder = new DbContextOptionsBuilder<QuaterDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        await using var dbContext = new QuaterDbContext(optionsBuilder.Options);

        // Query UserLabs table to validate membership and get actual role
        var userLab = await dbContext.UserLabs
            .AsNoTracking()
            .FirstOrDefaultAsync(ul => ul.UserId == userGuid && ul.LabId == labContext.CurrentLabId.Value);

        // If not a member, log warning and fail
        if (userLab == null)
        {
            _logger.LogWarning(
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
            _logger.LogDebug(
                "Authorization succeeded: User {UserId} has role {Role} in lab {LabId}, required {RequiredRole}",
                userGuid,
                userLab.Role,
                labContext.CurrentLabId.Value,
                requirement.MinimumRole);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Authorization failed: User {UserId} has role {Role} in lab {LabId}, but requires {RequiredRole}",
                userGuid,
                userLab.Role,
                labContext.CurrentLabId.Value,
                requirement.MinimumRole);
            context.Fail(new AuthorizationFailureReason(this, ErrorMessages.InsufficientLabPermissions));
        }
    }
}

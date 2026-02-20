using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using OpenIddict.Abstractions;

namespace Quater.Backend.Services;

/// <summary>
/// Service for retrieving current user information from HTTP context.
/// Uses OpenIddict's 'sub' claim for consistency with JWT access tokens.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current authenticated user's ID.
    /// Uses OpenIddict's 'sub' claim for consistency with JWT access tokens.
    /// </summary>
    /// <returns>The user ID from claims.</returns>
    public Guid GetCurrentUserId()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(OpenIddictConstants.Claims.Subject);

        if (string.IsNullOrEmpty(userIdString))
        {
            throw new UnauthorizedAccessException("Current user is not authenticated.");
        }

        return Guid.TryParse(userIdString, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Current user ID claim is invalid.");
    }

    /// <summary>
    /// Gets the current authenticated user's ID or returns the system user ID.
    /// </summary>
    /// <returns>The user ID from claims, or System user ID if not authenticated or invalid.</returns>
    public Guid GetCurrentUserIdOrSystem()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(OpenIddictConstants.Claims.Subject);

        if (string.IsNullOrEmpty(userIdString))
        {
            return SystemUser.GetId();
        }

        return Guid.TryParse(userIdString, out var userId)
            ? userId
            : SystemUser.GetId();
    }
}

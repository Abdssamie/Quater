using System.Security.Claims;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Constants;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information.
/// Uses OpenIddict claim types (sub, name, email) for consistency with JWT tokens.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the ClaimsPrincipal, or throws if not found.
    /// Uses OpenIddict's 'sub' claim for consistency with JWT access tokens.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when user ID is not found in claims.</exception>
    public static Guid GetUserIdOrThrow(this ClaimsPrincipal principal)
    {
        var userIdString = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userIdString))
        {
            throw new InvalidOperationException("User ID not found in claims. Ensure the user is authenticated.");
        }

        if (!Guid.TryParse(userIdString, out var userId))
        {
            throw new InvalidOperationException("User ID in claims is not a valid GUID.");
        }

        return userId;
    }

    /// <summary>
    /// Gets the user's email from the ClaimsPrincipal.
    /// Uses OpenIddict's 'email' claim for consistency with JWT access tokens.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user's email, or null if not found.</returns>
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(OpenIddictConstants.Claims.Email);
    }

    /// <summary>
    /// Gets the user's role from the ClaimsPrincipal.
    /// Uses Quater's custom role claim for multi-tenant authorization.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user's role, or null if not found.</returns>
    public static string? GetUserRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(QuaterClaimTypes.Role);
    }
}

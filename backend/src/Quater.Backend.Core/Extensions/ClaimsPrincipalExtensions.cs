using System.Security.Claims;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the ClaimsPrincipal, or throws if not found.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when user ID is not found in claims.</exception>
    // TODO: CRITICAL - Uses ClaimTypes.NameIdentifier instead of OpenIddictConstants.Claims.Subject.
    // OpenIddict tokens use 'sub' claim, not the legacy ASP.NET NameIdentifier claim type.
    // Risk: Inconsistent claim handling, potential authentication bypass if tokens don't contain NameIdentifier.
    // Should use: principal.FindFirstValue(OpenIddictConstants.Claims.Subject)
    public static Guid GetUserIdOrThrow(this ClaimsPrincipal principal)
    {
        var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
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
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user's email, or null if not found.</returns>
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email);
    }

    /// <summary>
    /// Gets the user's role from the ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user's role, or null if not found.</returns>
    public static string? GetUserRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Role);
    }
}

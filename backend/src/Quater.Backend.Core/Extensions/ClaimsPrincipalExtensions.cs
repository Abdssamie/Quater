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
    public static string GetUserIdOrThrow(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User ID not found in claims. Ensure the user is authenticated.");
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

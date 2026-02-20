namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Interface for getting current user information.
/// Implement this in your application to provide user context to the audit interceptor.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    /// <returns>The user ID.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when no user is authenticated or the user ID claim is invalid.
    /// </exception>
    Guid GetCurrentUserId();

    /// <summary>
    /// Gets the current user's ID, or returns the system user ID when unauthenticated.
    /// </summary>
    /// <returns>The current user ID if authenticated; otherwise the system user ID.</returns>
    Guid GetCurrentUserIdOrSystem();
}

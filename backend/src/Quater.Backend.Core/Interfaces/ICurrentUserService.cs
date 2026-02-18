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
    /// <returns>The user ID. Throws if no user is authenticated.</returns>
    Guid GetCurrentUserId();
}

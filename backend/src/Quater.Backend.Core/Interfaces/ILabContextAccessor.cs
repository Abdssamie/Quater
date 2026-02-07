using Quater.Shared.Enums;

namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Provides access to the current lab context for the request.
/// </summary>
public interface ILabContextAccessor
{
    /// <summary>
    /// Gets the current lab ID from the request context.
    /// </summary>
    Guid? CurrentLabId { get; }

    /// <summary>
    /// Gets the current user's role within the lab.
    /// </summary>
    UserRole? CurrentRole { get; }

    /// <summary>
    /// Gets whether the current user is a system admin (bypasses RLS).
    /// </summary>
    bool IsSystemAdmin { get; }

    /// <summary>
    /// Sets the lab context for the current request.
    /// </summary>
    /// <param name="labId">The lab ID.</param>
    /// <param name="role">The user's role within the lab.</param>
    void SetContext(Guid labId, UserRole role);

    /// <summary>
    /// Marks the current request as a system administrator request.
    /// </summary>
    void SetSystemAdmin();
}

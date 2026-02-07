using Quater.Backend.Core.Interfaces;
using Quater.Shared.Enums;

namespace Quater.Backend.Services;

/// <summary>
/// Implementation of ILabContextAccessor that stores lab context per request.
/// </summary>
public sealed class LabContextAccessor : ILabContextAccessor
{
    /// <summary>
    /// Gets the current lab ID from the request context.
    /// </summary>
    public Guid? CurrentLabId { get; private set; }

    /// <summary>
    /// Gets the current user's role within the lab.
    /// </summary>
    public UserRole? CurrentRole { get; private set; }

    /// <summary>
    /// Sets the lab context for the current request.
    /// </summary>
    /// <param name="labId">The lab ID.</param>
    /// <param name="role">The user's role within the lab.</param>
    public void SetContext(Guid labId, UserRole role)
    {
        CurrentLabId = labId;
        CurrentRole = role;
    }
}

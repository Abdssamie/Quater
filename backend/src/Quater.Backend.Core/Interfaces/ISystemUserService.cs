namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Service for retrieving the system admin user ID.
/// The system admin is identified by email (admin@quater.local) and is auto-created on first run.
/// </summary>
public interface ISystemUserService
{
    /// <summary>
    /// Gets the system admin user ID from the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The system admin user ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when system admin user is not found in database.</exception>
    Task<Guid> GetSystemUserIdAsync(CancellationToken ct = default);
}

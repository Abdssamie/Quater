using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;

namespace Quater.Backend.Services;

/// <summary>
/// Service for retrieving the system admin user ID from the database.
/// The system admin is identified by email (admin@quater.local).
/// </summary>
public sealed class SystemUserService(
    UserManager<User> userManager,
    ILogger<SystemUserService> logger) : ISystemUserService
{
    private Guid? _cachedSystemUserId;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Gets the system admin user ID from the database.
    /// Results are cached after first lookup for performance.
    /// </summary>
    public async Task<Guid> GetSystemUserIdAsync(CancellationToken ct = default)
    {
        // Return cached value if available
        if (_cachedSystemUserId.HasValue)
        {
            return _cachedSystemUserId.Value;
        }

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cachedSystemUserId.HasValue)
            {
                return _cachedSystemUserId.Value;
            }

            // Look up system admin by email
            const string adminEmail = "admin@quater.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser is null)
            {
                logger.LogError(
                    "System admin user not found in database. Expected email: {Email}. " +
                    "This indicates the database seeder did not run successfully.",
                    adminEmail);
                
                throw new InvalidOperationException(
                    $"System admin user not found in database. Expected email: {adminEmail}. " +
                    "Please ensure the database seeder has run successfully.");
            }

            _cachedSystemUserId = adminUser.Id;
            logger.LogDebug("System admin user ID cached: {UserId}", _cachedSystemUserId);
            
            return _cachedSystemUserId.Value;
        }
        finally
        {
            _lock.Release();
        }
    }
}

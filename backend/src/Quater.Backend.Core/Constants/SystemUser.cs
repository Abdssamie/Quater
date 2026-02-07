namespace Quater.Backend.Core.Constants;

/// <summary>
/// System admin user configuration.
/// The system admin ID must be set via SYSTEM_ADMIN_USER_ID environment variable.
/// </summary>
public static class SystemUser
{
    private static Guid? _cachedId;
    
    /// <summary>
    /// Gets the system admin user ID from environment variable.
    /// </summary>
    /// <returns>The system admin user ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when SYSTEM_ADMIN_USER_ID environment variable is not set or invalid.</exception>
    public static Guid GetId()
    {
        if (_cachedId.HasValue)
        {
            return _cachedId.Value;
        }
        
        var systemAdminId = Environment.GetEnvironmentVariable("SYSTEM_ADMIN_USER_ID");
        
        if (string.IsNullOrWhiteSpace(systemAdminId))
        {
            throw new InvalidOperationException(
                "SYSTEM_ADMIN_USER_ID environment variable is not set. " +
                "Please set this variable to a valid GUID for the system administrator user.");
        }
        
        if (!Guid.TryParse(systemAdminId, out var parsedId))
        {
            throw new InvalidOperationException(
                $"SYSTEM_ADMIN_USER_ID environment variable '{systemAdminId}' is not a valid GUID. " +
                "Please provide a valid GUID format (e.g., eb4b0ebc-7a02-43ca-a858-656bd7e4357f).");
        }
        
        _cachedId = parsedId;
        return parsedId;
    }
}
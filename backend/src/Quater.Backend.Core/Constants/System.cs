namespace Quater.Backend.Core.Constants;

/// <summary>
/// System user configuration for background operations and audit trails.
/// Uses a well-known constant GUID that is safe for single-tenant deployments
/// where each client has their own isolated database.
/// </summary>
public static class System
{
    /// <summary>
    /// Well-known GUID for system operations (background jobs, migrations, seeders).
    /// This is the same GUID used for the admin user created during database seeding.
    /// Safe for single-tenant deployments where databases are isolated per client.
    /// </summary>
    private static readonly Guid SystemId = new("eb4b0ebc-7a02-43ca-a858-656bd7e4357f");
    
    /// <summary>
    /// Gets the system user ID.
    /// </summary>
    /// <returns>The system user ID.</returns>
    public static Guid GetId() => SystemId;
}

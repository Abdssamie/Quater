namespace Quater.Shared.Enums;

/// <summary>
/// Type of action performed on an entity for audit logging.
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Entity was created
    /// </summary>
    Create,

    /// <summary>
    /// Entity was updated/modified
    /// </summary>
    Update,

    /// <summary>
    /// Entity was deleted (soft or hard delete)
    /// </summary>
    Delete,

    /// <summary>
    /// Entity was restored from soft delete
    /// </summary>
    Restore,

    /// <summary>
    /// Conflict was resolved during synchronization
    /// </summary>
    ConflictResolution
}

namespace Quater.Shared.Interfaces;

/// <summary>
/// Interface for entities that require audit tracking.
/// Tracks who created and modified the entity and when.
/// All audit properties are automatically managed by AuditInterceptor.
/// </summary>
public interface IAuditable
{
    Guid Id { get; } // Every auditable entity MUST have a Guid Id
    
    /// <summary>
    /// Gets the date and time when the entity was created.
    /// Automatically set by AuditInterceptor on entity creation.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who created the entity.
    /// Automatically set by AuditInterceptor on entity creation.
    /// </summary>
    string CreatedBy { get; }

    /// <summary>
    /// Gets the date and time when the entity was last updated.
    /// Automatically set by AuditInterceptor on entity modification.
    /// </summary>
    DateTime? UpdatedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who last updated the entity.
    /// Automatically set by AuditInterceptor on entity modification.
    /// </summary>
    string? UpdatedBy { get; }
}

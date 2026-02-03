namespace Quater.Shared.Interfaces;

/// <summary>
/// Interface for entities that require audit tracking.
/// Tracks who created and modified the entity and when.
/// </summary>
public interface IAuditable
{
    Guid Id { get; } // Every auditable entity MUST have a Guid Id
    
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; set; }
}

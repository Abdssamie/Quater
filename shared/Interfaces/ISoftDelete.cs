namespace Quater.Shared.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft-deleted entities are marked as deleted but not physically removed from the database.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was soft-deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }
}

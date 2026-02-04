namespace Quater.Shared.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft-deleted entities are marked as deleted but not physically removed from the database.
/// IsDeleted is managed by SoftDeleteInterceptor and should have a private setter in implementations.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets a value indicating whether the entity has been soft-deleted.
    /// This property is managed by SoftDeleteInterceptor via reflection.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets or sets the date and time when the entity was soft-deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }
}

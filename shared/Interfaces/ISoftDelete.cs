namespace Quater.Shared.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft-deleted entities are marked as deleted but not physically removed from the database.
/// All three soft-delete properties are managed exclusively through a <c>MarkDeleted</c>
/// domain method on the implementing entity; direct assignment is intentionally disallowed.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets the date and time when the entity was soft-deleted.
    /// </summary>
    DateTime? DeletedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who soft-deleted the entity.
    /// </summary>
    string? DeletedBy { get; }
}

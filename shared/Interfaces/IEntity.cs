namespace Quater.Shared.Interfaces;

/// <summary>
/// Base interface for all entities in the system.
/// Provides a unique identifier for each entity.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    Guid Id { get; set; }
}

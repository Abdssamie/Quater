namespace Quater.Shared.Interfaces;

/// <summary>
/// Interface for entities that support optimistic concurrency control.
/// Uses a row version to detect concurrent modifications.
/// </summary>
public interface IConcurrent
{
    /// <summary>
    /// Gets or sets the row version for optimistic concurrency control.
    /// This value is automatically updated by the database on each modification.
    /// </summary>
    byte[] RowVersion { get; set; }
}

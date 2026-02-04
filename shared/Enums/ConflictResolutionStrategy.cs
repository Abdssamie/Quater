namespace Quater.Shared.Enums;

/// <summary>
/// Strategy for resolving conflicts during synchronization between desktop and backend.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// The most recently modified version wins (based on LastModified timestamp)
    /// </summary>
    LastWriteWins,

    /// <summary>
    /// The server version always takes precedence
    /// </summary>
    ServerWins,

    /// <summary>
    /// The client version always takes precedence
    /// </summary>
    ClientWins,

    /// <summary>
    /// Manual resolution required - conflict is flagged for user intervention
    /// </summary>
    Manual
}

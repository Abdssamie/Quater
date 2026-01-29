using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Service for resolving synchronization conflicts
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Detect if a conflict exists between server and client versions
    /// </summary>
    bool HasConflict<T>(T serverEntity, T clientEntity) where T : ISyncable;

    /// <summary>
    /// Resolve conflict using specified strategy
    /// </summary>
    T ResolveConflict<T>(
        T serverEntity,
        T clientEntity,
        ConflictResolutionStrategy strategy) where T : ISyncable;

    /// <summary>
    /// Determine which version wins based on Last-Write-Wins strategy
    /// </summary>
    T ResolveLastWriteWins<T>(T serverEntity, T clientEntity) where T : ISyncable;

    /// <summary>
    /// Resolve using Server-Wins strategy
    /// </summary>
    T ResolveServerWins<T>(T serverEntity, T clientEntity) where T : ISyncable;

    /// <summary>
    /// Resolve using Client-Wins strategy
    /// </summary>
    T ResolveClientWins<T>(T serverEntity, T clientEntity) where T : ISyncable;
}

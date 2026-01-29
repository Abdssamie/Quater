using Quater.Backend.Core.Interfaces;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Backend.Sync;

/// <summary>
/// Service for resolving synchronization conflicts
/// </summary>
public class ConflictResolver : IConflictResolver
{
    /// <inheritdoc/>
    public bool HasConflict<T>(T serverEntity, T clientEntity) where T : ISyncable
    {
        // A conflict exists if both versions have been modified since last sync
        // and they have different LastSyncedAt timestamps
        return serverEntity.LastSyncedAt != clientEntity.LastSyncedAt;
    }

    /// <inheritdoc/>
    public T ResolveConflict<T>(
        T serverEntity,
        T clientEntity,
        ConflictResolutionStrategy strategy) where T : ISyncable
    {
        return strategy switch
        {
            ConflictResolutionStrategy.LastWriteWins => ResolveLastWriteWins(serverEntity, clientEntity),
            ConflictResolutionStrategy.ServerWins => ResolveServerWins(serverEntity, clientEntity),
            ConflictResolutionStrategy.ClientWins => ResolveClientWins(serverEntity, clientEntity),
            ConflictResolutionStrategy.Manual => throw new InvalidOperationException(
                "Manual conflict resolution requires user intervention"),
            _ => throw new ArgumentException($"Unknown conflict resolution strategy: {strategy}")
        };
    }

    /// <inheritdoc/>
    public T ResolveLastWriteWins<T>(T serverEntity, T clientEntity) where T : ISyncable
    {
        // Compare LastSyncedAt timestamps - most recent wins
        return serverEntity.LastSyncedAt > clientEntity.LastSyncedAt
            ? serverEntity
            : clientEntity;
    }

    /// <inheritdoc/>
    public T ResolveServerWins<T>(T serverEntity, T clientEntity) where T : ISyncable
    {
        // Server version always wins
        return serverEntity;
    }

    /// <inheritdoc/>
    public T ResolveClientWins<T>(T serverEntity, T clientEntity) where T : ISyncable
    {
        // Client version always wins
        return clientEntity;
    }
}

namespace Quater.Desktop.Core.Sync;

public sealed class ConflictResolutionService : IConflictResolutionService
{
    public Task ResolveAsync(string conflictId, ConflictResolutionChoice choice, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conflictId);
        return Task.CompletedTask;
    }
}

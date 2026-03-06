namespace Quater.Desktop.Core.Sync;

public interface IConflictResolutionService
{
    Task ResolveAsync(string conflictId, ConflictResolutionChoice choice, CancellationToken ct = default);
}

public enum ConflictResolutionChoice
{
    KeepLocal,
    KeepServer,
    Reload
}

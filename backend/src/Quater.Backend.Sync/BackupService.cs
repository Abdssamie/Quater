using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Backend.Data.Interceptors;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Sync;

/// <summary>
/// Service for creating and managing conflict backups
/// </summary>
public class BackupService : IBackupService
{
    private readonly QuaterDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUserService _currentUserService;

    public BackupService(
        QuaterDbContext context,
        TimeProvider timeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _timeProvider = timeProvider;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc/>
    public async Task<ConflictBackup> CreateBackupAsync(
        Guid entityId,
        string entityType,
        string serverVersion,
        string clientVersion,
        ConflictResolutionStrategy strategy,
        string deviceId,
        Guid labId,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var currentUser = _currentUserService.GetCurrentUserId() ?? "system";

        var backup = new ConflictBackup
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntityType = entityType,
            ServerVersion = serverVersion,
            ClientVersion = clientVersion,
            ResolutionStrategy = strategy,
            DeviceId = deviceId,
            LabId = labId,
            ConflictDetectedAt = now,
            CreatedDate = now,
            CreatedAt = now,
            CreatedBy = currentUser
        };

        _context.ConflictBackups.Add(backup);
        await _context.SaveChangesAsync(ct);

        return backup;
    }

    /// <inheritdoc/>
    public async Task MarkAsResolvedAsync(
        Guid backupId,
        string resolvedBy,
        string? notes = null,
        CancellationToken ct = default)
    {
        var backup = await _context.ConflictBackups.FindAsync(new object[] { backupId }, ct);
        if (backup == null)
            throw new InvalidOperationException($"ConflictBackup with ID {backupId} not found");

        backup.ResolvedAt = _timeProvider.GetUtcNow().UtcDateTime;
        backup.ResolvedBy = resolvedBy;
        backup.ResolutionNotes = notes;
        backup.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        backup.UpdatedBy = resolvedBy;

        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<ConflictBackup>> GetUnresolvedConflictsAsync(
        Guid labId,
        CancellationToken ct = default)
    {
        return await _context.ConflictBackups
            .Where(b => b.LabId == labId && b.ResolvedAt == null)
            .OrderByDescending(b => b.ConflictDetectedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<ConflictBackup?> GetBackupByEntityAsync(
        Guid entityId,
        string entityType,
        CancellationToken ct = default)
    {
        return await _context.ConflictBackups
            .Where(b => b.EntityId == entityId && b.EntityType == entityType)
            .OrderByDescending(b => b.ConflictDetectedAt)
            .FirstOrDefaultAsync(ct);
    }
}

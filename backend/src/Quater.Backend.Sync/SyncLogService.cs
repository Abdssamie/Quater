using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Sync;

/// <summary>
/// Service for tracking synchronization operations
/// </summary>
public class SyncLogService : ISyncLogService
{
    private readonly QuaterDbContext _context;
    private readonly TimeProvider _timeProvider;

    public SyncLogService(QuaterDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<SyncLog> CreateSyncLogAsync(
        string deviceId,
        string userId,
        SyncStatus status,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        
        var syncLog = new SyncLog
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            UserId = userId,
            Status = status,
            LastSyncTimestamp = now,
            CreatedDate = now,
            RecordsSynced = 0,
            ConflictsDetected = 0,
            ConflictsResolved = 0,
            LastSyncedAt = now
        };

        _context.SyncLogs.Add(syncLog);
        await _context.SaveChangesAsync(ct);

        return syncLog;
    }

    /// <inheritdoc/>
    public async Task UpdateSyncLogAsync(
        Guid syncLogId,
        SyncStatus status,
        int recordsSynced,
        int conflictsDetected,
        int conflictsResolved,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        var syncLog = await _context.SyncLogs.FindAsync(new object[] { syncLogId }, ct);
        if (syncLog == null)
            throw new NotFoundException(ErrorMessages.SyncLogNotFound);

        syncLog.Status = status;
        syncLog.RecordsSynced = recordsSynced;
        syncLog.ConflictsDetected = conflictsDetected;
        syncLog.ConflictsResolved = conflictsResolved;
        syncLog.ErrorMessage = errorMessage;
        syncLog.LastSyncTimestamp = _timeProvider.GetUtcNow().UtcDateTime;

        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<SyncLog?> GetLastSuccessfulSyncAsync(
        string deviceId,
        string userId,
        CancellationToken ct = default)
    {
        return await _context.SyncLogs
            .Where(s => s.DeviceId == deviceId && s.UserId == userId && s.Status == SyncStatus.Synced)
            .OrderByDescending(s => s.LastSyncTimestamp)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<SyncLog>> GetSyncHistoryAsync(
        string deviceId,
        string userId,
        int limit = 10,
        CancellationToken ct = default)
    {
        return await _context.SyncLogs
            .Where(s => s.DeviceId == deviceId && s.UserId == userId)
            .OrderByDescending(s => s.CreatedDate)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<(int total, int failed)> GetSyncStatsAsync(
        string deviceId,
        string userId,
        CancellationToken ct = default)
    {
        var total = await _context.SyncLogs
            .CountAsync(s => s.DeviceId == deviceId && s.UserId == userId, ct);

        var failed = await _context.SyncLogs
            .CountAsync(s => s.DeviceId == deviceId && s.UserId == userId && s.Status == SyncStatus.Failed, ct);

        return (total, failed);
    }
}

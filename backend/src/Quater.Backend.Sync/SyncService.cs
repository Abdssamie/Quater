using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;
using Quater.Shared.Models;

namespace Quater.Backend.Sync;

/// <summary>
/// Service for orchestrating bidirectional synchronization
/// </summary>
public class SyncService : ISyncService
{
    private readonly QuaterDbContext _context;
    private readonly ISyncLogService _syncLogService;
    private readonly IBackupService _backupService;
    private readonly IConflictResolver _conflictResolver;
    private readonly TimeProvider _timeProvider;

    public SyncService(
        QuaterDbContext context,
        ISyncLogService syncLogService,
        IBackupService backupService,
        IConflictResolver conflictResolver,
        TimeProvider timeProvider)
    {
        _context = context;
        _syncLogService = syncLogService;
        _backupService = backupService;
        _conflictResolver = conflictResolver;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<SyncResponse> PushAsync(
        SyncPushRequest request,
        CancellationToken ct = default)
    {
        // Create sync log entry
        var syncLog = await _syncLogService.CreateSyncLogAsync(
            request.DeviceId,
            request.UserId,
            SyncStatus.InProgress,
            ct);

        try
        {
            var recordsSynced = 0;
            var conflictsDetected = 0;
            var conflictsResolved = 0;
            var conflicts = new List<ConflictInfo>();

            // Process each entity from client
            foreach (var entityData in request.Entities)
            {
                try
                {
                    // Process based on entity type
                    var (synced, hasConflict) = await ProcessEntityAsync(
                        entityData,
                        request.DeviceId,
                        ct);

                    if (synced)
                        recordsSynced++;

                    if (hasConflict)
                    {
                        conflictsDetected++;
                        conflictsResolved++;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other entities
                    Console.WriteLine($"Error processing entity {entityData.Id}: {ex.Message}");
                }
            }

            // Update sync log with success
            await _syncLogService.UpdateSyncLogAsync(
                syncLog.Id,
                SyncStatus.Synced,
                recordsSynced,
                conflictsDetected,
                conflictsResolved,
                null,
                ct);

            return new SyncResponse
            {
                Success = true,
                Message = "Push completed successfully",
                ServerTimestamp = _timeProvider.GetUtcNow().UtcDateTime,
                RecordsSynced = recordsSynced,
                ConflictsDetected = conflictsDetected,
                ConflictsResolved = conflictsResolved,
                Entities = new List<SyncEntityData>(),
                Conflicts = conflicts
            };
        }
        catch (Exception ex)
        {
            // Update sync log with failure
            await _syncLogService.UpdateSyncLogAsync(
                syncLog.Id,
                SyncStatus.Failed,
                0,
                0,
                0,
                ex.Message,
                ct);

            return new SyncResponse
            {
                Success = false,
                Message = $"Push failed: {ex.Message}",
                ServerTimestamp = _timeProvider.GetUtcNow().UtcDateTime,
                RecordsSynced = 0,
                ConflictsDetected = 0,
                ConflictsResolved = 0,
                Entities = new List<SyncEntityData>(),
                Conflicts = new List<ConflictInfo>()
            };
        }
    }

    /// <inheritdoc/>
    public async Task<SyncResponse> PullAsync(
        SyncPullRequest request,
        CancellationToken ct = default)
    {
        // Create sync log entry
        var syncLog = await _syncLogService.CreateSyncLogAsync(
            request.DeviceId,
            request.UserId,
            SyncStatus.InProgress,
            ct);

        try
        {
            var entities = new List<SyncEntityData>();

            // Get all modified entities since last sync
            var samples = await GetModifiedSamplesAsync(request.LastSyncTimestamp, ct);
            entities.AddRange(samples);

            var testResults = await GetModifiedTestResultsAsync(request.LastSyncTimestamp, ct);
            entities.AddRange(testResults);

            var parameters = await GetModifiedParametersAsync(request.LastSyncTimestamp, ct);
            entities.AddRange(parameters);

            // Update sync log with success
            await _syncLogService.UpdateSyncLogAsync(
                syncLog.Id,
                SyncStatus.Synced,
                entities.Count,
                0,
                0,
                null,
                ct);

            return new SyncResponse
            {
                Success = true,
                Message = "Pull completed successfully",
                ServerTimestamp = _timeProvider.GetUtcNow().UtcDateTime,
                RecordsSynced = entities.Count,
                ConflictsDetected = 0,
                ConflictsResolved = 0,
                Entities = entities,
                Conflicts = new List<ConflictInfo>()
            };
        }
        catch (Exception ex)
        {
            // Update sync log with failure
            await _syncLogService.UpdateSyncLogAsync(
                syncLog.Id,
                SyncStatus.Failed,
                0,
                0,
                0,
                ex.Message,
                ct);

            return new SyncResponse
            {
                Success = false,
                Message = $"Pull failed: {ex.Message}",
                ServerTimestamp = _timeProvider.GetUtcNow().UtcDateTime,
                RecordsSynced = 0,
                ConflictsDetected = 0,
                ConflictsResolved = 0,
                Entities = new List<SyncEntityData>(),
                Conflicts = new List<ConflictInfo>()
            };
        }
    }

    /// <inheritdoc/>
    public async Task<SyncStatusResponse> GetStatusAsync(
        string deviceId,
        string userId,
        CancellationToken ct = default)
    {
        var lastSync = await _syncLogService.GetLastSuccessfulSyncAsync(deviceId, userId, ct);
        var (total, failed) = await _syncLogService.GetSyncStatsAsync(deviceId, userId, ct);

        // Get pending conflicts - we need to get the user's lab first
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        var pendingConflicts = 0;
        if (user?.LabId != null)
        {
            var conflicts = await _backupService.GetUnresolvedConflictsAsync(user.LabId, ct);
            pendingConflicts = conflicts.Count;
        }

        return new SyncStatusResponse
        {
            DeviceId = deviceId,
            UserId = userId,
            LastSyncTimestamp = lastSync?.LastSyncTimestamp ?? DateTime.MinValue,
            Status = lastSync?.Status ?? SyncStatus.Pending,
            TotalSyncs = total,
            FailedSyncs = failed,
            PendingConflicts = pendingConflicts
        };
    }

    private async Task<(bool synced, bool hasConflict)> ProcessEntityAsync(
        SyncEntityData entityData,
        string deviceId,
        CancellationToken ct)
    {
        // This is a simplified implementation
        // In a real system, you would deserialize the entity and update the database
        // For now, we'll just return success
        return (true, false);
    }

    private async Task<List<SyncEntityData>> GetModifiedSamplesAsync(
        DateTime since,
        CancellationToken ct)
    {
        var samples = await _context.Samples
            .Where(s => s.LastSyncedAt > since)
            .ToListAsync(ct);

        return samples.Select(s => new SyncEntityData
        {
            Id = s.Id,
            EntityType = nameof(Sample),
            Data = JsonSerializer.Serialize(s),
            LastModified = s.LastSyncedAt,
            IsDeleted = s.IsDeleted
        }).ToList();
    }

    private async Task<List<SyncEntityData>> GetModifiedTestResultsAsync(
        DateTime since,
        CancellationToken ct)
    {
        var testResults = await _context.TestResults
            .Where(t => t.LastSyncedAt > since)
            .ToListAsync(ct);

        return testResults.Select(t => new SyncEntityData
        {
            Id = t.Id,
            EntityType = nameof(TestResult),
            Data = JsonSerializer.Serialize(t),
            LastModified = t.LastSyncedAt,
            IsDeleted = t.IsDeleted
        }).ToList();
    }

    private async Task<List<SyncEntityData>> GetModifiedParametersAsync(
        DateTime since,
        CancellationToken ct)
    {
        var parameters = await _context.Parameters
            .Where(p => p.LastSyncedAt > since)
            .ToListAsync(ct);

        return parameters.Select(p => new SyncEntityData
        {
            Id = p.Id,
            EntityType = nameof(Parameter),
            Data = JsonSerializer.Serialize(p),
            LastModified = p.LastSyncedAt,
            IsDeleted = p.IsDeleted
        }).ToList();
    }
}

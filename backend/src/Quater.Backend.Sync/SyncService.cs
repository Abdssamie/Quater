using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        QuaterDbContext context,
        TimeProvider timeProvider,
        ILogger<SyncService> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SyncResponse> PushAsync(
        SyncPushRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting push sync for device {DeviceId} user {UserId}", request.DeviceId, request.UserId);

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
                        // Simplified logging for conflicts
                        _logger.LogWarning("Conflict detected for entity {EntityId} type {EntityType}", entityData.Id, entityData.EntityType);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other entities
                    _logger.LogError(ex, "Error processing entity {EntityId}: {Message}", entityData.Id, ex.Message);
                }
            }

            _logger.LogInformation("Push sync completed for device {DeviceId}. Synced: {Synced}, Conflicts: {Conflicts}", request.DeviceId, recordsSynced, conflictsDetected);

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
            _logger.LogError(ex, "Push sync failed for device {DeviceId}", request.DeviceId);

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
        _logger.LogInformation("Starting pull sync for device {DeviceId} user {UserId}", request.DeviceId, request.UserId);

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

            _logger.LogInformation("Pull sync completed for device {DeviceId}. Records: {Count}", request.DeviceId, entities.Count);

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
            _logger.LogError(ex, "Pull sync failed for device {DeviceId}", request.DeviceId);

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
        // With SyncLog removed, we return a simplified status
        await Task.CompletedTask;

        return new SyncStatusResponse
        {
            DeviceId = deviceId,
            UserId = userId,
            LastSyncTimestamp = _timeProvider.GetUtcNow().UtcDateTime, // Best effort
            Status = SyncStatus.Synced,
            TotalSyncs = 0, // No longer tracked
            FailedSyncs = 0, // No longer tracked
            PendingConflicts = 0 // No longer tracked
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
        await Task.Yield();
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

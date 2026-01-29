using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for SyncLog entity.
/// </summary>
public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> entity)
    {
        entity.ToTable("SyncLogs");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.DeviceId)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.LastSyncTimestamp)
            .IsRequired();

        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20);

        entity.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        entity.Property(e => e.RecordsSynced)
            .IsRequired()
            .HasDefaultValue(0);

        entity.Property(e => e.ConflictsDetected)
            .IsRequired()
            .HasDefaultValue(0);

        entity.Property(e => e.ConflictsResolved)
            .IsRequired()
            .HasDefaultValue(0);

        entity.Property(e => e.CreatedDate)
            .IsRequired();

        // Indexes
        entity.HasIndex(e => e.DeviceId)
            .HasDatabaseName("IX_SyncLogs_DeviceId");

        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_SyncLogs_UserId");

        entity.HasIndex(e => e.LastSyncTimestamp)
            .HasDatabaseName("IX_SyncLogs_LastSyncTimestamp");

        // Composite index for client queries (DeviceId, LastSyncTimestamp)
        entity.HasIndex(e => new { e.DeviceId, e.LastSyncTimestamp })
            .HasDatabaseName("IX_SyncLogs_DeviceId_LastSyncTimestamp");
    }
}

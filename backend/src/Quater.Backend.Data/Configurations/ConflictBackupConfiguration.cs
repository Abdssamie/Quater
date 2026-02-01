using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for ConflictBackup entity.
/// </summary>
public class ConflictBackupConfiguration : IEntityTypeConfiguration<ConflictBackup>
{
    public void Configure(EntityTypeBuilder<ConflictBackup> entity)
    {
        entity.ToTable("ConflictBackups");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.EntityId)
            .IsRequired();

        entity.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.ServerVersion)
            .IsRequired();

        entity.Property(e => e.ClientVersion)
            .IsRequired();

        // Enum stored as string for compatibility with SQLite desktop app
        // ConflictResolutionStrategy.LastWriteWins -> "LastWriteWins"
        entity.Property(e => e.ResolutionStrategy)
            .IsRequired()
            .HasConversion<string>();

        entity.Property(e => e.ConflictDetectedAt)
            .IsRequired();

        entity.Property(e => e.ResolvedAt);

        entity.Property(e => e.ResolvedBy)
            .HasMaxLength(100);

        entity.Property(e => e.ResolutionNotes)
            .HasMaxLength(1000);

        entity.Property(e => e.DeviceId)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.LabId)
            .IsRequired();

        // IAuditable properties
        entity.Property(e => e.CreatedAt)
            .IsRequired();

        entity.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.UpdatedAt);

        entity.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // Indexes
        entity.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_ConflictBackups_EntityType_EntityId");

        entity.HasIndex(e => e.ConflictDetectedAt)
            .HasDatabaseName("IX_ConflictBackups_ConflictDetectedAt");

        entity.HasIndex(e => e.ResolvedAt)
            .HasDatabaseName("IX_ConflictBackups_ResolvedAt");

        entity.HasIndex(e => e.LabId)
            .HasDatabaseName("IX_ConflictBackups_LabId");

        entity.HasIndex(e => e.DeviceId)
            .HasDatabaseName("IX_ConflictBackups_DeviceId");

        // Relationships
        entity.HasOne(e => e.Lab)
            .WithMany()
            .HasForeignKey(e => e.LabId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for AuditLogArchive entity.
/// </summary>
public class AuditLogArchiveConfiguration : IEntityTypeConfiguration<AuditLogArchive>
{
    public void Configure(EntityTypeBuilder<AuditLogArchive> entity)
    {
        entity.ToTable("AuditLogArchive");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(e => e.EntityId)
            .IsRequired();

        entity.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(20);

        entity.Property(e => e.OldValue);

        entity.Property(e => e.NewValue);

        entity.Property(e => e.Timestamp)
            .IsRequired();

        entity.Property(e => e.IpAddress)
            .HasMaxLength(45);

        entity.Property(e => e.ArchivedDate)
            .IsRequired();

        // Indexes
        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_AuditLogArchive_UserId");

        entity.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_AuditLogArchive_EntityType_EntityId");

        entity.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_AuditLogArchive_Timestamp");

        entity.HasIndex(e => e.ArchivedDate)
            .HasDatabaseName("IX_AuditLogArchive_ArchivedDate");
    }
}

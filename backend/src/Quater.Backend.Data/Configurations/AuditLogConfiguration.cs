using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for AuditLog entity.
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.ToTable("AuditLogs");

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

        // No MaxLength - allows unlimited text storage (nvarchar(max) or text)
        entity.Property(e => e.OldValue);

        entity.Property(e => e.NewValue);

        entity.Property(e => e.Timestamp)
            .IsRequired();

        entity.Property(e => e.IpAddress)
            .HasMaxLength(45);

        entity.Property(e => e.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        entity.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

        entity.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        entity.HasIndex(e => e.IsArchived)
            .HasDatabaseName("IX_AuditLogs_IsArchived");

        // Configure relationship to User with FK constraint for data integrity
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for Parameter entity.
/// </summary>
public class ParameterConfiguration : IEntityTypeConfiguration<Parameter>
{
    public void Configure(EntityTypeBuilder<Parameter> entity)
    {
        entity.ToTable("Parameters");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Unit)
            .IsRequired()
            .HasMaxLength(20);

        entity.Property(e => e.Description)
            .HasMaxLength(500);

        entity.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        entity.Property(e => e.CreatedDate)
            .IsRequired();

        entity.Property(e => e.LastModified)
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

        // ISoftDelete properties
        entity.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(e => e.DeletedAt);

        entity.Property(e => e.DeletedBy)
            .HasMaxLength(100);

        // ISyncable properties
        entity.Property(e => e.LastSyncedAt)
            .IsRequired();

        entity.Property(e => e.SyncVersion)
            .HasMaxLength(50);

        // IConcurrent properties
        entity.Property(e => e.RowVersion)
            .IsRowVersion();

        // Indexes
        entity.HasIndex(e => e.Name)
            .IsUnique()
            .HasDatabaseName("IX_Parameters_Name");

        entity.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Parameters_IsActive");

        entity.HasIndex(e => e.IsDeleted)
            .HasDatabaseName("IX_Parameters_IsDeleted");
    }
}

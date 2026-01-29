using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for Lab entity.
/// </summary>
public class LabConfiguration : IEntityTypeConfiguration<Lab>
{
    public void Configure(EntityTypeBuilder<Lab> entity)
    {
        entity.ToTable("Labs");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(e => e.Location)
            .HasMaxLength(500);

        entity.Property(e => e.ContactInfo)
            .HasMaxLength(500);

        entity.Property(e => e.CreatedDate)
            .IsRequired();

        entity.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

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

        // IConcurrent properties
        entity.Property(e => e.RowVersion)
            .IsRowVersion();

        // Indexes
        entity.HasIndex(e => e.Name)
            .HasDatabaseName("IX_Labs_Name");

        entity.HasIndex(e => e.IsDeleted)
            .HasDatabaseName("IX_Labs_IsDeleted");

        // Relationships
        entity.HasMany(e => e.Users)
            .WithOne(e => e.Lab)
            .HasForeignKey(e => e.LabId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.Samples)
            .WithOne(e => e.Lab)
            .HasForeignKey(e => e.LabId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

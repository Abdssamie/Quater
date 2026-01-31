using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for Sample entity.
/// </summary>
public class SampleConfiguration : IEntityTypeConfiguration<Sample>
{
    public void Configure(EntityTypeBuilder<Sample> entity)
    {
        entity.ToTable("Samples");

        entity.HasKey(e => e.Id);

        // Enum stored as string for compatibility with SQLite desktop app
        // SampleType.DrinkingWater -> "DrinkingWater"
        entity.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>();

        entity.Property(e => e.LocationLatitude)
            .IsRequired();

        entity.Property(e => e.LocationLongitude)
            .IsRequired();

        entity.Property(e => e.LocationDescription)
            .HasMaxLength(200);

        entity.Property(e => e.LocationHierarchy)
            .HasMaxLength(500);

        entity.Property(e => e.CollectionDate)
            .IsRequired();

        entity.Property(e => e.CollectorName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Notes)
            .HasMaxLength(1000);

        // Enum stored as string for compatibility with SQLite desktop app
        // SampleStatus.Pending -> "Pending"
        entity.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();

        entity.Property(e => e.Version)
            .IsRequired()
            .IsConcurrencyToken();

        entity.Property(e => e.LastModified)
            .IsRequired()
            .IsConcurrencyToken();

        entity.Property(e => e.LastModifiedBy)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(e => e.IsSynced)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(e => e.LabId)
            .IsRequired();

        entity.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.CreatedDate)
            .IsRequired();

        // IAuditable properties
        entity.Property(e => e.CreatedAt)
            .IsRequired();

        entity.Property(e => e.UpdatedAt);

        entity.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // ISoftDelete properties
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
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("'\\x0000000000000001'::bytea");

        // Indexes
        entity.HasIndex(e => e.LastModified)
            .HasDatabaseName("IX_Samples_LastModified");

        entity.HasIndex(e => e.IsSynced)
            .HasDatabaseName("IX_Samples_IsSynced");

        entity.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Samples_Status");

        entity.HasIndex(e => e.LabId)
            .HasDatabaseName("IX_Samples_LabId");

        entity.HasIndex(e => e.CollectionDate)
            .HasDatabaseName("IX_Samples_CollectionDate");

        entity.HasIndex(e => e.IsDeleted)
            .HasDatabaseName("IX_Samples_IsDeleted");

        // Composite index for sync queries
        entity.HasIndex(e => new { e.IsSynced, e.LastModified })
            .HasDatabaseName("IX_Samples_IsSynced_LastModified");

        // Composite index for lab queries (LabId, CollectionDate, Status)
        entity.HasIndex(e => new { e.LabId, e.CollectionDate, e.Status })
            .HasDatabaseName("IX_Samples_LabId_CollectionDate_Status");

        // Relationships
        entity.HasMany(e => e.TestResults)
            .WithOne(e => e.Sample)
            .HasForeignKey(e => e.SampleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

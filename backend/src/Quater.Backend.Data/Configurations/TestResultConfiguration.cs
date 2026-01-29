using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for TestResult entity.
/// </summary>
public class TestResultConfiguration : IEntityTypeConfiguration<TestResult>
{
    public void Configure(EntityTypeBuilder<TestResult> entity)
    {
        entity.ToTable("TestResults");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.SampleId)
            .IsRequired();

        entity.Property(e => e.ParameterName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Value)
            .IsRequired();

        entity.Property(e => e.Unit)
            .IsRequired()
            .HasMaxLength(20);

        entity.Property(e => e.TestDate)
            .IsRequired();

        entity.Property(e => e.TechnicianName)
            .IsRequired()
            .HasMaxLength(100);

        // Enum stored as string for compatibility with SQLite desktop app
        // TestMethod.Spectrophotometry -> "Spectrophotometry"
        entity.Property(e => e.TestMethod)
            .IsRequired()
            .HasConversion<string>();

        // Enum stored as string for compatibility with SQLite desktop app
        // ComplianceStatus.Pass -> "Pass"
        entity.Property(e => e.ComplianceStatus)
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
            .IsRowVersion();

        // Indexes
        entity.HasIndex(e => e.SampleId)
            .HasDatabaseName("IX_TestResults_SampleId");

        entity.HasIndex(e => e.LastModified)
            .HasDatabaseName("IX_TestResults_LastModified");

        entity.HasIndex(e => e.IsSynced)
            .HasDatabaseName("IX_TestResults_IsSynced");

        entity.HasIndex(e => e.ComplianceStatus)
            .HasDatabaseName("IX_TestResults_ComplianceStatus");

        entity.HasIndex(e => e.TestDate)
            .HasDatabaseName("IX_TestResults_TestDate");

        entity.HasIndex(e => e.IsDeleted)
            .HasDatabaseName("IX_TestResults_IsDeleted");

        // Composite index for sync queries
        entity.HasIndex(e => new { e.IsSynced, e.LastModified })
            .HasDatabaseName("IX_TestResults_IsSynced_LastModified");

        // Composite index for sample-parameter queries
        entity.HasIndex(e => new { e.SampleId, e.ParameterName })
            .HasDatabaseName("IX_TestResults_SampleId_ParameterName");
    }
}

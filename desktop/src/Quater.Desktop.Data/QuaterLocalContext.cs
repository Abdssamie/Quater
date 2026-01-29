using Microsoft.EntityFrameworkCore;
using Quater.Shared.Models;
using Quater.Shared.Enums;
using Quater.Shared.Infrastructure.Converters;

namespace Quater.Desktop.Data;

/// <summary>
/// Local SQLite database context for the Quater desktop application.
/// Supports offline data storage and synchronization with the backend.
/// </summary>
public class QuaterLocalContext : DbContext
{
    public QuaterLocalContext(DbContextOptions<QuaterLocalContext> options)
        : base(options)
    {
    }

    // Entity DbSets
    public DbSet<Sample> Samples { get; set; } = null!;
    public DbSet<TestResult> TestResults { get; set; } = null!;
    public DbSet<Parameter> Parameters { get; set; } = null!;
    public DbSet<SyncLog> SyncLogs { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Configure SQLite-specific settings
        if (optionsBuilder.IsConfigured)
        {
            // Enable Write-Ahead Logging (WAL) mode for better concurrency
            optionsBuilder.UseSqlite(sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Sample entity
        ConfigureSample(modelBuilder);

        // Configure TestResult entity
        ConfigureTestResult(modelBuilder);

        // Configure Parameter entity
        ConfigureParameter(modelBuilder);

        // Configure SyncLog entity
        ConfigureSyncLog(modelBuilder);
    }

    private void ConfigureSample(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sample>(entity =>
        {
            entity.ToTable("Samples");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion(new SampleTypeConverter());

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

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion(new SampleStatusConverter());

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

            // Composite index for sync queries
            entity.HasIndex(e => new { e.IsSynced, e.LastModified })
                .HasDatabaseName("IX_Samples_IsSynced_LastModified");

            // Relationships
            entity.HasMany(e => e.TestResults)
                .WithOne(e => e.Sample)
                .HasForeignKey(e => e.SampleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureTestResult(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestResult>(entity =>
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

            entity.Property(e => e.TestMethod)
                .IsRequired()
                .HasConversion(new TestMethodConverter());

            entity.Property(e => e.ComplianceStatus)
                .IsRequired()
                .HasConversion(new ComplianceStatusConverter());

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

            // Composite index for sync queries
            entity.HasIndex(e => new { e.IsSynced, e.LastModified })
                .HasDatabaseName("IX_TestResults_IsSynced_LastModified");
        });
    }

    private void ConfigureParameter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Parameter>(entity =>
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

            // Indexes
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_Parameters_Name");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Parameters_IsActive");
        });
    }

    private void ConfigureSyncLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncLog>(entity =>
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
        });
    }

    /// <summary>
    /// Configures the database connection with WAL mode enabled.
    /// Call this method after creating the database.
    /// </summary>
    public void EnableWalMode()
    {
        Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }
}

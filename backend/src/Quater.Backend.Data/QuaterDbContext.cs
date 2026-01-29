using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Quater.Shared.Models;

namespace Quater.Backend.Data;

/// <summary>
/// Database context for the Quater water quality lab management system.
/// Inherits from IdentityDbContext to support ASP.NET Core Identity.
/// </summary>
public class QuaterDbContext : IdentityDbContext<User>
{
    public QuaterDbContext(DbContextOptions<QuaterDbContext> options)
        : base(options)
    {
    }

    // Entity DbSets
    public DbSet<Lab> Labs { get; set; } = null!;
    public DbSet<Sample> Samples { get; set; } = null!;
    public DbSet<TestResult> TestResults { get; set; } = null!;
    public DbSet<Parameter> Parameters { get; set; } = null!;
    public DbSet<SyncLog> SyncLogs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<AuditLogArchive> AuditLogArchives { get; set; } = null!;
    public DbSet<ConflictBackup> ConflictBackups { get; set; } = null!;

    /// <summary>
    /// Configures the entity models and their relationships for the database.
    /// </summary>
    /// <remarks>
    /// This DbContext uses the Core Domain Pattern with shared models from Quater.Shared.
    /// 
    /// Entity configurations are defined in separate IEntityTypeConfiguration classes
    /// in the Configurations folder for better organization and maintainability.
    /// 
    /// Enum Handling Strategy (PostgreSQL):
    /// - All enum properties use HasConversion&lt;string&gt;() for string storage
    /// - This ensures compatibility with the desktop SQLite database
    /// - Enums are stored as their string representation (e.g., "Pending", "Admin")
    /// 
    /// See docs/architecture/core-domain-pattern.md for more information.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuaterDbContext).Assembly);
    }

    private void ConfigureLab(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lab>(entity =>
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

            // Indexes
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_Labs_Name");

            // Relationships
            entity.HasMany(e => e.Users)
                .WithOne(e => e.Lab)
                .HasForeignKey(e => e.LabId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Samples)
                .WithOne(e => e.Lab)
                .HasForeignKey(e => e.LabId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            // Enum stored as string for compatibility with SQLite desktop app
            // UserRole.Admin -> "Admin", UserRole.Technician -> "Technician"
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.LabId)
                .IsRequired();

            entity.Property(e => e.CreatedDate)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            entity.HasIndex(e => e.LabId)
                .HasDatabaseName("IX_Users_LabId");

            entity.HasIndex(e => e.Role)
                .HasDatabaseName("IX_Users_Role");

            // Relationships
            entity.HasMany(e => e.AuditLogs)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.AuditLogArchives)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.SyncLogs)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSample(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sample>(entity =>
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

    private void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
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

            entity.Property(e => e.OldValue)
                .HasMaxLength(4000);

            entity.Property(e => e.NewValue)
                .HasMaxLength(4000);

            entity.Property(e => e.ConflictResolutionNotes)
                .HasMaxLength(1000);

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
        });
    }

    private void ConfigureAuditLogArchive(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogArchive>(entity =>
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

            entity.Property(e => e.OldValue)
                .HasMaxLength(4000);

            entity.Property(e => e.NewValue)
                .HasMaxLength(4000);

            entity.Property(e => e.ConflictResolutionNotes)
                .HasMaxLength(1000);

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
        });
    }

    private void ConfigureConflictBackup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConflictBackup>(entity =>
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

            entity.Property(e => e.CreatedDate)
                .IsRequired();

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
        });
    }
}

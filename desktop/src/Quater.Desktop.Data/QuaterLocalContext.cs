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

    public DbSet<Lab> Labs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<AuditLogArchive> AuditLogArchives { get; set; } = null!;

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

    /// <summary>
    /// Configures the entity models and their relationships for the SQLite database.
    /// </summary>
    /// <remarks>
    /// This DbContext uses the Core Domain Pattern with shared models from Quater.Shared.
    /// 
    /// Enum Handling Strategy (SQLite):
    /// - All enum properties use explicit Value Converters from Quater.Shared.Infrastructure.Converters
    /// - SQLite requires explicit converters because it doesn't natively support enums
    /// - Enums are stored as their string representation (e.g., "Pending", "Admin")
    /// - This ensures compatibility with the backend PostgreSQL database during sync
    /// 
    /// Example enum configuration:
    /// <code>
    /// entity.Property(e => e.Status)
    ///     .HasConversion(new SampleStatusConverter());  // Stores SampleStatus.Pending as "Pending"
    /// </code>
    /// 
    /// Value Converters are defined in shared/Infrastructure/Converters/ and handle
    /// bidirectional conversion between enum types and string storage.
    /// 
    /// See docs/architecture/core-domain-pattern.md for more information.
    /// See docs/guides/value-converter-usage.md for usage examples.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Sample entity
        ConfigureSample(modelBuilder);

        // Configure TestResult entity
        ConfigureTestResult(modelBuilder);

        // Configure Parameter entity
        ConfigureParameter(modelBuilder);



        // Configure Lab entity
        ConfigureLab(modelBuilder);

        // Configure User entity
        ConfigureUser(modelBuilder);

        // Configure AuditLog entity
        ConfigureAuditLog(modelBuilder);

        // Configure AuditLogArchive entity
        ConfigureAuditLogArchive(modelBuilder);
    }

    private void ConfigureSample(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sample>(entity =>
        {
            entity.ToTable("Samples");

            entity.HasKey(e => e.Id);

            // Value Converter handles enum-to-string conversion for SQLite
            // SampleType.DrinkingWater -> "DrinkingWater"
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

            // Value Converter handles enum-to-string conversion for SQLite
            // SampleStatus.Pending -> "Pending"
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

            // Value Converter handles enum-to-string conversion for SQLite
            // TestMethod.Spectrophotometry -> "Spectrophotometry"
            entity.Property(e => e.TestMethod)
                .IsRequired()
                .HasConversion(new TestMethodConverter());

            // Value Converter handles enum-to-string conversion for SQLite
            // ComplianceStatus.Pass -> "Pass"
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



    private void ConfigureLab(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lab>(entity =>
        {
            entity.ToTable("Lab");

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
        });
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.HasKey(e => e.Id);

            // Value Converter handles enum-to-string conversion for SQLite
            // UserRole.Admin -> "Admin", UserRole.Technician -> "Technician"
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion(new UserRoleConverter());

            entity.Property(e => e.LabId)
                .IsRequired();

            entity.Property(e => e.CreatedDate)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Relationships
            entity.HasOne(e => e.Lab)
                .WithMany(l => l.Users)
                .HasForeignKey(e => e.LabId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");

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



            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.IsArchived)
                .IsRequired()
                .HasDefaultValue(false);

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.ArchivedDate)
                .IsRequired();

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogArchives)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Configures the database connection with WAL mode enabled.
    /// Call this method after creating the database.
    /// </summary>
    /// <remarks>
    /// Write-Ahead Logging (WAL) mode improves concurrency by allowing
    /// readers to access the database while a write is in progress.
    /// This is particularly useful for desktop applications with background sync.
    /// </remarks>
    public void EnableWalMode()
    {
        Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }
}

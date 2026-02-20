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
    public DbSet<UserInvitation> UserInvitations { get; set; } = null!;
    public DbSet<UserLab> UserLabs { get; set; } = null!;
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

        // Configure UserInvitation entity
        ConfigureUserInvitation(modelBuilder);

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

            // Configure Location value object as owned entity
            entity.OwnsOne(e => e.Location, location =>
            {
                location.Property(l => l.Latitude)
                    .HasColumnName("LocationLatitude")
                    .IsRequired();

                location.Property(l => l.Longitude)
                    .HasColumnName("LocationLongitude")
                    .IsRequired();

                location.Property(l => l.Description)
                    .HasColumnName("LocationDescription")
                    .HasMaxLength(200);

                location.Property(l => l.Hierarchy)
                    .HasColumnName("LocationHierarchy")
                    .HasMaxLength(500);
            });

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

            // RowVersion for concurrency (maps to IConcurrent.RowVersion)
            entity.Property(e => e.RowVersion)
                .IsRequired()
                .IsRowVersion()
                .IsConcurrencyToken();

            // Audit properties (IAuditable)
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .IsRequired();

            entity.Property(e => e.UpdatedAt);

            entity.Property(e => e.UpdatedBy);

            // Soft delete (ISoftDelete)
            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.DeletedAt);

            entity.Property(e => e.DeletedBy);

            entity.Property(e => e.LabId)
                .IsRequired();

            // Shadow property for client-side sync tracking (not on shared model)
            entity.Property<bool>("IsSynced")
                .IsRequired()
                .HasDefaultValue(false);

            // Shadow property for tracking last sync modification time
            entity.Property<DateTime?>("LastSyncedAt");

            // Indexes
            entity.HasIndex("IsSynced")
                .HasDatabaseName("IX_Samples_IsSynced");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Samples_Status");

            entity.HasIndex(e => e.LabId)
                .HasDatabaseName("IX_Samples_LabId");

            entity.HasIndex(e => e.CollectionDate)
                .HasDatabaseName("IX_Samples_CollectionDate");

            // Composite index for sync queries
            entity.HasIndex("IsSynced", nameof(Sample.UpdatedAt))
                .HasDatabaseName("IX_Samples_IsSynced_UpdatedAt");

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

            // Configure Measurement value object as owned entity
            entity.OwnsOne(e => e.Measurement, measurement =>
            {
                measurement.Property(m => m.ParameterId)
                    .HasColumnName("ParameterId")
                    .IsRequired();

                measurement.Property(m => m.Value)
                    .HasColumnName("Value")
                    .IsRequired();

                measurement.Property(m => m.Unit)
                    .HasColumnName("Unit")
                    .IsRequired()
                    .HasMaxLength(20);
            });

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion(new TestResultStatusConverter());

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

            entity.Property(e => e.VoidedTestResultId);

            entity.Property(e => e.ReplacedByTestResultId);

            entity.Property(e => e.IsVoided)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.VoidReason)
                .HasMaxLength(500);

            // RowVersion for concurrency (maps to IConcurrent.RowVersion)
            entity.Property(e => e.RowVersion)
                .IsRequired()
                .IsRowVersion()
                .IsConcurrencyToken();

            // Audit properties (IAuditable)
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .IsRequired();

            entity.Property(e => e.UpdatedAt);

            entity.Property(e => e.UpdatedBy);

            // Soft delete (ISoftDelete)
            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.DeletedAt);

            entity.Property(e => e.DeletedBy);

            // Shadow property for client-side sync tracking (not on shared model)
            entity.Property<bool>("IsSynced")
                .IsRequired()
                .HasDefaultValue(false);

            // Shadow property for tracking last sync modification time
            entity.Property<DateTime?>("LastSyncedAt");

            // Indexes
            entity.HasIndex(e => e.SampleId)
                .HasDatabaseName("IX_TestResults_SampleId");

            entity.HasIndex("IsSynced")
                .HasDatabaseName("IX_TestResults_IsSynced");

            entity.HasIndex(e => e.ComplianceStatus)
                .HasDatabaseName("IX_TestResults_ComplianceStatus");

            entity.HasIndex(e => e.TestDate)
                .HasDatabaseName("IX_TestResults_TestDate");

            // Composite index for sync queries
            entity.HasIndex("IsSynced", nameof(TestResult.UpdatedAt))
                .HasDatabaseName("IX_TestResults_IsSynced_UpdatedAt");
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

            entity.Property(e => e.Threshold);

            entity.Property(e => e.MinValue);

            entity.Property(e => e.MaxValue);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // RowVersion for concurrency (maps to IConcurrent.RowVersion)
            entity.Property(e => e.RowVersion)
                .IsRequired()
                .IsRowVersion()
                .IsConcurrencyToken();

            // Audit properties (IAuditable)
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .IsRequired();

            entity.Property(e => e.UpdatedAt);

            entity.Property(e => e.UpdatedBy);

            // Soft delete (ISoftDelete)
            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.DeletedAt);

            entity.Property(e => e.DeletedBy);

            // Shadow property for client-side sync tracking (not on shared model)
            entity.Property<bool>("IsSynced")
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_Parameters_Name");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Parameters_IsActive");

            entity.HasIndex("IsSynced")
                .HasDatabaseName("IX_Parameters_IsSynced");
        });
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

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // RowVersion for concurrency (maps to IConcurrent.RowVersion)
            entity.Property(e => e.RowVersion)
                .IsRequired()
                .IsRowVersion()
                .IsConcurrencyToken();

            // Audit properties (IAuditable)
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .IsRequired();

            entity.Property(e => e.UpdatedAt);

            entity.Property(e => e.UpdatedBy);

            // Soft delete (ISoftDelete)
            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.DeletedAt);

            entity.Property(e => e.DeletedBy);

            // Shadow property for client-side sync tracking (not on shared model)
            entity.Property<bool>("IsSynced")
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_Labs_Name");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Labs_IsActive");
        });
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(e => e.Id);

            // IdentityUser properties
            entity.Property(e => e.UserName)
                .HasMaxLength(256);

            entity.Property(e => e.NormalizedUserName)
                .HasMaxLength(256);

            entity.Property(e => e.Email)
                .HasMaxLength(256);

            entity.Property(e => e.NormalizedEmail)
                .HasMaxLength(256);

            entity.Property(e => e.EmailConfirmed)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.PasswordHash);

            entity.Property(e => e.SecurityStamp);

            entity.Property(e => e.ConcurrencyStamp);

            entity.Property(e => e.PhoneNumber);

            entity.Property(e => e.PhoneNumberConfirmed)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.TwoFactorEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.LockoutEnd);

            entity.Property(e => e.LockoutEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.AccessFailedCount)
                .IsRequired()
                .HasDefaultValue(0);

            // Custom User properties
            entity.Property(e => e.LastLogin);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // RowVersion for concurrency (maps to IConcurrent.RowVersion)
            entity.Property(e => e.RowVersion)
                .IsRequired()
                .IsRowVersion()
                .IsConcurrencyToken();

            // Shadow property for client-side sync tracking (not on shared model)
            entity.Property<bool>("IsSynced")
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes (IdentityUser indexes)
            entity.HasIndex(e => e.NormalizedUserName)
                .IsUnique()
                .HasDatabaseName("IX_Users_NormalizedUserName");

            entity.HasIndex(e => e.NormalizedEmail)
                .HasDatabaseName("IX_Users_NormalizedEmail");
        });

        // Configure UserLab join entity
        modelBuilder.Entity<UserLab>(entity =>
        {
            entity.ToTable("UserLabs");

            entity.HasKey(e => new { e.UserId, e.LabId });

            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion(new UserRoleConverter());

            entity.Property(e => e.AssignedAt)
                .IsRequired();

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserLabs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lab)
                .WithMany(l => l.UserLabs)
                .HasForeignKey(e => e.LabId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureUserInvitation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserInvitation>(entity =>
        {
            entity.ToTable("UserInvitations");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired();

            entity.Property(e => e.TokenHash)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.InvitedByUserId)
                .IsRequired();

            entity.Property(e => e.RowVersion)
                .IsRequired()
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .IsRequired();

            entity.Property(e => e.UpdatedAt);

            entity.Property(e => e.UpdatedBy);

            entity.HasIndex(e => e.TokenHash)
                .IsUnique();

            entity.HasIndex(e => e.Email);

            entity.HasIndex(e => e.Status);

            entity.HasIndex(e => e.ExpiresAt);

            entity.HasIndex(e => e.InvitedByUserId);

            entity.HasIndex(e => e.UserId)
                .IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(u => u.ReceivedInvitation)
                .HasForeignKey<UserInvitation>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedBy)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasConversion(new EntityTypeConverter());

            entity.Property(e => e.EntityId)
                .IsRequired();

            entity.Property(e => e.Action)
                .IsRequired()
                .HasConversion(new AuditActionConverter());

            entity.Property(e => e.OldValue)
                .HasMaxLength(4000);

            entity.Property(e => e.NewValue)
                .HasMaxLength(4000);

            entity.Property(e => e.IsTruncated)
                .IsRequired()
                .HasDefaultValue(false);

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
            entity.ToTable("AuditLogArchives");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasConversion(new EntityTypeConverter());

            entity.Property(e => e.EntityId)
                .IsRequired();

            entity.Property(e => e.Action)
                .IsRequired()
                .HasConversion(new AuditActionConverter());

            entity.Property(e => e.OldValue)
                .HasMaxLength(4000);

            entity.Property(e => e.NewValue)
                .HasMaxLength(4000);

            entity.Property(e => e.IsTruncated)
                .IsRequired()
                .HasDefaultValue(false);

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

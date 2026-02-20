using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Quater.Shared.Models;

namespace Quater.Backend.Data;

/// <summary>
/// Database context for the Quater water quality lab management system.
/// Inherits from IdentityDbContext to support ASP.NET Core Identity.
/// </summary>
public class QuaterDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
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
    public DbSet<UserLab> UserLabs { get; set; } = null!;
    public DbSet<UserInvitation> UserInvitations { get; set; } = null!;

    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<AuditLogArchive> AuditLogArchives { get; set; } = null!;


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
    /// Soft Delete and Audit Trail:
    /// - Soft delete is handled by SoftDeleteInterceptor (converts DELETE to UPDATE with IsDeleted=true)
    /// - Audit trail is handled by AuditTrailInterceptor (creates AuditLog entries for all changes)
    /// - Both interceptors are registered in Program.cs and added to the DbContext options
    /// 
    /// See docs/architecture/core-domain-pattern.md for more information.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserLab>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LabId });
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserLabs)
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Lab)
                .WithMany(l => l.UserLabs)
                .HasForeignKey(e => e.LabId);
            entity.Property(e => e.Role).HasConversion<string>();
            
            // Global query filter to match Lab's soft delete filter
            // This prevents loading UserLab records for soft-deleted Labs
            entity.HasQueryFilter(e => !e.Lab.IsDeleted);
        });

        modelBuilder.Entity<UserInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Unique index on TokenHash for secure token lookup
            entity.HasIndex(e => e.TokenHash).IsUnique();
            
            // Indexes for common queries
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt);
            
            // Store enum as string for compatibility with SQLite
            entity.Property(e => e.Status).HasConversion<string>();
            
            // One-to-one relationship: User receives one invitation
            entity.HasOne(e => e.User)
                .WithOne(u => u.ReceivedInvitation)
                .HasForeignKey<UserInvitation>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // One-to-many relationship: Admin can send many invitations
            entity.HasOne(e => e.InvitedBy)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuaterDbContext).Assembly);
    }
}

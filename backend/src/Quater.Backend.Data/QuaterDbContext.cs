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

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuaterDbContext).Assembly);
    }
}

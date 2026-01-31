using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Configurations;

/// <summary>
/// Entity configuration for User entity (extends IdentityUser).
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
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

        // IAuditable properties
        entity.Property(e => e.CreatedAt)
            .IsRequired();

        entity.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.UpdatedAt);

        entity.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // IConcurrent properties
        entity.Property(e => e.RowVersion)
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("'\\x0000000000000001'::bytea");

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
    }
}

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

        entity.Property(e => e.Id)
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();

        entity.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // IConcurrent properties
        entity.Property(e => e.RowVersion)
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("'\\x0000000000000001'::bytea");

        // Indexes
        entity.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // Relationships
        entity.HasMany(e => e.AuditLogs)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.AuditLogArchives)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

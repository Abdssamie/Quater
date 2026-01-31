using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for AuditAction enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// </summary>
/// <remarks>
/// This converter is part of the Core Domain Pattern implementation that allows
/// both backend (PostgreSQL) and desktop (SQLite) applications to use the same
/// domain models while storing enums as strings in the database.
/// 
/// Conversion examples:
/// - AuditAction.Create <-> "Create"
/// - AuditAction.Update <-> "Update"
/// - AuditAction.Delete <-> "Delete"
/// - AuditAction.Restore <-> "Restore"
/// - AuditAction.ConflictResolution <-> "ConflictResolution"
/// 
/// Usage in DbContext:
/// <code>
/// entity.Property(e => e.Action)
///     .HasConversion(new AuditActionConverter());
/// </code>
/// </remarks>
public class AuditActionConverter : ValueConverter<AuditAction, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditActionConverter"/> class.
    /// </summary>
    public AuditActionConverter() 
        : base(
            v => v.ToString(),              // Convert enum to string
            v => Enum.Parse<AuditAction>(v)) // Convert string to enum
    {
    }
}

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for UserRole enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// </summary>
/// <remarks>
/// This converter is part of the Core Domain Pattern implementation that allows
/// both backend (PostgreSQL) and desktop (SQLite) applications to use the same
/// domain models while storing enums as strings in the database.
/// 
/// Conversion examples:
/// - UserRole.Admin = "Admin"
/// - UserRole.Technician = "Technician"
/// - UserRole.Viewer = "Viewer"
/// 
/// Usage in DbContext:
/// <code>
/// entity.Property(e => e.Role)
///     .HasConversion(new UserRoleConverter());
/// </code>
/// </remarks>
public class UserRoleConverter : ValueConverter<UserRole, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRoleConverter"/> class.
    /// </summary>
    public UserRoleConverter()
        : base(
            v => v.ToString(),           // Convert enum to string
            v => Enum.Parse<UserRole>(v)) // Convert string to enum
    {
    }
}

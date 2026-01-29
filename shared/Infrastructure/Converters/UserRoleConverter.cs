using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for UserRole enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// Converts: UserRole.Admin <-> "Admin"
/// </summary>
public class UserRoleConverter : ValueConverter<UserRole, string>
{
    public UserRoleConverter() 
        : base(
            v => v.ToString(),
            v => (UserRole)Enum.Parse(typeof(UserRole), v))
    {
    }
}

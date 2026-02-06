using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// Converts UserRole enum to/from string for database storage
/// </summary>
public class UserRoleConverter : ValueConverter<UserRole, string>
{
    public UserRoleConverter()
        : base(
            v => v.ToString(),
            v => Enum.Parse<UserRole>(v))
    {
    }
}

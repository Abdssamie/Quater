using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// Converts EntityType enum to/from string for database storage
/// </summary>
public class EntityTypeConverter : ValueConverter<EntityType, string>
{
    public EntityTypeConverter()
        : base(
            v => v.ToString(),
            v => Enum.Parse<EntityType>(v))
    {
    }
}

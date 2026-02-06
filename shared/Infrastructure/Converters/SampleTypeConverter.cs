using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// Converts SampleType enum to/from string for database storage
/// </summary>
public class SampleTypeConverter : ValueConverter<SampleType, string>
{
    public SampleTypeConverter()
        : base(
            v => v.ToString(),
            v => Enum.Parse<SampleType>(v))
    {
    }
}

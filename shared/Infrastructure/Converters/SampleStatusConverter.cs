using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// Converts SampleStatus enum to/from string for database storage
/// </summary>
public class SampleStatusConverter : ValueConverter<SampleStatus, string>
{
    public SampleStatusConverter()
        : base(
            v => v.ToString(),
            v => Enum.Parse<SampleStatus>(v))
    {
    }
}

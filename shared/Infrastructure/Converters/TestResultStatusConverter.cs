using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// Converts TestResultStatus enum to/from string for database storage
/// </summary>
public class TestResultStatusConverter : ValueConverter<TestResultStatus, string>
{
    public TestResultStatusConverter()
        : base(
            v => v.ToString(),
            v => Enum.Parse<TestResultStatus>(v))
    {
    }
}

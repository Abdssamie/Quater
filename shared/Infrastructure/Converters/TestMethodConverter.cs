using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for TestMethod enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// </summary>
/// <remarks>
/// This converter is part of the Core Domain Pattern implementation that allows
/// both backend (PostgreSQL) and desktop (SQLite) applications to use the same
/// domain models while storing enums as strings in the database.
/// 
/// Conversion examples:
/// - TestMethod.Titration <-> "Titration"
/// - TestMethod.Spectrophotometry <-> "Spectrophotometry"
/// - TestMethod.Chromatography <-> "Chromatography"
/// 
/// Usage in DbContext:
/// <code>
/// entity.Property(e => e.TestMethod)
///     .HasConversion(new TestMethodConverter());
/// </code>
/// </remarks>
public class TestMethodConverter : ValueConverter<TestMethod, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestMethodConverter"/> class.
    /// </summary>
    public TestMethodConverter() 
        : base(
            v => v.ToString(),                                  // Convert enum to string
            v => (TestMethod)Enum.Parse(typeof(TestMethod), v)) // Convert string to enum
    {
    }
}

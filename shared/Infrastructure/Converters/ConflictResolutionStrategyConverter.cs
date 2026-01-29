using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for ConflictResolutionStrategy enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// </summary>
/// <remarks>
/// This converter is part of the Core Domain Pattern implementation that allows
/// both backend (PostgreSQL) and desktop (SQLite) applications to use the same
/// domain models while storing enums as strings in the database.
/// 
/// Conversion examples:
/// - ConflictResolutionStrategy.LastWriteWins <-> "LastWriteWins"
/// - ConflictResolutionStrategy.ServerWins <-> "ServerWins"
/// - ConflictResolutionStrategy.ClientWins <-> "ClientWins"
/// - ConflictResolutionStrategy.Manual <-> "Manual"
/// 
/// Usage in DbContext:
/// <code>
/// entity.Property(e => e.ResolutionStrategy)
///     .HasConversion(new ConflictResolutionStrategyConverter());
/// </code>
/// </remarks>
public class ConflictResolutionStrategyConverter : ValueConverter<ConflictResolutionStrategy, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictResolutionStrategyConverter"/> class.
    /// </summary>
    public ConflictResolutionStrategyConverter() 
        : base(
            v => v.ToString(),                                    // Convert enum to string
            v => (ConflictResolutionStrategy)Enum.Parse(typeof(ConflictResolutionStrategy), v)) // Convert string to enum
    {
    }
}

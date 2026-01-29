using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for SyncStatus enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// </summary>
/// <remarks>
/// This converter is part of the Core Domain Pattern implementation that allows
/// both backend (PostgreSQL) and desktop (SQLite) applications to use the same
/// domain models while storing enums as strings in the database.
/// 
/// Conversion examples:
/// - SyncStatus.Pending <-> "Pending"
/// - SyncStatus.InProgress <-> "InProgress"
/// - SyncStatus.Synced <-> "Synced"
/// - SyncStatus.Failed <-> "Failed"
/// - SyncStatus.Conflict <-> "Conflict"
/// 
/// Usage in DbContext:
/// <code>
/// entity.Property(e => e.SyncStatus)
///     .HasConversion(new SyncStatusConverter());
/// </code>
/// </remarks>
public class SyncStatusConverter : ValueConverter<SyncStatus, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncStatusConverter"/> class.
    /// </summary>
    public SyncStatusConverter() 
        : base(
            v => v.ToString(),                                    // Convert enum to string
            v => (SyncStatus)Enum.Parse(typeof(SyncStatus), v)) // Convert string to enum
    {
    }
}

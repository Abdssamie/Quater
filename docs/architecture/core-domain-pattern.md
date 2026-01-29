# Core Domain Pattern Architecture

## Overview

The Quater project implements a **Core Domain Pattern** to eliminate code duplication between the backend API and desktop application. Previously, domain models and enums were duplicated across two locations:

- `backend/src/Quater.Backend.Core/Models/` and `Enums/`
- `desktop/src/Quater.Desktop.Data/Models/` and `Enums/`

This duplication created maintenance challenges:
- Changes had to be synchronized manually across both locations
- Risk of inconsistencies between backend and desktop implementations
- Increased cognitive load for developers
- Potential for bugs when models diverged

## Solution: Shared Domain Models

We consolidated all domain models and enums into a single shared project:

```
shared/
├── Models/
│   ├── Sample.cs
│   ├── TestResult.cs
│   ├── Parameter.cs
│   ├── SyncLog.cs
│   ├── Lab.cs
│   ├── User.cs
│   ├── AuditLog.cs
│   └── AuditLogArchive.cs
├── Enums/
│   ├── SampleType.cs
│   ├── SampleStatus.cs
│   ├── TestMethod.cs
│   ├── ComplianceStatus.cs
│   └── UserRole.cs
└── Infrastructure/
    └── Converters/
        ├── SampleTypeConverter.cs
        ├── SampleStatusConverter.cs
        ├── TestMethodConverter.cs
        ├── ComplianceStatusConverter.cs
        └── UserRoleConverter.cs
```

Both the backend and desktop applications now reference the shared project:

```xml
<ProjectReference Include="..\..\shared\Quater.Shared.csproj" />
```

## Architecture

### Project Structure

```
Quater/
├── shared/                          # Shared domain models (Quater.Shared)
│   ├── Models/                      # Domain entities
│   ├── Enums/                       # Domain enumerations
│   └── Infrastructure/
│       └── Converters/              # EF Core Value Converters
│
├── backend/
│   ├── src/
│   │   ├── Quater.Backend.Core/     # Business logic, DTOs, validators
│   │   ├── Quater.Backend.Data/     # PostgreSQL DbContext
│   │   └── Quater.Backend.Api/      # ASP.NET Core API
│   └── tests/
│       └── Quater.Backend.Core.Tests/
│
└── desktop/
    └── src/
        ├── Quater.Desktop.Data/     # SQLite DbContext
        └── Quater.Desktop/          # Avalonia UI application
```

### Benefits

1. **Single Source of Truth**: Models are defined once in the shared project
2. **Type Safety**: Both applications use the same strongly-typed enums
3. **Consistency**: Impossible for models to diverge between backend and desktop
4. **Maintainability**: Changes to models are automatically reflected everywhere
5. **Database Compatibility**: Value Converters handle database-specific storage requirements

## Value Converters

### The Challenge

Different databases handle enums differently:

- **PostgreSQL** (Backend): Can store enums as strings natively using `HasConversion<string>()`
- **SQLite** (Desktop): Requires explicit Value Converters for enum-to-string conversion

### The Solution

We created EF Core Value Converters in the shared project that handle enum-to-string conversion for SQLite:

```csharp
public class SampleTypeConverter : ValueConverter<SampleType, string>
{
    public SampleTyerter() 
        : base(
            v => v.ToString(),                              // Enum -> String
            v => (SampleType)Enum.Parse(typeof(SampleType), v))  // String -> Enum
    {
    }
}
```

### Database Storage

Both databases store enums as strings, ensuring compatibility:

| Enum Value | Database Storage |
|------------|------------------|
| `SampleType.DrinkingWater` | `"DrinkingWater"` |
| `SampleStatus.Pending` | `"Pending"` |
| `TestMethod.Spectrophotometry` | `"Spectrophotometry"` |
| `ComplianceStatus.Pass` | `"Pass"` |
| `UserRole.Admin` | `"Admin"` |

### Backend Configuration (PostgreSQL)

The backend uses EF Core's built-in `HasConversion<string>()`:

```csharp
// backend/src/Quater.Backend.Data/QuaterDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Sample>(entity =>
    {
        entity.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>();  // PostgreSQL native conversion
            
        entity.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();
    n```

### Desktop Configuration (SQLite)

The desktop uses explicit Value Converters from the shared project:

```csharp
// desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs
using Quater.Shared.Infrastructure.Converters;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Sample>(entity =>
    {
        entity.Property(e => e.Type)
            .IsRequired()
            .HasConversion(new SampleTypeConverter());  // Explicit converter
            
        entity.Property(e => e.Status)
            .IsRequired()
            .HasConversion(new SampleStatusConverter());
    });
}
```

## Adding New Models

When adding a new domain model to the shared project:

1. **Create the model** in `shared/Models/YourModel.cs`:
   ```csharp
   namespace Quater.Shared.Models;
   
   public class YourModel
   {
       public Guid Id { get; set; }
       public string Name { get; set; } = string.Empty;
       // ... other properties
   }
   ```

2. **Add DbSet to backend** in `backend/src/Quater.Backend.Data/QuaterDbContext.cs`:
   ```csharp
   public DbSet<YourModel> YourModels { get; set; } = null!;
   ```

3. **Configure entity** in the same file:
   ```csha private void ConfigureYourModel(ModelBuilder modelBuilder)
   {
       modelBuilder.Entity<YourModel>(entity =>
       {
           entity.ToTable("YourModels");
           entity.HasKey(e => e.Id);
           // ... configure properties
       });
   }
   ```

4. **Generate backend migration**:
   ```bash
   cd backend/src/Quater.Backend.Data
   dotnet ef migrations add AddYourModel
   ```

5. **Add DbSet to desktop** (if needed) in `desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs`

6. **Generate desktop migration** (if needed):
   ```bash
   cd desktop/src/Quater.Desktop.Data
   dotnet ef migrations add AddYourModel
   ```

## Adding New Enums

When adding a new enum that needs database storage:

1. **Create the enum** in `shared/Enums/YourEnum.cs`:
   ```csharp
   namespace Quater.Shared.Enums;
   
   public enum YourEnum
   {
       Value1,
       Value2,
       Value3
   }
   ```

2. **Create a Value Converter** in `shared/Infrastructure/Converters/YourEnumConverter.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
   using Quater.Shared.Enums;
   
   namespace Quater.Shared.Infrastructure.Converters;
   
   /// <summary>
   /// EF Core Value Converter for YourEnum to string conversion.
   /// Enables SQLite compatibility while maintaining type safety in C# code.
   /// Converts: YourEnum.Value1 <-> "Value1"
   /// </summary>
   public class YourEnumConverter : ValueConverter<YourEnum, string>
   {
       public YourEnumConverter() 
           : base(
               v => v.ToString(),
               v => (YourEnum)Enum.Parse(typeof(YourEnum), v))
       {
       }
   }
   ```

3. **Apply in backend DbContext** using `HasConversion<string>()`:
   ```csharp
   entity.Property(e => e.YourEnumProperty)
       .IsRequired()
       .HasConversion<string>();
   ```

4. **Apply in desktop DbContext** using the explicit converter:
   ```csharp
   using Quater.Shared.Infrastructure.Converters;
   
   entity.Property(e => e.YourEnumProperty)
       .IsRequired()
       .HasConversion(new YourEnumConverter());
   ```

5. **Generate migrations** for both backend and desktop as shown above.

## Testing

### Unit Tests

Unit tests reference the shared models directly:

```csharp
using Quater.Shared.Models;
using Quater.Shared.Enums;

[Fact]
public void Sample_Should_Have_Valid_Status()
{
    var sample = new Sample
    {
        Status = SampleStatus.Pending,
        Type = SampleType.DrinkingWater
    };
    
    Assert.Equal(SampleStatus.Pending, sample.Status);
}
```

### Value Converter Tests

Test that converters properly handle enum-to-string conversion:

```csharp
[Fact]
public void SampleTypeConverter_Should_Convert_To_String()
{
    var converter = new SampleTypeConverter();
    var result = converter.ConvertToProvider(SampleType.DrinkingWater);
    Assert.Equal("DrinkingWater", result);
}

[Fact]
public void SampleTypeConverter_Should_Convert_From_String()
{
    var converter = new SampleTypeConverter();
    var result = converter.ConvertFromProvider("DrngWater");
    Assert.Equal(SampleType.DrinkingWater, result);
}
```

## Migration History

This architecture was implemented through a series of migrations:

1. **Backend Migration**: `20260129213357_UseSharedModels` - No schema changes (no-op migration)
2. **Desktop Migration**: `20260129214849_UseSharedModelsWithConverters` - No schema changes (no-op migration)

Both migrations were no-ops because we only changed the code structure, not the database schema. Enums were already stored as strings in both databases.

## Best Practices

1. **Always use shared models**: Never create duplicate model definitions
2. **Use Value Converters for SQLite**: Always create converters for new enums
3. **Test converters**: Write unit tests for all Value Converters
4. **Generate migrations**: Always generate and test migrations after model changes
5. **Document changes**: Update this documentation when adding new patterns

## Troubleshooting

### Issue: "Type 'YourEnum' cannot be used as a property on entity type"

**Solution**: Create a Value Converter for the enum and apply it in the DbContext configuration.

### Issue: "No suitable constrfound for entity type"

**Solution**: Ensure your model has a parameterless constructor or configure the entity properly in OnModelCreating.

### Issue: Migration shows unexpected schema changes

**Solution**: Verify that enum properties use the same conversion strategy (string) in both old and new configurations.

## References

- [EF Core Value Converters Documentation](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions)
- [EF Core Enum Storage](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions#enum-to-string-conversions)
- [SQLite Type Affinity](https://www.sqlite.org/datatype3.html)

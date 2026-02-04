# Value Converter Usage Guide

## Quick Start

Value Converters enable SQLite to store enums as strings while maintaining type safety in C# code. This guide shows you how to use existing converters and create new ones.

## Using Existing Converters

### Available Converters

The shared project provides these converters out of the box:

| Converter | Enum Type | Usage |
|-----------|-----------|-------|
| `SampleTypeConverter` | `SampleType` | Sample water type classification |
| `SampleStatusConverter` | `SampleStatus` | Sample processing status |
| `TestMethodConverter` | `TestMethod` | Laboratory test methodology |
| `ComplianceStatusConverter` | `ComplianceStatus` | Regulatory compliance result |
| `UserRoleConverter` | `UserRole` | User permission level |

### Desktop Application (SQLite)

Apply converters explicitly in your DbContext:

```csharp
using Quater.Shared.Infrastructure.Converters;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Sample>(entity =>
    {
        // Apply converter to enum property
        entity.Property(e => e.Type)
            .IsRequired()
            .HasConversion(new SampleTypeConverter());
            
        entity.Property(e => e.Status)
            .IsRequired()
            .HasConversion(new SampleStatusConverter());
    });
}
```

### Backend Application (PostgreSQL)

Use EF Core's built-in string conversion:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Sample>(entity =>
    {
        // PostgreSQL can use built-in conversion
        entity.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>();
            
        entity.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();
    });
}
```

## Creating New Converters

### Step 1: Define Your Enum

Create your enum in `shared/Enums/`:

```csharp
// shared/Enums/Priority.cs
namespace Quater.Shared.Enums;

/// <summary>
/// Represents the priority level of a task or sample.
/// </summary>
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}
```

### Step 2: Create the Converter

Create a converter in `shared/Infrastructure/Converters/`:

```csharp
// shared/Infrastructure/Converters/PriorityConverter.cs
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for Priority enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// Converts: Priority.High <-> "High"
/// </summary>
public class PriorityConverter : ValueConverter<Priority, string>
{
    public PriorityConverter() 
        : base(
            v => v.ToString(),                           // Enum to string
            v => (Priority)Enum.Parse(typeof(Priority), v))  // String to enum
    {
    }
}
```

### Step 3: Apply the Converter

#### In Desktop DbContext (SQLite)

```csharp
// desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs
using Quater.Shared.Infrastructure.Converters;

private void ConfigureYourEntity(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<YourEntity>(entity =>
    {
        entity.Property(e => e.PriorityLevel)
            .IsRequired()
            .HasConversion(new PriorityConverter());  // Use explicit converter
    });
}
```

#### In Backend DbContext (PostgreSQL)

```csharp
// backend/src/Quater.Backend.Data/QuaterDbContext.cs

private void ConfigureYEntity(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<YourEntity>(entity =>
    {
        entity.Property(e => e.PriorityLevel)
            .IsRequired()
            .HasConversion<string>();  // Use built-in conversion
    });
}
```

### Step 4: Generate Migrations

Generate migrations for both databases:

```bash
# Backend (PostgreSQL)
cd backend/src/Quater.Backend.Data
dotnet ef migrations add AddPriorityEnum

# Desktop (SQLite)
cd desktop/src/Quater.Desktop.Data
dotnet ef migrations add AddPriorityEnum
```

### Step 5: Test the Converter

Create unit tests to verify the converter works correctly:

```csharp
// tests/Quater.Backend.Core.Tests/Converters/PriorityConverterTests.cs
using Quater.Shared.Enums;
using Quater.Shared.Infrastructure.Converters;
using Xunit;

namespace Quater.Backend.Core.Tests.Converters;

public class PriorityConverterTests
{
    [Theory]
    [InlineData(Priority.Low, "Low")]
    [InlineData(Priority.Medium, "Medium")]
    [InlineData(Priority.High, "High")]
    [InlineData(Priority.Critical, "Critical")]
    public void Should_Convert_Enum_To_String(Priority priority, string expected)
    {
        // Arrange
      onverter = new PriorityConverter();
        
        // Act
        var result = converter.ConvertToProvider(priority);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Low", Priority.Low)]
    [InlineData("Medium", Priority.Medium)]
    [InlineData("High", Priority.High)]
    [InlineData("Critical", Priority.Critical)]
    public void Should_Convert_String_To_Enum(string value, Priority expected)
    {
        // Arrange
        var converter = new PriorityCo
        
        // Act
        var result = converter.ConvertFromProvider(value);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Should_Throw_On_Invalid_String()
    {
        // Arrange
        var converter = new PriorityConverter();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            converter.ConvertFromProvider("InvalidValue"));
    }
}
```

## Testing Converters

### Unit Testing Pattern

Always test both directions of conversion:

```csharp
[Fact]
public void Converter_Should_Handle_Roundtrip()
{
    // Arrange
    var co= new YourEnumConverter();
    var originalValue = YourEnum.SomeValue;
    
    // Act
    var stringValue = converter.ConvertToProvider(originalValue);
    var roundtripValue = converter.ConvertFromProvider(stringValue);
    
    // Assert
    Assert.Equal(originalValue, roundtripValue);
}
```

### Integration Testing

Test that converters work with EF Core:

```csharp
[Fact]
public async Task Should_Store_And_Retrieve_Enum_Value()
{
    // Arrange
    using var context = CreateTestContext();
    var entity = new YourEntity
    {
        Id = Guid.NewGuid(),
        EnumProperty = YourEnum.SomeValue
    };
    
    // Act
    context.YourEntities.Add(entity);
    await context.SaveChangesAsync();
    
    var retrieved = await context.YourEntities
        .FirstOrDefaultAsync(e => e.Id == entity.Id);
    
    // Assert
    Assert.NotNull(retrieved);
    Assert.Equal(YourEnum.SomeValue, retrieved.EnumProperty);
}
```

## Common Pitfalls

### ❌ Don't: Use integers for enum storage

```csharp
// BAD: Storing as integer
entity.Property(e => e.Status)
    .HasConversion<int>();  // Breaks when enum values change
```

**Why it's bad**: If you reorder enum values or add new ones, existing data becomes invalid.

##Use strings for enum storage

```csharp
// GOOD: Storing as string
entity.Property(e => e.Status)
    .HasConversion<string>();  // Resilient to enum reordering
```

### ❌ Don't: Forget to create converters for SQLite

```csharp
// BAD: No converter for SQLite
entity.Property(e => e.Status)
    .IsRequired();  // SQLite will fail to store enum
```

### ✅ Do: Always use converters for SQLite enums

```csharp
// GOOD: Explicit converter for SQLite
entity.Property(e => e.Status)
    .IsRequired()
    .HasConversion(new StatusConverter());
```

### ❌ Don't: Mix conversion strategies

```csharp
// BAD: Different strategies in backend vs desktop
// Backend: .HasConversion<int>()
// Desktop: .HasConversion(new StatusConverter())
// Result: Data incompatibility during sync
```

### ✅ Do: Use consistent string storage everywhere

```csharp
// GOOD: Both use string storage
// Backend: .HasConversion<string>()
// Desktop: .HasConversion(new StatusConverter())
// Result: Compatible data format
```

### ❌ Don't: Forget to handle null values

```csharp
// BAD: No null handling
public class BadConverter : ValueConverter<Status?, string>
{
    public BadConverter() : base(
        v => v.ToString(),  // Throws on null
        v => (Status)Enum.Parse(typeof(Status), v))
    {
    }
}
```

### ✅ Do: Handle nullable enums properly

```csharp
// GOOD: Proper null handling
public class GoodConverter : ValueConverter<Status?, string?>
{
    public GoodConverter() : base(
        v => v.HasValue ? v.Value.ToString() : null,
        v => string.IsNullOrEmpty(v) ? null : (Status?)Enum.Parse(typeof(Status), v))
    {
    }
}
```

## Advanced Patterns

### Custom String Formatting

If you need custom string repreon:

```csharp
public class CustomStatusConverter : ValueConverter<Status, string>
{
    public CustomStatusConverter() : base(
        v => v switch
        {
            Status.InProgress => "in_progress",  // Custom format
            Status.Completed => "completed",
            _ => v.ToString().ToLowerInvariant()
        },
        v => v switch
        {
            "in_progress" => Status.InProgress,
            "completed" => Status.Completed,
            _ => (Status)Enum.Parse(typeof(Status), v, ignoreCase: true)
        })
    {
    }
}
```

### Validation in Converters

Add validation to catch invalid values early:

```csharp
public class ValidatedConverter : ValueConverter<Status, string>
{
    public ValidatedConverter() : base(
        v => v.ToString(),
        v => 
        {
            if (!Enum.TryParse<Status>(v, out var result))
            {
                throw new ArgumentException(
                    $"Invalid status value: '{v}'. Valid values are: {string.Join(", ", Enum.GetNames<Status>())}");
            }
            return result;
        })
    {
    }
}
```

## Performance Considern### Converter Reuse

Converters are lightweight and can be reused:

```csharp
// Create once, use multiple times
private static readonly SampleTypeConverter _sampleTypeConverter = new();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Sample>()
        .Property(e => e.Type)
        .HasConversion(_sampleTypeConverter);
        
    modelBuilder.Entity<HistoricalSample>()
        .Property(e => e.Type)
        .HasConversion(_sampleTypeConverter);  // Reuse same instance
}
```

### Caching Parsed Values

For frequently accessed enums, consider caching:

```csharp
public class CachedConverter : ValueConverter<Status, string>
{
    private static readonly Dictionary<string, Status> _cache = 
        Enum.GetValues<Status>()
            .ToDictionary(v => v.ToString(), v => v);
    
    public CachedConverter() : base(
        v => v.ToString(),
        v => _cache.TryGetValue(v, out var result) 
            ? result 
            : (Status)Enum.Parse(typeof(Status), v))
    {
    }
}
```

## Troubleshooting

### Problem: "Cannot convert enum to string"

**Symptom**: EF Core throws an error when saving entities with enum properties.

**Solution**: Ensure you've applied a converter to the property:
```csharp
entity.Property(e => e.EnumProperty)
    .HasConversion(new YourEnumConverter());
```

### Problem: "Invalid cast from 'System.String' to 'YourEnum'"

**Symptom**: Error when querying entities from the database.

**Solution**: The converter is not being applied. Check that:
1. The converter is registered in `OnModelCreating`
2. You're using the correct converter type
3. The migration was generated and applied

### Problem: Migration shows unexpected changes

**Symptom**: Migration wants to alter existing columns.

**Solution**: Ensure both old and new configurations use sge:
```bash
# Check what the migration will do
dotnet ef migrations add TestMigration --no-build
# Review the generated migration file
# If it's a no-op (empty Up/Down methods), you're good
```

### Problem: Sync fails with "Invalid enum value"

**Symptom**: Desktop app fails to sync data from backend.

**Solution**: Ensure both databases use the same string format:
- Backend: `HasConversion<string>()`
- Desktop: `HasConversion(new YourConverter())`
- Both should produce identical string values

## Quick Reference

### Checklist for Adding New Enum

- [ ] Create enum in `shared/Enums/`
- [ ] Create converter in `shared/Infrastructure/Converters/`
- [ ] Add XML documentation to converter
- [ ] Apply converter in desktop DbContext (explicit)
- [ ] Apply conversion in backend DbContext (`<string>`)
- [ ] Generate backend migration
- [ ] Generate desktop migration
- [ ] Write unit tests for converter
- [ ] Test roundtrip conversion
- [ ] Verify migrations are no-op if replacing existing enum
- [ ] Update this documentation if needed

### File Locations

```
shared/
├── Enums/
│   └── YourEnum.cs                    # 1. Define enum here
└── Infrastructure/
    └── Converters/
        └── YourEnumConverter.cs       # 2. Create converter here

desktop/src/Quater.Desktop.Data/
└── QuaterLocalContext.cs              # 3. Apply converter here (SQLite)

backend/src/Quater.Backend.Data/
└── QuaterDbContext.cs                 # 4. Apply conversion here (PostgreSQL)

tests/Quater.Backend.Core.Tests/
└── Converters/
    └── YourEnumConverterTests.cs      # 5. Test converter here
```

## Additional Resources

- [Core Domain Pattern Architecture](../architecture/core-domain-pattern.md) - Full architecture documentation
- [EF Core Value Conversions](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions) - Official Microsoft docs
- [SQLite Type Affinity](https://www.sqlite.org/datatype3.html) - Understanding SQLite types

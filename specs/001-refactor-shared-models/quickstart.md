# Quickstart Guide: Refactored Shared Models

**Feature**: Refactor Shared Models for Consistency and Maintainability  
**Date**: 2025-01-17

## Overview

This guide helps developers quickly understand and use the refactored shared models with value objects, type-safe enums, and immutability enforcement.

---

## For Developers Using These Models

### Creating a Location

**Before** (separate properties):
```csharp
var sample = new Sample
{
    LocationLatitude = 33.5731,
    LocationLongitude = -7.5898,
    LocationDescription = "Municipal Well #3",
    LocationHierarchy = "Casablanca/Anfa/Site-A"
};
```

**After** (value object):
```csharp
var location = new Location(
    latitude: 33.5731,
    longitude: -7.5898,
    description: "Municipal Well #3",
    hierarchy: "Casablanca/Anfa/Site-A"
);

var sample = new Sample
{
    Location = location,
    // ... other properties
};
```

**Validation** (automatic):
```csharp
// This throws ArgumentOutOfRangeException
var invalidLocation = new Location(91.0, -7.5898);
```

---

### Creating a Measurement

**Before** (separate properties):
```csharp
var testResult = new TestResult
{
    ParameterName = "pH",
    Value = 7.2,
    Unit = "pH"
};
```

**After** (value object with validation):
```csharp
// First, get the Parameter
var phParameter = await context.Parameters
    .FirstAsync(p => p.Name == "pH");

// Create measurement with validation
var measurement = new Measurement(phParameter, 7.2, "pH");

var testResult = new TestResult
{
    Measurement = measurement,
    // ... other properties
};
```

**Validation** (automatic):
```csharp
// This throws ArgumentException (wrong unit)
var invalid = new Measurement(phParameter, 7.2, "mg/L");

// This throws ArgumentOutOfRangeException (value out of range)
var invalid = new Measurement(phParameter, 15.0, "pH");
```

---

### Using EntityType Enum

**Before** (magic strings):
```csharp
var auditLog = new AuditLog
{
    EntityType = "Sample" // Typo risk!
};

// Filtering
var logs = context.AuditLogs
    .Where(log => log.EntityType == "Sample");
```

**After** (type-safe):
```csharp
var auditLog = new AuditLog
{
    EntityType = EntityType.Sample // Compile-time safety
};

// Filtering with autocomplete
var logs = context.AuditLogs
    .Where(log => log.EntityType == EntityType.Sample);
```

---

### Handling TestResult Immutability

**Draft → Submitted**:
```csharp
// Create draft
var testResult = new TestResult
{
    Status = TestResultStatus.Draft,
    Measurement = measurement,
    TestDate = DateTime.UtcNow,
    TechnicianName = "John Doe"
};

await context.TestResults.AddAsync(testResult);
await context.SaveChangesAsync();

// Submit (makes immutable)
testResult.Status = TestResultStatus.Submitted;
await context.SaveChangesAsync();

// Now immutable - this will throw InvalidOperationException
testResult.Measurement = newMeasurement; // ERROR!
```

**Voiding and Replacing**:
```csharp
// Create replacement
var replacement = new TestResult
{
    VoidedTestResultId = testResult.Id,
    Measurement = correctedMeasurement,
    Status = TestResultStatus.Draft
};

// Void original
testResult.Status = TestResultStatus.Voided;
testResult.ReplacedByTestResultId = replacement.Id;
testResult.VoidReason = "Incorrect parameter reading";

await context.SaveChangesAsync();
```

---

## For Migration

### Pre-Migration Checklist

- [ ] Backup production database
- [ ] Verify all Parameter definitions are complete (Name, Unit, MinValue, MaxValue)
- [ ] Test migration on development database
- [ ] Review migration SQL scripts
- [ ] Plan rollback procedure
- [ ] Schedule maintenance window (if needed)

### Running Migrations

```bash
# Navigate to backend project
cd backend/src/Quater.Backend.Data

# Create migration
dotnet ef migrations add RefactorSharedModels

# Review generated migration
# Check Up() and Down() methods

# Apply to development database
dotnet ef database update

# Validate data
# Run validation queries (see below)

# Apply to production (after testing)
dotnet ef database update --connection "ProductionConnectionString"
```

### Post-Migration Validation

**Check for data loss**:
```sql
-- Verify Sample locations migrated
SELECT COUNT(*) FROM Samples 
WHERE LocationLatitude IS NULL OR LocationLongitude IS NULL;
-- Should be 0

-- Verify TestResult measurements migrated
SELECT COUNT(*) FROM TestResults 
WHERE ParameterId IS NULL OR Value IS NULL OR Unit IS NULL;
-- Should be 0

-- Verify audit log entity types converted
SELECT COUNT(*) FROM AuditLogs 
WHERE EntityType IS NULL OR EntityType NOT IN (1,2,3,4,5,6,7,8,9);
-- Should be 0
```

### Rollback Procedure

```bash
# Revert to previous migration
dotnet ef database update PreviousMigrationName

# Or rollback all changes
dotnet ef database update 0
```

---

ommon Pitfalls

### ❌ Don't: Use old property names

```csharp
// This won't compile anymore
var lat = sample.LocationLatitude; // ERROR: Property doesn't exist
```

**✅ Do**: Use value object
```csharp
var lat = sample.Location.Latitude;
```

---

### ❌ Don't: Try to modify submitted TestResults

```csharp
var testResult = await context.TestResults.FindAsync(id);
if (testResult.Status == TestResultStatus.Submitted)
{
    testResult.Measurement = newMeasurement; // Throws exception!
}
```

**✅ Do**: Create replacement
```csharp
if (testResult.Status == TestResultStatus.Submitted)
{
    var replacement = TestResult
    {
        VoidedTestResultId = testResult.Id,
        Measurement = correctedMeasurement
    };
    testResult.Status = TestResultStatus.Voided;
    testResult.ReplacedByTestResultId = replacement.Id;
}
```

---

### ❌ Don't: Create invalid coordinates

```csharp
// This throws exception
var location = new Location(200.0, 300.0);
```

**✅ Do**: Validate input first
```csharp
if (latitude >= -90 && latitude <= 90 && 
    longitude >= -180 && longitude <= 180)
{
    var location = new Location(latitude, longitude);
}
else
{
    // Handle invalid input
}
```

---

### ❌ Don't: Create invalid parameter/unit combinations

```csharp
var phParameter = await context.Parameters.FirstAsync(p => p.Name == "pH");

// This throws exception (wrong unit)
var measurement = new Measurement(phParameter, 7.2, "mg/L");
```

**✅ Do**: Use correct unit
```csharp
var measurement = new Measurement(phParameter, 7.2, phParameter.Unit);
```

---

## Property Reference

### Consolidated Properties

| Old Property | New Property | Model |
|--------------|--------------|-------|
| `CreatedDate` | `CreatedAt` | All models |
| `LastModified` | `UpdatedAt` | All models | `Version` | `RowVersion` | All models |
| `LocationLatitude` | `Location.Latitude` | Sample |
| `LocationLongitude` | `Location.Longitude` | Sample |
| `ParameterName` | `Measurement.ParameterId` | TestResult |
| `Value` | `Measurement.Value` | TestResult |
| `Unit` | `Measurement.Unit` | TestResult |

---

## Need Help?

- **Documentation**: See `data-model.md` for complete model definitions
- **Contracts**: See `contracts/value-objects.md` for validation rules
- **Research**: See `research.md` for technical decisions

**Quickstart Status**: ✅ COMPLETE

# Data Model Design

**Feature**: Refactor Shared Models for Consistency and Maintainability  
**Date**: 2025-01-17  
**Status**: Complete

## Overview

This document defines the refactored data model for all entities in the `shared/Models/` directory, including new value objects and consolidated properties.

---

## Value Objects

### Location

**Purpose**: Encapsulate GPS coordinates with validation to prevent invalid location data.

**Definition**:
```csharp
namespace Quater.Shared.ValueObjects;

/// <summary>
/// Represents a geographic location with GPS coordinates and descriptive information.
/// Validates coordinate ranges at construction.
/// </summary>
public sealed record Location
{
    /// <summary>
    /// Latitude coordinate (-90 to 90 degrees)
    /// </summary>
    public double Latitude { get; init; }
    
    /// <summary>
    /// Longitude coordinate (-180 to 180 degrees)
    /// </summary>
    public double Longitude { get; init; }
    
    /// <summary>
    /// Human-readable location description (e.g., "Municipal Well #3")
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Hierarchical location path for reporting (e.g., "Region/District/Site")
    /// </summary>
    public string? Hierarchy { get; init; }
    
    /// <summary>
    /// Creates a new Location with validated coordinates.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If coordinates are invalid</exception>
    public Location(double latitude, double longitude, string? description = null, string? hierarchy = null)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90");
        
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180");
        
        Latitude = latitude;
        Longitude = longitude;
        Description = description;
        Hierarchy = hierarchy;
    }
}
```

**EF Core Mapping** (in DbContext):
```csharp
modelBuilder.Entity<Sample>()
    .OwnsOne(s => s.Location, location =>
    {
        location.Property(l => l.Latitude).HasColumnName("LocationLatitude").IsRequired();
        location.Property(l => l.Longitude).HasColumnName("LocationLongitude").IsRequired();
        location.Property(l => l.Description).HasColumnName("LocationDescription").HasMaxLength(200);
        location.Property(l => l.Hierarchy).HasColumnName("LocationHierarchy").HasMaxLength(500);
    });
```

**Validation Rules**:
- Latitude: -90 to 90 (inclusive)
- Longitude: -180 to 180 (inclusive)
- Description: Optional, max 200 characters
- Hierarchy: Optional, max 500 characters

---

### Measurement

**Purpose**: Encapsulate test measurement data with parameter/unit validation to prevent invalid combinations.

**Definition**:
```csharp
namespace Quater.Shared.ValueObjects;

/// <summary>
/// Represents a water quality measurement with validated parameter/unit combination.
/// </summary>
public sealed record Measurement
{
    /// <summary>
    /// Reference to the Parameter entity defining valid units and ranges
    /// </summary>
    public Guid ParameterId { get; init; }
    
    /// <summary>
    /// Measured value
    /// </summary>
    public double Value { get; init; }
    
    /// <summary>
    /// Unit of measurement (must match Parameter definition)
    /// </summary>
    publg Unit { get; init; }
    
    /// <summary>
    /// Creates a new Measurement with validation against Parameter definition.
    /// </summary>
    /// <param name="parameter">Parameter entity with validation rules</param>
    /// <param name="value">Measured value</param>
    /// <param name="unit">Unit of measurement</param>
    /// <exception cref="ArgumentException">If unit doesn't match parameter or value out of range</exception>
    public Measurement(Parameter parameter, double value, string unit)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
        ArgumentException.ThrowIlOrWhiteSpace(unit, nameof(unit));
        
        // Validate unit matches parameter
        if (!string.Equals(parameter.Unit, unit, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Unit '{unit}' does not match parameter '{parameter.Name}' expected unit '{parameter.Unit}'",
                nameof(unit));
        
        // Validate value range
        if (parameter.MinValue.HasValue && value < parameter.MinValue.Value)
            throw new ArgumentOutOfRangeException(nameof(value), 
                $"Value {value} is below minimum {parameter.MinValue.Value} for parameter '{parameter.Name}'");
        
        if (parameter.MaxValue.HasValue && value > parameter.MaxValue.Value)
            throw new ArgumentOutOfRangeException(nameof(value), 
                $"Value {value} exceeds maximum {parameter.MaxValue.Value} for parameter '{parameter.Name}'");
        
        ParameterId = parameter.Id;
        Value = value;
        Unit = unit;
    }
    
    /// <summary>
    /// Creates a Measurement from existing data (for deserialization/EF Core).
    /// Does not validate against Parameter.
    /// </summary>
    public Measurement(Guid parameterId, double value, string unit)
    {
        ParameterId = parameterId;
        Value = value;
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
    }
}
```

**EF Core Mapping** (in DbContext):
```csharp
modelBuilder.Entity<TestResult>()
    .OwnsOne(tr => tr.Measurement, measurement =>
    {
        measurement.Property(m => m.ParameterId).HasColumnName("ParameterId").IsRequired();
        measurement.Property(m => m.Value).HasColumnName("Value").IsRequired();
        measurement.Property(m => m.Unit).HasColumnName("Unit").HasMaxLength(20).IsRequired();
        
        // Foreign key to Parameter table
        measurement.HasOne<Parameter>()
            .WithMany()
            .HasForeignKey(m => m.ParameterId);
    });
```

**Validation Rules**:
- ParameterId: Must reference existing Parameter
- Value: Must be within Parameter.MinValue and Parameter.MaxValue (if defined)
- Unit: Must match Parameter.Unit exactly (case-insensitive)

---

## Enums

### EntityType (NEW)

**Purpose**: Type-safe reference to entity types, replacing magic strings.

**Definition**:
```csharp
namespace Quater.Shared.Enums;

/// <summary>
/// Defines entity types for audit logging and conflict resolution.
/// </summary>
public enum EntityType
{
    Lab = 1,
    User = 2,
    Sample = 3,
    TestResult = 
    Parameter = 5,
    AuditLog = 6,
    AuditLogArchive = 7,
    ConflictBackup = 8,
    SyncLog = 9
}
```

**Usage**:
```csharp
// Old (magic string - error-prone)
var auditLog = new AuditLog { EntityType = "Sample" };

// New (type-safe)
var auditLog = new AuditLog { EntityType = EntityType.Sample };
```

### TestResultStatus (NEW)

**Purpose**: Track TestResult lifecycle for immutability enforcement.

**Definition**:
```csharp
namespace Quater.Shared.Enums;

/// <summary>
/// Defines the lifecycle status of a TestResult.
/// </summary>
public enum TestResultStatus
{
    /// <summary>
    /// Draft result, can be modified
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Submitted result, immutable for regulatory compliance
    /// </summary>
    Submitted = 1,
    
    /// <summary>
    /// Voided result (replaced by another TestResult)
    /// </summary>
    Voided = 2
}
```

---

## Refactored Models

### Sample

**Changes**:
- ✅ Add: `Location Location { get; init; }`
- ❌ Remove: `LocationLatitude`, `LocationLongitude`, `LocationDescription`, `LocationHierarchy`
- ❌ Remove: `CreatedDate` (use `CreatedAt` from IAuditable)
- ❌ Remove: `LastModified` (use `UpdatedAt` from IAuditable)
- ❌ Remove: `Version` (use `RowVersion` from IConcurrent)
- ✅ Keep: All interface properties consistent

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Represents a water sample collected from a specific location at a specific time.
/// </summary>
public sealed class Sample : IEntity, IAuditable, ISoftDelete, ISyncable, IConcurrent
{
    public Guid Id { get; set; }
    
    public SampleType Type { get; set; }
    
    /// <summary>
    /// Sample collection location with validated GPS coordinates
    /// </summary>
    public Location Location { get; init; } = null!;
    
    public DateTime CollectionDate { get; set; }
    
    public string CollectorName { get; set; } = string.Empty;
    
    public string? Notes { get; set; }
    
    public SampleStatus Status { get; set; }
    
    public Guid LabId { get; set; }
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; s  
    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // ISyncable
    public DateTime LastSyncedAt { get; init; }
    public string? SyncVersion { get; set; }
    
    // IConcurrent
    public byte[] RowVersion { get; set; } = null!;
    
    // Navigation properties
    public Lab Lab { get; set; } = null!;
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
```

---

### TestResult

**Changes**:
- ✅ Add: `Measurement Measurement { get; init; }`
- ✅ Add: `TestResultStatus Status { get; set; }`
- ✅ Add: `Guid? VoidedTestResultId { get; init; }` (for corrections)
- ✅ Add: `Guid? ReplacedByTestResultId { get; set; }` (set when voided)
- ✅ Add: `bool IsVoided { get; set; }`
- ✅ Add: `string? VoidReason { get; set; }`
- ❌ Remove: `ParameterName`, `Value`, `Unit`
- ❌ Remove: `CreatedDate`, `LastModified`, `Version`
- ❌ Remove: Duplicate `CreatedBy` (use IAuditable.CreatedBy)
- ✅ Keep: All interface properties consistent

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Represents a single water quality test performed on a sample.
/// Immutable after submission for regulatory compliance.
/// </summary>
public sealed class TestResult : IEntity, IAuditable, ISoftDelete, ISyncable, IConcurrent
{
    public Guid Id { get; set; }
    
    public Guid SampleId { get; init; }
    
    /// <summary>
    /// Test measurement with validated parameter/unit combination
    /// </summary>
    public Measurement Measurement { get; init; } = null!;
    
    public DateTime TestDate { get; set; }
    
    public string TechnicianName { get; set; } = string.Empty;
    
    public TestMethod TestMethod { get; set; }
    
    publiStatus ComplianceStatus { get; set; }
    
    /// <summary>
    /// Lifecycle status (Draft/Submitted/Voided)
    /// </summary>
    public TestResultStatus Status { get; set; } = TestResultStatus.Draft;
    
    /// <summary>
    /// Reference to TestResult being corrected (if this is a correction)
    /// </summary>
    public Guid? VoidedTestResultId { get; init; }
    
    /// <summary>
    /// Reference to TestResult that replaced this one (if voided)
    /// </summary>
    public Guid? ReplacedByTestResultId { get; set; }
    
    /// <summary>
    /// Whether this TestResult has been voided
    /// </summary>
    public bool IsVoided { get; set; } = false;
    
    /// <summary>
    /// Reason for voiding (required if IsVoided = true)
    /// </summary>
    public string? VoidReason { get; set; }
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // ISyncable
    pubime LastSyncedAt { get; init; }
    public string? SyncVersion { get; set; }
    
    // IConcurrent
    public byte[] RowVersion { get; set; } = null!;
    
    // Navigation properties
    public Sample Sample { get; init; } = null!;
    public TestResult? VoidedTestResult { get; init; }
    public TestResult? ReplacedByTestResult { get; set; }
}
```

---

### Lab

**Changes**:
- ❌ Remove: `CreatedDate` (use `CreatedAt` from IAuditable)
- ✅ Seal class
- ✅ Use immutable collections

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Represents a water quality lab organization.
//mmary>
public sealed class Lab : IEntity,, ISoftDelete, IConcurrent
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Location { get; set; }
    
    public string? ContactInfo { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // IConcurrent
    public byte[] RowVersion { get; set; } = null!;
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Sample> Samples { get; set; } = new List<Sample>();
}
```

---

### Parameter

**Changes**:
- ❌ Remove: `CreatedDate`, `LastModified` (use IAuditable properties)
- ✅ Seal class

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Represents a water quality parameter with compliance thresholds.
/// </summary>
public sealed class Parameter : IEntity, IAuditable, ISoftDelete, ISyncable, IConcurrent
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Unit { get; set; } = string.Empty;
    
    public double? WhoThreshold { get; set; }
    
    public double? MoroccanThreshold { get; set; }
    
    public double? MinValue { get; set; }
    
    public double? MaxValue { get; set; }
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string   public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // ISoftDelete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // ISyncable
    public DateTime LastSyncedAt { get; init; }
    public string? SyncVersion { get; set; }
    
    // IConcurrent
    public byte[] RowVersion { get; set; } = null!;
}
```

---

### AuditLog

**Changes**:
- ✅ Change: `string EntityType` → `EntityType EntityType`
- ✅ Add: Computed property `ArchiveEligibleAt`
- ✅ Add: Computed property `IsEligibleForArchival`
- ✅ Seal class

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Tracks all data modifications for compliance and conflict resolution.
/// Archived after 90 days.
/// </summary>
public sealed class AuditLog : IEntity
{
    public Guid Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of entity modified (type-safe enum)
    /// </summary>
    public EntityType EntType { get; set; }
    
    public Guid EntityId { get; set; }
    
    public AuditAction Action { get; set; }
    
    public string? OldValue { get; set; }
    
    public string? NewValue { get; set; }
    
    public string? ChangedFields { get; set; }
    
    public bool IsTruncated { get; set; } = false;
    
    public string? OverflowStoragePath { get; set; }
    
    public Guid? ConflictBackupId { get; set; }
    
    public string? ConflictResolutionNotes { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string? IpAddress { get; set; }
    
    public bool IsArchived { get; set; } = false;
    
    /// <summary>
    /// Computed: Date when this log becomes eligible for archival (Timestamp + 90 days)
    /// </summary>
    [NotMapped]
    public DateTime ArchiveEligibleAt => Timestamp.AddDays(90);
    
    /// <summary>
    /// Computed: Whether this log is eligible for archival now
    /// </summary>
    [NotMapped]
    public bool IsEligibleForArchival => DateTime.UtcNow >= ArchiveEligibleAt && !IsArchived;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ConflictBackup? ConflictBackup { get; set; }
}
```

---

### AuditLogArchive

**Changes**:
- ✅ Change: `string EntityType` → `EntityType EntityType`
- ✅ Seal class

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Archived audit logs older than 90 days (cold storage).
/// </summary>
public sealed class AuditLogArchive : IEntity
{
    public Guid Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of entity modified (type-safe enum)
    /// </summary>
    public EntityType EntityType { get; set; }
    
    public Guid EntityId { get; set; }
   n    public AuditAction Action { get; set; }
    
    public string? OldValue { get; set; }
    
    public string? NewValue { get; set; }
    
    public string? ChangedFields { get; set; }
    
    public bool IsTruncated { get; set; } = false;
    
    public string? OverflowStoragePath { get; set; }
    
    public Guid? ConflictBackupId { get; set; }
    
    public string? ConflictResolutionNotes { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string? IpAddress { get; set; }
    
    public DateTime ArchivedDate { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ConflictBackup? ConflictBackup { get; set; }
}
```

---

### ConflictBackup

**Changes**:
- ✅ Change: `string EntityType` → `EntityType EntityType`
- ✅ Fix: `UpdatedBy` nullability (should be nullable)
- ✅ Add: Documentation of default "Server wins" strategy
- ✅ Seal class

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Stores backup copies of conflicting records during synchronization.
/// Default strategy: Server wins (server data takes precedence).
/// </summary>
public sealed class ConflictBackup : IEntity, IAuditable
{
    public Guid Id { get; set; }
    
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// Type of entity (type-safe enum)
    /// </summary>
    public EntityType EntityType { get; set; }
    
    public string ServerVersion { get; init; } = string.Empty;
    
    public string ClientVersion { get; init; } = string.Empty;
    
    /// <summary>
    /// Strategy used to resolve the conflict.
    /// Default: ServerWins (for regulatory compliance)
    /// </summary>
    public ConflictResolutionStrategy ResolutionStrategy { get; set; } = ConflictResolutionStrategy.Serveins;
    
    public DateTime ConflictDetectedAt { get; set; }
    
    public DateTime? ResolvedAt { get; set; }
    
    public string? ResolvedBy { get; set; }
    
    public string? ResolutionNotes { get; set; }
    
    public string DeviceId { get; set; } = string.Empty;
    
    public Guid LabId { get; set; }
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; } // Fixed: now nullable
    
    // Navigation properties
    public Lab Lab { get; set; } = null!;
}
```

---

### SyncLog

**Changes**:
- ✅ Seal class
- ✅ Keep existing structure (already clean)

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Tracks synchronization between clients and server.
/// </summary>
public sealed class SyncLog : IEntity, ISyncable
{
    public Guid Id { get; set; }
    
    public string DeviceId { get; init; } = string.Empty;
    
    public required string UserId { get; init; }
    
    public DateTime LastSyncTimestamp { get; set; }
    
    public SyncStatus Status { get; set; } = ending;
    
    public string? ErrorMessage { get; set; }
    
    public int RecordsSynced { get; set; }
    
    public int ConflictsDetected { get; set; }
    
    public int ConflictsResolved { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    // ISyncable
    public DateTime LastSyncedAt { get; init; }
    public string? SyncVersion { get; set; }
    
    // Navigation properties
    public User User { get; init; } = null!;
}
```

---

### User

**Changes**:
- ❌ Remove: `CreatedDate` (use `CreatedAt` from IAuditable)
- ✅ Keep existing structure (already clean)

**Refactored Definition**:
```csharp
namespace Quater.Shared.Models;

/// <summary>
/// Represents a system user with role-based access.
/// Extends ASP.NET Core Identity IdentityUser.
/// </summary>
public class User : IdentityUser, IAuditable, IConcurrent
{
    public UserRole Role { get; set; }
    
    public Guid LabId { get; set; }
    
    public DateTime? LastLogin { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // IConcurrent
    public byte[] RowVersion { get; set; } = null!;
    
    // Navigation properties
    public Lab Lab { get; init; } = null!;
    public ICollection<AuditLog> AuditLogs { get; init; } = new List<AuditLog>();
    public ICollection<AuditLogArchive> AuditLogArchives { get; init; } = new List<AuditLogArchive>();
    public ICollection<SyncLog> SyncLogs { get; init; } = new List<SyncLog>();
}
```

---

## Property Mapping (Old → New)

### Sample
| Old Property | New Property | Notes |
|--------------|--------------|-------|
| `LocationLatitude` | `Location.Latitude` | Part of Location value object |
| `LocationLongitude` | `Location.Longitude` | Part of Location value object |
| `LocationDescription` | `Location.Description` | Part of Location value object |
| `LocationHierarchy` | `Location.Hierarchy` | Part of Location value object |
| `CreatedDate` | `CreatedAt` | From IAuditable |
| `LastModified` | `UpdatedAt` | From IAuditable |
| `Version` | `RowVersion` | From IConcurrent |

### TestResult
| Old Property | New Property | Notes |
|--------------|--------------------|
| `ParameterName` | `Measurement.ParameterId` | Type-safe reference |
| `Value` | `Measurement.Value` | Part of Measurement value object |
| `Unit` | `Measurement.Unit` | Part of Measurement value object |
| `CreatedDate` | `CreatedAt` | From IAuditable |
| `LastModified` | `UpdatedAt` | From IAuditable |
| `Version` | `RowVersion` | From IConcurrent |
| `CreatedBy` (duplicate) | `CreatedBy` | From IAuditable (removed duplicate) |

### Lab
| Old Property | New Property | Notes |
|--------------|--------------|-------|
| `CreatedDate` | `CreatedAt` | From IAuditable |

### Parameter
| Old Property | New Property | Notes |
|--------------|--------------|-------|
| `CreatedDate` | `CreatedAt` | From IAuditable |
| `LastModified` | `UpdatedAt` | From IAuditable |

### AuditLog / AuditLogArchive
| Old Property | New Property | Notes |
|--------------|--------------|-------|
| `EntityType` (string) | `EntityType` (enum) | Type-safe enum |

### ConflictBackup
| Old Property | New Property | Notes |
|--------------|--------------|-------|
| `EntityType` (string) | `EntityType` (enum) | Type-safe enum |
| `UpdatedBy` (non-nullable) | `UpdatedBy` (nullable) | Fixed nullability |

---

## Database Schema Changes

### New Tables
- None (value objects use table splitting)

### Modified Columns

**Samples**:
- No schema changes (Location maps to existing columns)

**TestResults**:
- Rename: `ParameterName` → `ParameterId` (type change: string → Guid)
- Keep: `Value`, `Unit` (same columns, different access path)
- Add: `Status` (int, default 0 = Draft)
- Add: `VoidedTestResultId` (Guid, nullable)
- Add: `ReplacedByTestResultId` (Guid, nullable)
- Add: `IsVoided` (bit, default 0)
- Add: `VoidReason` (nvarchar(1000), nullable)
- Drop: `CreatedDate`, `LastModified`, `Version`

**Labs**:
- Drop: `CreatedDate`

**Parameters**:
- Drop: `CreatedDate`, `LastModified`

**AuditLogs / AuditLogArchives**:
- Modify: `EntityType` (nvarchar(100) → int)

**ConflictBackups**:
- Modify: `EntityType` (nvarchar(100) → int)

---

**Data Model Status**: ✅ COMPLETE  
**Next Step**: Create contracts/ directory with value object contracts and migration guide

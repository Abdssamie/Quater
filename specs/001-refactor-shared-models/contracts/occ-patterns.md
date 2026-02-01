# Optimistic Concurrency Control (OCC) Patterns

**Version**: 1.0  
**Date**: 2025-02-01  
**Status**: Active

## Overview

This document defines how Optimistic Concurrency Control (OCC) is implemented in Quater using Entity Framework Core's `RowVersion` mechanism. OCC prevents lost updates when multiple users edit the same entity simultaneously.

**Key Principle**: OCC is **completely independent** of navigation properties. Removing navigations does NOT affect concurrency control.

---

## What is Optimistic Concurrency Control?

### The Problem: Lost Updates

```
Time    User A                          User B
----    ------                          ------
T1      Read Sample (pH = 7.0)         
T2                                      Read Sample (pH = 7.0)
T3      Update pH = 7.5                
T4      Save ✅                         
T5                                      Update pH = 6.8
T6                                      Save ✅ (overwrites A's change!)
```

**Result**: User A's change (pH = 7.5) is lost! User B unknowingly overwrote it.

### The Solution: Version Checking

```
Time    User A                          User B
----    ------                          ------
T1      Read Sample (pH = 7.0, v1)     
T2                                      Read Sample (pH = 7.0, v1)
T3      Update pH = 7.5                
T4      Save ✅ (version → v2)         
T5                                      Update pH = 6.8
T6                                      Save ❌ CONFLICT! (expected v1, found v2)
T7                                      Reload, see A's change, decide what to do
```

**Result**: User B is notified of the conflict and can choose how to resolve it.

---

## Implementation in Quater

### 1. IConcurrent Interface

**File**: `shared/Interfaces/IConcurrent.cs`

```csharp
namespace Quater.Shared.Interfaces;

/// <summary>
/// Interface for entities that support optimistic concurrency control.
/// Uses a row version to detect concurrent modifications.
/// </summary>
public interface IConcurrent
{
    /// <summary>
    /// Gets or sets the row version for optimistic concurrency control.
    /// This value is automatically updated by the database on each modification.
    /// </summary>
    byte[] RowVersion { get; set; }
}
```

**Key Points**:
- `byte[]` type is required for EF Core's `[Timestamp]` attribute
- Database automatically updates this value on every UPDATE
- Application code should **never** manually set `RowVersion`

### 2. Model Implementation

**Example**: `shared/Models/Sample.cs`

```csharp
public sealed class Sample : IEntity, IAuditable, ISoftDelete, ISyncable, IConcurrent
{
    // ... other properties ...

    // IConcurrent interface property
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
```

**Entities with OCC**:
- ✅ `Sample` - Field data can be edited by multiple technicians
- ✅ `TestResult` - Test results can be reviewed/corrected
- ✅ `Parameter` - Parameter definitions can be updated
- ✅ `Lab` - Lab settings can be modified
- ✅ `User` - User profiles can be updated
- ❌ `AuditLog` - Immutable (append-only)
- ❌ `ConflictBackup` - Immutable (created once)
- ❌ `SyncLog` - Immutable (append-only)

### 3. EF Core Configuration

**File**: `backend/src/Quater.Backend.Data/Configurations/SampleConfiguration.cs`

```csharp
public class SampleConfiguration : IEntityTypeConfiguration<Sample>
{
    public void Configure(EntityTypeBuilder<Sample> entity)
    {
        // RowVersion is configured automatically by [Tim] attribute
        // EF Core adds this configuration:
        entity.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();
        
        // No manual configuration needed!
    }
}
```

**Database Schema** (PostgreSQL):
```sql
CREATE TABLE "Samples" (
    "Id" uuid NOT NULL,
    -- ... other columns ...
    "RowVersion" bytea NOT NULL,  -- xmin in PostgreSQL
    CONSTRAINT "PK_Samples" PRIMARY KEY ("Id")
);
```

**Database Schema** (SQLite):
```sql
CREATE TABLE "Samples" (
    "Id" TEXT NOT NULL,
    -- ... other columns ...
    "RowVersion" BLOB NOT NULL,
    CONSTRAINT "PK_Samples" PRIMARY KEY ("Id")
);
```

---

## Usage Patterns

### Pattern 1: Basic Update with Conflict Detection

```csharp
public async Task<Result<Sample>> UpdateSampleAsync(Guid id, UpdateSampleDto dto)
{
    try
    {
        // 1. Load entity (includes RowVersion)
        var sample = await _context.Samples.FindAsync(id);
        if (sample == null)
            return Result<Sample>.NotFound();
        
        // 2. Apply changes
        sample.CollectorName = dto.CollectorName;
        sample.Notes = dto.Notes;
        // RowVersion is NOT modified by application c        
        // 3. Save changes
        await _context.SaveChangesAsync();
        
        // 4. Success - RowVersion automatically updated by database
        return Result<Sample>.Success(sample);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // 5. Conflict detected!
        return Result<Sample>.Conflict("Sample was modified by another user");
    }
}
```

### Pattern 2: Reload and Retry

```csharp
public async Task<Result<Sample>> UpdateSampleWithRetryAsync(
    Guid id, 
    UpdateSampleDto dto,
    int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
    {
            var sample = await _context.Samples.FindAsync(id);
            if (sample == null)
                return Result<Sample>.NotFound();
            
            sample.CollectorName = dto.CollectorName;
            sample.Notes = dto.Notes;
            
            await _context.SaveChangesAsync();
            return Result<Sample>.Success(sample);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (attempt == maxRetries - 1)
            {
                // Final attempt failed
                return Result<Sample>.Conflict(
                    "Sample was modified by another user. Please reload and try again.");
            }
            
            // Reload entity with new RowVersion and retry
            var entry = ex.Entries.Single();
            await entry.ReloadAsync();
        }
    }
    
    return Result<Sample>.Conflict("Max retries exceeded");
}
```

### Pattern 3: Show Conflict Details to User

```csharp
public async Task<ConflictInfo> GetConflictDetailsAsync(DbUpdateConcurrencyException ex)
{
    var entry = ex.Entries.Single();
    var entity = entry.Entity as Sample;
    
    // Get current values (what user tried to save)
    var currentValues = entry.CurrentValues;
    // Get database values (what another user saved)
    var databaseValues = await entry.GetDatabaseValuesAsync();
    
    // Get original values (what user started with)
    var originalValues = entry.OriginalValues;
    
    return new ConflictInfo
    {
        EntityType = "Sample",
        EntityId = entity.Id,
        YourChanges = new
        {
            CollectorName = currentValues.GetValue<string>(nameof(Sample.CollectorName)),
            Notes = currentValues.GetValue<string>(nameof(Sample.Notes))
        },
        TheirChanges = new
        {
            CollectorName = databaseValues.GetValue<string>(nameof(Sample.CollectorName)),
            Notes = databaseValues.GetValue<string>(nameof(Sample.Notes))
        },
        OriginalValues = new
        {
            CollectorName = originalValues.GetValue<string>(nameof(Sample.CollectorName)),
            Notes = originalValues.GetValue<string>(nameof(Sample.Notes))
        }
    };
}
```

### Pattern 4: User Chooses Resolution Strategy

```csharp
public async Task<Result<Sample>> Resosync(
    Guid id,
    ConflictResolutionStrategy strategy,
    UpdateSampleDto dto)
{
    var sample = await _context.Samples.FindAsync(id);
    if (sample == null)
        return Result<Sample>.NotFound();
    
    switch (strategy)
    {
        case ConflictResolutionStrategy.ClientWins:
            // User wants to overwrite server changes
            // Reload to get latest RowVersion, then apply user's changes
            await _context.Entry(sample).ReloadAsync();
            sample.CollectorName = dto.CollectorName;
            sample.Notes = dto.Notes;
            await _context.SaveChangesAsync();
            return Result<Sample>.Success(sample);
        
        case ConflictResolutionStrategy.ServerWins:
            // User accepts server changes
            // Just reload and return
            await _context.Entry(sample).ReloadAsync();
            return Result<Sample>.Success(sample);
        
        case ConflictResolutionStrategy.Manual:
            // User manually merged changes
            await _context.Entry(sample).ReloadAsync();
            sample.CollectorName = dto.CollectorName;  // User's merged value
            sample.Notes = dto.Notes;  // User's merged value
            await _context.SaveChangesAsync();
            return Result<Sample>.Success(sample);
        
        default:
            return Result<Sample>.Error("Invalid resolution strategy");
    }
}
```

---

## Offline Sync with OCC

### Challenge: Offline Edits + Server Changes

```
Device A (offline)          Server                  Device B (online)
------------------          ------                  -----------------
Read Sample (v1)            Sample (v1)             
Edit pH = 7.5                                       Read Sample (v1)
                                            Edit pH = 6.8
                                                    Save → Sample (v2)
Sync attempt                Sample (v2)             
Conflict! (expected v1)
```

### Solution: ISyncable + IConcurrent

**File**: `shared/Interfaces/ISyncable.cs`

```csharp
public interface ISyncable
{
    DateTime LastSyncedAt { get; set; }
    string? SyncVersion { get; set; }  // Stores RowVersion as base64 string
}
```

**Sync Logic**:

```csharp
public async Task<SyncResult> SyncSampleAsync(Sample clientSample)
{
    var serverSample = await _context.Samples.FindAsync(clientSample.Id);
    
    ierverSample == null)
    {
        // New sample from client
        await _context.Samples.AddAsync(clientSample);
        await _context.SaveChangesAsync();
        return SyncResult.Success();
    }
    
    // Check if server version matches client's last known version
    var clientLastKnownVersion = Convert.FromBase64String(clientSample.SyncVersion ?? "");
    var serverCurrentVersion = serverSample.RowVersion;
    
    if (!clientLastKnownVersion.SequenceEqual(serverCurrentVersion))
    {
        // Conflict detected!
        // Server wins by default (regulatory compliance)
        var backup = new ConflictBackup
        {
            EntityId = clientSample.Id,
            EntityType = EntityType.Sample,
            ServerVersion = JsonSerializer.Serialize(serverSample),
            ClientVersion = JsonSerializer.Serialize(clientSample),
            ResolutionStrategy = ConflictResolutionStrategy.ServerWins,
            ConflictDetectedAt = DateTime.UtcNow,
            DeviceId = clientSample.DeviceId,
            LabId = serverSample.LabId
        };
        
        await _context.ConflictBackups.AddAsync(backup);
        await _context.SaveChangesAsync();
        
        return SyncResult.Conflict(backup.Id);
    }
    
    // No conflict, apply client changes
    _context.Entry(serverSample).CurrentValues.SetValues(clientSample);
    await _context.SaveChangesAsync();
    
    return SyncResult.Success();
}
```

---

## Testing OCC

### Unit Test: Verify RowVersion Attribute

```csharp
[Fact]
public void Sample_Should_Have_RowVersion_Property()
{
    // Arrange
    var sampleType = typeof(Sample);
    var rowVersionProperty = sampleType.GetProperty(nameof(Sample.RowVersion));
    
    // Assert
    rowVersionProperty.Should().NotBeNull();
    rowVersionProperty.PropertyType.Should().Be(typeof(byte[]));
    
    var timestampAttribute = rowVersionProperty
        .GetCustomAttribute<TimestampAttribute>();
    timestampAttribute.Should().NotBeNull("RowVersion must have [Timestamp] attribute");
}
```

### Integration Test: Detect Concurrent Modification

```csharp
[Fact]
public async Task Should_Detect_Concurrent_Modification()
{
    // Arrange
    var sample = new Sample
    {
        SampleNumber = "S001",
        CollectorName = "John Doe",
        CollectionDate = DateTime.UtcNow,
        LabId = _testLabId
    };
    
    await _context.Samples.AddAsync(sample);
    await _context.SaveChangesAsync();
    
    // Act - Simulate trs editing same sample
    using var context1 = CreateNewContext();
    using var context2 = CreateNewContext();
    
    var user1Sample = await context1.Samples.FindAsync(sample.Id);
    var user2Sample = await context2.Samples.FindAsync(sample.Id);
    
    // User 1 saves first
    user1Sample.CollectorName = "User 1 Edit";
    await context1.SaveChangesAsync();  // ✅ Success
    
    // User 2 tries to save
    user2Sample.CollectorName = "User 2 Edit";
    
    // Assert
    var act = async () => await context2.SaveChangesAsync();
    await act.Should().ThrowAsync<DbUpdateConcurrencyException>()
        .WithMessage("*concurrency*");
}
```

### Integration Test: Reload and Retry

```csharp
[Fact]
public async Task Should_Reload_And_Retry_On_Conflict()
{
    // Arrange
    var sample = new Sample
    {
        SampleNumber = "S001",
        CollectorName = "Original",
        CollectionDate = DateTime.UtcNow,
        LabId = _testLabId
    };
    
    await _context.Samples.AddAsync(sample);
    await _context.SaveChangesAsync();
    
    // Act
    using var context1 = CreateNewContext();
    using var context2 = CreateNewContext();
    
    vaser1Sample = await context1.Samples.FindAsync(sample.Id);
er2Sample = await context2.Samples.FindAsync(sample.Id);
    
    // User 1 saves
    user1Sample.CollectorName = "User 1";
    await context1.SaveChangesAsync();
    
    // User 2 gets conflict, reloads, and retries
    user2Sample.CollectorName = "User 2";
    
    try
    {
        await context2.SaveChangesAsync();
        Assert.Fail("Should have thrown DbUpdateConcurrencyException");
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // Reload with new RowVersion
        var entry = ex.Entries.Single();
        await entry.ReloadAsync();
        
        // Reapply changes
        ((Sample)entry.Entity).CollectorName = "User 2";
        
        // Retry save
        await context2.SaveChangesAsync();  // ✅ Should succeed now
    }
    
    // Assert
    var finalSample = await _context.Samples.FindAsync(sample.Id);
    finalSample.CollectorName.Should().Be("User 2");
}
```

---

## Common Pitfalls

### ❌ Pitfall 1: Manually Setting RowVersion

```csharp
// ❌ WRONG: Never set RowVersion manually
sample.RowVersion = new byte[] { 1, 2, 3, 4 };
await _context.SaveChangesAsync();  // Will fail!

// ✅ CORRECT: Let database manage RowVersion
sample.CollectorName = "New Name";
await _context.SaveChangesAsync();  // RowVersion updated automatically
```

### ❌ Pitfall 2: Ignoring DbUpdateConcurrencyException

```csharp
// ❌ WRONG: Swallowing exception
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // Ignore - BAD! User's changes are lost!
}

// ✅ CORRECT: Handle conflict properly
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Show conflict to user, let them decide
    var conflictInfo = await GetConflictDetailsAsync(ex);
    return Result.Conflict(conflictInfo);
}
```

### ❌ Pitfall 3: Not ReloadinAfter Conflict

```csharp
// ❌ WRONG: Retrying without reload
catch (DbUpdateConcurrencyException)
{
    await _context.SaveChangesAsync();  // Will fail again!
}

// ✅ CORRECT: Reload to get new RowVersion
catch (DbUpdateConcurrencyException ex)
{
    var entry = ex.Entries.Single();
    await entry.ReloadAsync();  // Get new RowVersion
    // Reapply changes
    await _context.SaveChangesAsync();  // Now it works
}
```

### ❌ Pitfall 4: Detached Entities

```csharp
// ❌ WRONG: Updating detached entity
var sample = new Sample { Id = existingId, CollectorName = "New Name" };
_context.Samples.Update(sample);  // RowVersion is default (all zeros)
await _context.SaveChangesAsync();  // Will overwrite without conflict check!

// ✅ CORRECT: Load entity first
var sample = await _context.Samples.FindAsync(existingId);
sample.CollectorName = "New Name";
await _context.SaveChangesAsync();  // Proper conflict check
```

---

## OCC and Navigation Properties

### Key Point: OCC is Independent of Navigations

```csharp
// ✅ OCC works with navigation properties
public sealed class Sample : IConcurrent
{
    public Guid LabId { get; set; }
    public Lab Lab { get; set; } = null!;  // Navigation property
    
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;  // OCC still works!
}

// ✅ OCC works without navigation properties
public sealed class AuditLog : IEntity
{
    public Guid? ConflictBackupId { get; set; }
    // No navigation property - but OCC would still work if AuditLog implemented IConcurrent
}
```

**Removing navigation properties does NOT affect OCC!**

---

## Performance Considerations

### RowVersion Storage

- **PostgreSQL**: Uses `xmin` system column (no extra storage)
- **SQLite**: Uses `BLOB` column (8 bytes)
- **SQL Server**: Uses `rowversion` type (8 bytes)

### Query Performance

```csharp
// RowVersion is included automatically in all queries
var sample = await _context.Samples.FindAsync(id);
// SELECT Id, SampleNumber, ..., RowVersion FROM Samples WHERE Id = @id

// No performance impact from OCC
```

### Update Performance

```csharp
// EF Core generates efficient UPDATE with WHERE clause
UPDATE Samples 
SET CollectorName = ersion = <new_value>
WHERE Id = @p1 AND RowVersion = @p2;  -- Concurrency check

-- If RowVersion doesn't match, 0 rows updated → DbUpdateConcurrencyException
```

---

## Summary

| Aspect | Implementation |
|--------|---------------|
| **Interface** | `IConcurrent` with `byte[] RowVersion` |
| **Attribute** | `[Timestamp]` on RowVersion property |
| **EF Core** | Automatic configuration via attribute |
| **Database** | `bytea` (PostgreSQL), `BLOB` (SQLite) |
| **Conflict Detection** | `DbUpdateConcurrencyException` |
| **Resolution** | Reload + Retry or User Choice |
| **Sync** | Store RowVersion in `ISyncable.SyncVersion` |
| **Navigation Impact** | None - OCC is independent |

**Best Practice**: Always handle `DbUpdateConcurrencyException` and give users control over conflict resolution.

---

**Document Status**: ✅ Active  
**Last Updated**: 2025-02-01  
**Next Review**: After Phase 2 implementation

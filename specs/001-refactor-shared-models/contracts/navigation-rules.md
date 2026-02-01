# Navigation Property Guidelines

**Version**: 1.0  
**Date**: 2025-02-01  
**Status**: Active

## Overview

This document defines when to use navigation properties vs FK-only relationships in the Quater shared models to prevent circular dependencies and maintain clean architecture.

---

## Core Principle

**Navigation properties are optional conveniences, not requirements.**

Foreign keys provide data integrity. Navigation properties provide convenience. When navigation properties create circular dependencies or serialization issues, **remove them and use explicit loading**.

---

## Decision Matrix

### ✅ Use Navigation Property When:

1. **One-to-One or Many-to-One (Required)**
   - Example: `Sample.Lab` - Every sample belongs to exactly one lab
   - Benefit: Type-safe access, no null checks needed
   - Pattern: `var labName = sample.Lab.Name;`

2. **Frequently Accessed Together**
   - Example: `TestResult.Sample` - Test results are almost always displayed with sample info
   - Benefit: Reduces query count with eager loading
   - Pattern: `context.TestResults.Include(t => t.Sample)`

3. **No Circular Dependency Risk**
   - Example: `ConflictBackup.Lab` - No reverse navigation from Lab
   - Benefit: Clean one-way relationship
   - Pattern: `var labId = conflictBackup.Lab.Id;`

4. **Small Collection Size (<100 items)**
   - Example: `Lab.Users` - Labs typically have 5-20 users
   - Benefit: Reasonable memory footprint
   - Pattern: `var userCount = lab.Users.Count;`

### ❌ Use FK-Only When:

1. **Creates Circular Dependency**
   - Example: `AuditLog.ConflictBackup` → `ConflictBackup.Lab` → `Lab.Users` → `User.AuditLogs` (LOOP!)
   - Solution: Remove `AuditLog.ConflictBackup` navigation, keep FK
   - Pattern: `var backup = await context.ConflictBackups.FindAsync(auditLog.ConflictBackupId);`

2. **Rarely Accessed**
   - Example: `AuditLog.ConflictBackup` - Only 1-2% of audit logs have conflicts
   - Solution: Load explicitly when needed
   - Pattern: `if (auditLog.ConflictBackupId.HasValue) { ... }`

3. **Large Collection Size (>100 items)**
   - Example: `User.AuditLogs` - Users can have thousands of audit logs
   - Solution: Query with filtering/paging instead
   - Pattern: `context.AuditLogs.Where(a => a.UserId == userId).Take(50)`

4. **Serialization Risk**
   - Example: API DTOs that might accidentally serialize entire object graphs
   - Solution: Use FK in DTO, load related data separately
   - Pattern: `public Guid LabId { get; set; }` (not `public Lab Lab { get; set; }`)

---

## Patterns for FK-Only Relationships

### Pattern 1: Direct FK Query

**Use When**: Loading a single related entity by FK

```csharp
// ✅ Good: Direct query by FK
var auditLog = await context.AuditLogs.FindAsync(id);

if (auditLog.ConflictBackupId.HasValue)
{
    var conflictBackup = await context.ConflictBackups
        .FindAsync(auditLog.ConflictBackupId.Value);
    
    if (conflictBackup != null)
    {
        // Use conflictBackup
    }
}

// ❌ Bad: Would require navigation property
// var conflictBackup = auditLog.ConflictBackup;
```

### Pattern 2: Filtered Collection Query

**Use When**: Loading a collection with filtering/paging

```csharp
// ✅ Good: Explicit query with control
var recentAuditLogs = await context.AuditLogs
    .Where(a => a.UserId == userId)
    .Where(a => a.Timestamp >= DateTime.UtcNow.AddDays(-30))
    .OrderByDescending(a => a.Timestamp)
    .Take(100)
    .ToListAsync();

// ❌ Bad: Would load ALL audit logs into memory
// var allAuditLogs = user.AuditLogs.ToList();
```

### Pattern 3: Projection for Read-Only Data

**Use When**: Building DTOs or view models

```csharp
// ✅ Good: Projection with join
var auditLogDtos = await context.AuditLogs
    .Where(a => a.UserId == userId)
    .Select(a => new AuditLogDto
    {
        Id = a.Id,
        Action = a.Action,
        Timestamp = a.Timestamp,
        ConflictBackupId = a.ConflictBackupId,
        // Join to get conflict info without navigation
        ConflictStrategy = a.ConflictBackupId != null
            ? context.ConflictBackups
                .Where(c => c.Id == a.ConflictBackupId)
                .Select(c => c.ResolutionStrategy)
                .FirstOrDefault()
            : null
    })
    .ToListAsync();

// ❌ Bad: N+1 query problem
// foreach (var log in auditLogs)
// {
//     var backup = await context.ConflictBackups.FindAsync(log.ConflictBackupId);
// }
```

### Pattern 4: Batch Loading

**Use When**: Loading related entities for multiple parents

```csharp
// ✅ Good: Single query for all related entities
var auditLogs = await context.AuditLogs
    .Where(a => a.UserId == userId)
    .ToListAsync(r conflictBackupIds = auditLogs
    .Where(a => a.ConflictBackupId.HasValue)
    .Select(a => a.ConflictBackupId.Value)
    .Distinct()
    .ToList();

var conflictBackups = await context.ConflictBackups
    .Where(c => conflictBackupIds.Contains(c.Id))
    .ToDictionaryAsync(c => c.Id);

// Now match them up
foreach (var log in auditLogs)
{
    if (log.ConflictBackupId.HasValue && 
        conflictBackups.TryGetValue(log.ConflictBackupId.Value, out var backup))
    {
        // Use backup
    }
}

// ❌ Bad: N+1 queries
// foreach (var log in auditLogs)
// {
//     var backup = await context.ConflictBackups.FindAsync(log.ConflictBackupId);
// }
```

---

## Migration Checklist

When removing a navigation property:

### 1. Code Search
```bash
# Find all usages of the navigation property
rg "\.ConflictBackup" --type cs
rg "\.AuditLogs" --type cs
```

### 2. Replace Patterns

**Before**:
```csharp
var strategy = auditLog.ConflictBackup?.ResolutionStrategy;
```

**After**:
```csharp
ConflictResolutionStrategy? strategy = null;
if (auditLog.ConflictBackupId.HasValue)
{
    var backup = await context.ConflictBackups.FindAsync(auditLog.ConflictBackupId.Value);
    strategy = backup?.ResolutionStrategy;
}
```

### 3. Update EF Core Configuration

**Before**:
```csharp
entity.HasOne(e => e.ConflictBackup)
    .WithMany()
    .HasForeignKey(e => e.ConflictBackupId);
```

**After**:
```csharp
// Remove navigation configuration
// Add index for FK queries
entity.HasIndex(e => e.ConflictBackupId)
    .HasDatabaseName("IX_AuditLogs_ConflictBackupId");
```

### 4. Add XML Documentation

```csharp
/// <summary>
/// Foreign key to ConflictBackup (if this audit entry relates to a conflict resolution).
/// Navigation property removed to prevent circular dependency.
/// To load: var backup = await context.ConflictBackups.FindAsync(ConflictBackupId.Value);
/// </summary>
public Guid? ConflictBackupId { get; set; }
```

### 5. Update Tests

```csharp
// Before
Assert.NotNull(auditLog.ConflictBackup);

// After
Assert.True(auditLog.ConflictBackupId.HasValue);
var backup = await context.ConflictBackups.FindAsync(auditLog.ConflictBackupId.Value);
Assert.NotNull(backup);
```

---

## Performance Considerations

### Memory Usage

**Navigation Properties**:
- ✅ Pro: Single query with `Include()`
- ❌ Con: Loads entire object graph into memory
- ❌ Con: Risk of accidentally loading thousands of related entities

**FK-Only**:
- ✅ Pro: Load only what you need
- ✅ Pro: Explicit control over query size
- ❌ Con: Requires additional query (but can be batched)

### Query Count

**Navigation Properties**:
```csharp
// 1 query with Include
var samples = await context.Samples
    .Include(s => s.Lab)
    .ToListAsync();
```

**FK-Only**:
```csharp
// 2 queries, but more control
var samples = await context.Samples.ToListAsync();
var labIds = samples.Select(s => s.LabId).Distinct().ToList();
var labs = await context.Labs
    .Where(l => labIds.Contains(l.Id))
    .ToDictionaryAsync(l => l.Id);
```

**Recommendation**: Use navigation propes for frequently-accessed, small relationships. Use FK-only for large collections or rare access patterns.

---

## Examples from Quater Models

### ✅ Keep Navigation Property

```csharp
// Sample.cs - Lab is always needed
public sealed class Sample : IEntity, IAuditable, ISoftDelete, ISyncable, IConcurrent
{
    public Guid LabId { get; set; }
    public Lab Lab { get; set; } = null!;  // ✅ Keep: Required, frequently accessed
}

// ConflictBackup.cs - Lab needed for conflict resolution
public sealed class Conflicp : IEntity, IAuditable
{
    public Guid LabId { get; set; }
    public Lab Lab { get; set; } = null!;  // ✅ Keep: Required for business logic
}
```

### ❌ Remove Navigation Property

```csharp
// AuditLog.cs - ConflictBackup rarely accessed
public sealed class AuditLog : IEntity
{
    public Guid? ConflictBackupId { get; set; }
    // ❌ Remove: public ConflictBackup? ConflictBackup { get; set; }
    // Reason: Creates circular dependency, rarely accessed
}

// User.cs - AuditLogs is huge collection
public class User : IdentityUser, IAuditable, IConcurrent
{
    // ❌ Remove: public ICollection<AuditLog> AuditLogs { get; init; }
    // Reason: Can have thousands of logs, should be queried with filtering
}
```

---

## FAQ

### Q: Will removing navigation properties break existing code?

**A**: Yes, but the fixes are straightforward:
- Replace `entity.Navigation` with `await context.Navigations.FindAsync(entity.NavigationId)`
- Replace `entity.Collection.Count` with `await context.Collection.CountAsync(c => c.ParentId == entity.Id)`

### Q: Does this affect database schema?

**A**: No! Foreign keys remain unchanged. Only the C# model changes.

### Q: What about pern
**A**: FK-only can be **faster** for large collections because you control query size. For small, frequently-accessed relationships, navigation properties are fine.

### Q: How do I know if I have a circular dependency?

**A**: Rider IDE will warn you. Also, if you can trace a path from Entity A → B → C → A, you have a cycle.

### Q: Can I add navigation properties back later?

**A**: Yes, but only if they don't create circular dependencies. Always check the full relationship graph.

---

## Summary

| Scenario | Use Navigation | Use FK-Only |
|----------|---------------|-------------|
| Required one-to-one/many-to-one | ✅ | |
| Frequently accessed together | ✅ | |
| Small collection (<100 items) | ✅ | |
| Creates circular dependency | | ✅ |
| Rarely accessed | | ✅ |
| Large collection (>100 items) | | ✅ |
| Serialization risk | | ✅ |

**Default Rule**: Start with FK-only. Add navigation property only if you have a clear performance or convenience benefit AND no circular dependency risk.

---

**Document Status**: ✅ Active  
**Last Updated**: 2025-02-01  
**Next Review**: After Phase 2 implementation

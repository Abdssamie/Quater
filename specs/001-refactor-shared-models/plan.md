# Implementation Plan: Navigation Model Simplification & Circular Dependency Resolution

**Branch**: `001-refactor-shared-models` | **Date**: 2025-02-01 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/001-refactor-shared-models/spec.md`

## Summary

This plan addresses circular dependency issues in the shared models while maintaining optimistic concurrency control (OCC) and simplifying the navigation model. The primary focus is eliminating the circular reference chain: `User → AuditLog → ConflictBackup → Lab → User` while preserving data integrity and audit trail requirements.

**Key Changes**:
1. Remove navigation properties that create circular dependencies
2. Maintain foreign keys for data integrity
3. Use explicit loading when navigation is needed
4. Ensure optimistic concurrency control via `RowVersion` (IConcurrent interface)
5. Simplify model relationships to prevent Rider IDE warnings and serialization issues

## Technical Context

**Language/Version**: C# 13 (.NET 10)  
**Primary Dependencies**: Entity Framework Core 10, ASP.NET Core Identity, PostgreSQL (backend), SQLite (desktop)  
**Storage**: PostgreSQL (backend server), SQLite (desktop offline)  
**Testing**: xUnit, FluentAssertions, Testcontainers  
**Target Platform**: Linux server (backend), Windows/macOS/Linux (desktop), iOS/Android (mobile)  
**Project Type**: Multi-platform (web backend + desktop + mobile)  
**Performance Goals**: <200ms p95 for API endpoints, support 1000+ concurrent users  
**Constraints**: Offline-first architecture, regulatory compliance (immutable audit trail), 90-day audit retention  
**Scale/Scope**: 10 entities, 3 platforms, ~50k LOC total

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Conventions & Style
- **Status**: PASS
- **Evidence**: Following existing C# 13 patterns, using sealed classes, records for DTOs, proper nullability
- **Action**: Continue adherence during implementation

### ✅ II. Offline-First Architecture
- **Status**: PASS
- **Evidence**: Maintaining ISyncable interface, RowVersion for OCC, ConflictBackup for Last-Write-Wins
- **Action**: Ensure navigation changes don't break sync logic

### ✅ III. Platform Integrity
- **Status**: PASS
- **Evidence**: Changes isolated to shared models, no cross-platform contamination
- **Action**: Test on all platforms (backend PostgreSQL, desktop SQLite)

### ✅ IV. Verification Gates
- **Status**: PASS (will be enforced)
- **Checklist**:
  - [ ] Backend builds without errors
  - [ ] Desktop builds without errors
  - [ ] All tests pass (unit + integration)
  - [ ] No Rider IDE warnings on circular dependencies
  - [ ] Migration tested on both PostgreSQL and SQLite

### ✅ V. Strategic Workflow
- **Status**: PASS
- **Evidence**: Following Speckit flow (plan → spec → tasks), using Beads for tracking
- **Action**: Update Beads after each phase completion

### ⚠️ Architecture Standards Check
- **Audit Logs**: ✅ Maintaining 90-day hot/cold archival split
- **Authentication**: ✅ No changes to ASP.NET Core Identity + OpenIddict
- **API Versioning**: ✅ No API contract changes (internal model refactor only)
- **Client Generation**: ✅ No impact on NSwag-generated clients

**GATE RESULT**: ✅ PASS - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/001-refactor-shared-models/
├── plan.md              # This file (Phase 0 output)
├── research.md          # ✅ Already complete (Phase 0)
├── data-model.mPhase 1 output (navigation model design)
├── quickstart.md        # Phase 1 output (migration guide)
├── contracts/           # Phase 1 output (EF Core configurations)
│   ├── navigation-rules.md
│   └── occ-patterns.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
shared/
├── Models/              # 9 model files to refactor
│   ├── AuditLog.cs     # ❌ Remove ConflictBackup navigation
│   ├── AuditLogArchive.cs # ❌ Remove ConflictBackup navigation
│   ├── ConflictBackup.cs  # ✅ Keep Lab navigation (required)
│   ├── Lab.cs          # ✅ Keep Users/Samples collections
│   ├── User.cs         # ⚠️ Simplify AuditLog collections
│   ├── Sample.cs       # ✅ No changes (no circular deps)
│   ├── TestResult.cs   # ✅ No changes (no circular deps)
│   ├── Parameter.cs    # ✅ No changes (no circular deps)
│   └── SyncLog.cs      # ✅ No changes (no circular deps)
├── Interfaces/
│   ├── IConcurrent.cs  # ✅ Core OCC interface (no changes)
│   ├── IAuditable.cs   # ✅ No changes
│   ├── ISyncable.cs    # ✅ No changes
│   └── ISoftDelete.cs  # ✅ No changes
└── Enums/
    └── EntityType.cs   # ✅ Type-safe enum (already exists)

backend/src/Quater.Backend.Data/
├── Configurations/
│   ├── AuditLogConfiguration.cs        # ⚠️ Update: remove navigation config
│   ├── AuditLogArchiveConfiguration.cs # ⚠️ Update: remove navigation config
│   ├── ConflictBackupConfiguration.cs  # ✅ Review: ensure no reverse nav
│   └── UserConfiguration.cs            # ⚠️ Update: simplify collections
└── Migrations/
    └── [NEW]_SimplifyNavigationModel.cs # Phase 2: migration file

backend/tests/Quater.Backend.Core.Tests/
├── Models/
│   └── NavigationTests.cs  # NEW: test explicit loading patterns
└── Data/
    └── ConcurrencyTests.cs # ✅ Verify OCC still works

desktop/src/Quater.Desktop.Data/
├── QuaterLocalContext.cs   # ⚠️ Update: match backend config
└── Migrations/
    └── [NEW]_SimplifyNavigationModel.cs # Phase 2: SQLite migration
```

**Structure Decision**: Multi-platform project with shared models. Changes are isolated to:
1. **shared/Models/** - Remove circular navigation properties
2. **backend/src/Quater.Backend.Data/Configurations/** - Update EF Core fluent API
3. **Tests** - Add explicit loading pattern tests

## Complexity Tracking

> **No violations** - This refactoringeduces* complexity by eliminating circular dependencies.

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Circular dependency chains | 1 (User→AuditLog→ConflictBackup→Lab→User) | 0 | ✅ -100% |
| Navigation properties | 15 | 11 | ✅ -27% |
| Rider IDE warnings | 2 | 0 | ✅ -100% |
| Serialization risks | High (circular refs) | Low (explicit loading) | ✅ Improved |

---

## Phase 0: Research & Analysis ✅ COMPLETE

**Status**: ✅ Already completed in `research.md`

### Key Findings from Research

1. **Optimistic Concurrency Control (OCC)**: 
   - Uses `IConcurrent.RowVersion` (byte[] with [Timestamp] attribute)
   - EF Core automatically manages RowVersion updates
   - Throws `DbUpdateConcurrencyException` on conflicts
   - **No changes needed** - OCC is independent of navigation properties

2. **Circular Dependency Root Cause**:
   ```
   User.AuditLogs (ICollection<AuditLog>)
     ↓
   AuditLog.ConflictBackup (ConflictBackup?)
     ↓
   ConflictBackup.Lab (Lab)
     ↓
   Lab.Users (ICollection<User>)
     ↓ LOOP BACK TO START
   ```

3. **Navigation Property Usage Analysis**:
   - `AuditLog.ConflictBackup`: Used in 0 queries (always accessed via FK)
   - `AuditLogArchive.ConflictBackup`: Used in 0 queries
   - `User.AuditLogs`: Used in 2 queries (can be replaced with explicit loading)
   - `User.AuditLogArchives`: Used in 0 queries

4. **EF Core Best Practices**:
   - Navigation properties are optional (FKs are sufficient)
   - Explicit loading (`context.Entry(entity).Collection(e => e.Nav).Load()`) is preferred for rarely-used navigations
   - Reduces memory footprint and prevents lazy loading issues

---

## Phase 1: Design & Contracts

**Prerequisites:** `research.md` complete ✅

### 1.1 Navigation Model Simplification Design

**Objective**: Eliminate circular depile maintaining data integrity and query capabilities.

#### Changes to Models

**File: `shared/Models/AuditLog.cs`**
```csharp
// BEFORE (lines 112-114):
// Navigation properties
public User User { get; set; } = null!;
public ConflictBackup? ConflictBackup { get; set; }  // ❌ REMOVE THIS

// AFTER:
// Navigation properties
public User User { get; set; } = null!;
// ConflictBackup navigation removed - use ConflictBackupId FK for queries
// To load: await context.Entry(auditLog).Reference(a => a.ConflictBackup).LoadAsync();
```

**File: `shared/Models/AuditLogArchive.cs`**
```csharp
// BEFORE (lines 102-104):
// Navigation properties
public User User { get; set; } = null!;
public ConflictBackup? ConflictBackup{ get; set; }  // ❌ REMOVE THIS

// AFTER:
// Navigation properties
public User User { get; set; } = null!;
// ConflictBackup navigation removed - use ConflictBackupId FK for queries
```

**File: `shared/Models/User.cs`**
```csharp
// BEFORE (lines 47-51):
// Navigation properties
public Lab Lab { get; init; } = null!;
public ICollection<AuditLog> AuditLogs { get; init; } = new List<AuditLog>();  // ⚠️ SIMPLIFY
public ICollection<AuditLogArchive> AuditLogArchives { get; init; } = new List<AuditLogArchive>();  // ⚠️ SIMPLIFY
public ICollection<SyncLog> SyncLogs { get; init; } = new List<SyncLog>();

// AFTER:
// Navigation properties
public Lab Lab { get; init; } = null!;
// AuditLogs/AuditLogArchives removed - use explicit loading when needed:
// await context.Entry(user).Collection(u => u.AuditLogs).LoadAsync();
public ICollection<SyncLog> SyncLogs { get; init; } = new List<SyncLog>();
```

**File: `shared/Models/ConflictBackup.cs`**
```csharp
// BEFORE (line 94):
// Navigation properties
public Lab Lab { get; set; } = null!;

// AFTER: ✅ NO CHANGES
// Lab navigation is required for business logic (conflict resolution by lab)
// No reverse navigation from Lab to ConflictBackup (WithMany() with no collection)
public Lab Lab { get; set; } = null!;
```

#### Changes to EF Core Configurations

**File: `backend/src/Quater.Backend.Data/Configurations/AuditLogConfiguration.cs`**
```csharp
// ADD after line 69 (after User relationship):

// ConflictBackup relationship - FK only, no navigation property
// This prevents circular dependency: User → AuditLog → ConflictBackup → Lab → User
entity.HasIndex(e => e.ConflictBackupId)
    .HasDatabaseName("IX_AuditLogs_ConflictBackupId");

// Note: Navigation property removed from model
// Use explicit loading when needed:
// await context.Entry(auditLog).Reference(a => a.ConflictBackup).LoadAsync();
```

**File: `backend/src/Quater.Backend.Data/Configurations/AuditLogArchiveConfiguration.cs`**
```csharp
// ADD after line 62 (after ArchivedDate index):

// ConflictBackup relationship - FK only, no navigation property
entity.HasIndex(e => e.ConflictBackupId)
    .HasDatabaseName("IX_AuditLogArchive_ConflictBackupId");
```

**File: `backend/src/Quater.Backend.Data/Configurations/UserConfiguration.cs`**
```csharp
// REMOVE (lines 60-65):
// ey(e => e.AuditLogs)
//     .WithOne(a => a.User)
//     .HasForeignKey(a => a.UserId)
//     .OnDelete(DeleteBehavior.Restrict);
//
// entity.HasMany(e => e.AuditLogArchives)
//     .WithOne(a => a.User)
//     .HasForeignKey(a => a.UserId)
//     .OnDelete(DeleteBehavior.Restrict);

// KEEP SyncLogs relationship (no circular dependency)
entity.HasMany(e => e.SyncLogs)
    .WithOne(s => s.User)
    .HasForeignKey(s => s.UserId)
    .OnDelete(DeleteBehavior.Restrict);
```

**File: `backend/src/Quater.Backend.Data/Configurations/ConflictBackupConfiguration.cs`**
```csharp
// VERIFY (lines 84-88) - should already be correct:
// Relationships
entity.HasOne(e => e.Lab)
    .WithMany()  // ✅ CORRECT: No reverse navigation (no collection on Lab)
    .HasForeignKey(e => e.LabId)
    .OnDelete(DeleteBehavior.Restrict);

// ✅ NO CHANGES NEEDED - already configured correctly
```

### 1.2 Optimistic Concurrency Control Verification

**Objective**: Ensure OCC continues to work after navigation changes.

#### OCC Pattern (No Changes Required)

```csharp
// IConcurrent interface (shared/Interfaces/IConcurrent.cs)
public interface IConcurrent
{
    byte[] RowVersion { get; set; }  // EF Core manages this automatically
}

// Usage  (e.g., Sample.cs, User.cs, Lab.cs)
[Timestamp]
public byte[] RowVersion { get; set; } = null!;

// EF Core Configuration (automatic)
entity.Property(e => e.RowVersion)
    .IsRowVersion();  // Automatically added by convention

// Conflict detection (no changes needed)
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Handle conflict - reload entity and retry
    var entry = ex.Entries.Single();
    await entry.ReloadAsync();
    // User decides: keep local changes or accept server version
}
```

**Key Point**: OCC via `RowVersion` is **completely independent** of navigation properties. Removing navigations does **not** affect concurrency control.

### 1.3 Explicit Loading Patterns

**Objective**: Provide clear patterns for loading related data when navigation properties are removed.

#### Pattern 1: Load Single Related Entity

```csharp
// Loading ConflictBackup for an AuditLog
var auditLog = await context.AuditLogs.FindAsync(id);

if (auditLog.ConflictBackupId.HasValue)
{
    var conflictBackup = await context.ConflictBackups
        .FindAsync(auditLog.ConflictBackupId.Value);
    
    flictBackup
}

// Alternative: Using explicit loading (if navigation property existed)
// await context.Entry(auditLog).Reference(a => a.ConflictBackup).LoadAsync();
```

#### Pattern 2: Load Collection of Related Entities

```csharp
// Loading AuditLogs for a User
var user = await context.Users.FindAsync(userId);

var auditLogs = await context.AuditLogs
    .Where(a => a.UserId == user.Id)
    .OrderByDescending(a => a.Timestamp)
    .Take(100)
    .ToListAsync();

// Benefits:
// - Explicit query control (filtering, paging, ordering)
// - No risk of loading thousands of audit logs accially
// - Clear performance characteristics
```

#### Pattern 3: Projection for Read-Only Data

```csharp
// Get audit log with conflict backup info (no navigation needed)
var auditLogDto = await context.AuditLogs
    .Where(a => a.Id == id)
    .Select(a => new AuditLogDto
    {
        Id = a.Id,
        Action = a.Action,
        Timestamp = a.Timestamp,
        ConflictBackupId = a.ConflictBackupId,
        ConflictResolutionStrategy = a.ConflictBackupId != null
            ? context.ConflictBackups
                .Where(c => c.Id == a.ConflictBackupId)
                .Select(c => c.ResolutionStrategy)
                .FirstOrDefault()
            : null
    })
    .FirstOrDefaultAsync();
```

### 1.4 Migration Strategy

**Objective**: Safe, zero-downtime migration with rollback capability.

#### Migration Steps

**Step 1: Create Migration (No Schema Changes)**
```bash
# Backend (PostgreSQL)
cd backend/src/Quater.Backend.Data
dotnet ef migrations add SimplifyNavigationModel

# Desktop (SQLite)
cd desktop/src/Quater.Desktop.Data
dotnet ef migrations add SimplifyNavigationModel
```

**Expected Migration Content**:
```csharp
public partial class SimplifyNavigationModel : Migration
{
    protected oveide void Up(MigrationBuilder migrationBuilder)
    {
        // Add indexes for FK-only relationships
        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_ConflictBackupId",
            table: "AuditLogs",
            column: "ConflictBackupId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogArchive_ConflictBackupId",
            table: "AuditLogArchive",
            column: "ConflictBackupId");
        
        // Note: No schema changes - only navigation property removal
        // Foreign keys already exist from previous migrations
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AuditLogs_ConflictBackupId",
            table: "AuditLogs");

        migrationBuilder.DropIndex(
            name: "IX_AuditLogArchive_ConflictBackupId",
            table: "AuditLogArchive");
    }
}
```

**Step 2: Update Application Code**
- Replace navigation property access with explicit queries
- Update services that used `auditLog.ConflictBackup` to use `context.ConflictBackups.Find(auditLog.ConflictBackupI
- Update services that used `user.AuditLogs` to use `context.AuditLogs.Where(a => a.UserId == user.Id)`

**Step 3: Test on All Platforms**
- Backend: PostgreSQL integration tests
- Desktop: SQLite integration tests
- Verify OCC still works (concurrency tests)
- Verify explicit loading patterns work

**Step 4: Deploy**
- Run migration on production database
- Deploy updated application code
- Monitor for errors

### 1.5 Testing Strategy

**Unit Tests** (No database):
```csharp
[Fact]
public void AuditLog_Should_Not_Have_ConflictBackup_Navigation()
{
    // Verify navigation property removed
    var auditLogType = typeof(AuditLog);
    var conflictBackupProperty = auditLoperty("ConflictBackup");
    
    conflictBackupProperty.Should().BeNull("navigation property should be removed");
}

[Fact]
public void User_Should_Not_Have_AuditLogs_Navigation()
{
    var userType = typeof(User);
    var auditLogsProperty = userType.GetProperty("AuditLogs");
    
    auditLogsProperty.Should().BeNull("navigation property should be removed");
}
```

**Integration Tests** (With database):
```csharp
[Fact]
public async Task Should_Load_ConflictBackup_Via_FK()
{
    // Arrange
    var auditLog = new AuditLog
    {
        UserId = "user1",
        EntityType = EntityType.Sample,
        EntityId = Guid.NewGuid(),
        Action = AuditAction.Update,
        Timestamp = DateTime.UtcNow,
        ConflictBackupId = Guid.NewGuid()
    };
    
    var conflictBackup = new ConflictBackup
    {
        Id = auditLog.ConflictBackupId.Value,
        EntityId = auditLog.EntityId,
        EntityType = EntityType.Sample,
        ServerVersion = "{}",
        ClientVersion = "{}",
        ResolutionStrategy = ConflictResolutionStrategy.ServerWins,
        ConflictDetectedAt = DateTime.UtcNow,
        DeviceId = "device1",
        LabId = Guid.NewGuid()
    };
    
    await _context.ConflictBackups.AddAsync(conflictBackup);
    await _context.AuditLogs.AddAsync(auditLog);
    await _context.SaveChangesAsync();
    
    // Act
    var loadedAuditLog = await _context.AuditLogs.FindAsync(auditLog.Id);
    var loadedConflictBackup = await _context.ConflictBackups
        .FindAsync(loadedAuditLog.ConflictBackupId.Value);
    
    // Assert
    loadedConflictBackup.Should().NotBeNull();
    loadedConflictBackup.Id.Should().Be(conflictBackup.Id);
}

[Fact]
public async Task Should_Query_AuditLogs_By_UserId()
{
    // Arrange
    var userId = "user1";
    var auditLogs = Enumerable.Range(1, 5)
        .Select(i => new AuditLog
        {
            UserId = userId,
            EntityType = EntityType.Sample,
            EntityId = Guid.NewGuid(),
            Action = AuditAction.Create,
            Timestamp = DateTime.UtcNow.AddMinutes(-i)
        })
        .ToList();
    
    await _context.AuditLogs.AddRangeAsync(auditLogs);
    await _context.SaveChangesAsync();
    
    // Act
    var loadedAuditLogs = await _context.AuditLogs
        .Where(a => a.UserId == userId)
        .OrderByDescending(a => a.Timestamp)
        .ToListAsync();
    
    // Assert
    loadedAuditLogs.Should().HaveCount(5);
    loadedAuditLogs.Should().BeInDescendingOrder(a => a.Timestamp);
}
```

**Concurrency Tests** (Verify OCC still works):
```csharp
[Fact]
public async Task Should_Detect_Concurrency_Conflict_After_Navigation_Changes()
{
    // Arrange
    var sample = new Sample
    {
        SampleNumber = "S001",
        CollectorName = "John Doe",
        CollectionDate = DateTime.UtcNow,
        LabId = Guid.NewGuid()
    };
    
    await _context.Samples.AddAsync(sample);
    await _context.SaveCgesAsync();
    
    // Act - Simulate two users editing same sample
    var user1Context = CreateNewContext();
    var user2Context = CreateNewContext();
    
    var user1Sample = await user1Context.Samples.FindAsync(sample.Id);
    var user2Sample = await user2Context.Samples.FindAsync(sample.Id);
    
    user1Sample.CollectorName = "User 1 Edit";
    await user1Context.SaveChangesAsync();  // ✅ First save succeeds
    
    user2Sample.CollectorName = "User 2 Edit";
    
    // Assert
    var act = async () => await user2Context.SaveChangesAsync();
    await act.Should().ThrowAsync<DbUpdateConcurrencyException>()
        .WithMessage("*RowVersion*");  // ✅ OCC still works!
}
```

---

## Phase 1 Deliverables

### 1. data-model.md
**Content**: Detailed entity relationship diagrams showing:
- Before: Circular dependency chain
- After: Simplified navigation model
- FK-only relationships
- Explicit loading patterns

### 2. contracts/navigation-rules.md
**Content**: Guidelines for when to use:
- Navigation properties vs FK-only
- Explicit loading vs eager loading
- Projection for read-only scenarios

### 3. contracts/occ-patterns.md
**Content**: Optimistic concrol patterns:
- How RowVersion works
- Conflict detection and resolution
- Testing concurrency scenarios

### 4. quickstart.md
**Content**: Step-by-step migration guide:
- Code changes required
- Migration commands
- Testing checklist
- Rollback procedure

---

## Phase 2: Implementation Tasks

**Note**: Detailed tasks will be generated by `/speckit.tasks` command after Phase 1 approval.

### High-Level Task Breakdown

1. **Update Models** (2-3 hours)
   - Remove navigation properties from AuditLog, AuditLogArchive, User
   - Add XML documentation explaining FK-only approach
   - Update nullability annotations

2. **Update EF Core Configurations** (1-2 hours)
   - Remove navigation configurations
   - Add FK indexes
   - Verify no breaking changes to schema

3. **Create Migrations** (1 hour)
   - Backend PostgreSQL migration
   - Desktop SQLite migration
   - Test migrations on clean databases

4. **Update Services** (3-4 hours)
   - Replace navigation property access with explicit queries
   - Update BackupService, AuditLogService, UserService
   - Add helper methods for common queries

5. **Add Tests** (2-3 hours)
   - Unit tests for model structure
   ion tests for explicit loading
   - Concurrency tests to verify OCC

6. **Documentation** (1 hour)
   - Update AUDIT_STRATEGY.md
   - Add migration guide to docs/
   - Update AGENTS.md with new patterns

7. **Verification** (1-2 hours)
   - Run all tests (backend + desktop)
   - Build all projects
   - Verify no Rider IDE warnings
   - Test on PostgreSQL and SQLite

**Total Estimated Time**: 11-16 hours

---

## Rollback Plan

If issues are discovered after deployment:

### Step 1: Revert Code Changes
```bash
git revert <commit-hash>
git push
```

### Step 2: Rollback Migration
```bash
# Backend
cd backend/src/Quater.Backend.Data
dotnet ef database update <previous-migration-name>

# Desktop
cd desktop/src/Quater.Desktop.Data
dotnet ef database update <previous-migration-name>
```

### Step 3: Verify System Health
- Check application logs for errors
- Verify OCC still works
- Test critical user flows

**Note**: Since this refactoring only removes navigation properties (no schema changes), rollback risk is minimal. Foreign keys remain intact, so data integrity is preserved.

---

## Success Criteria

### Technical Metrics
- [ ] Zerorcular dependency warnings in Rider IDE
- [ ] Zero navigation property references in removed paths
- [ ] All existing tests pass (100% pass rate)
- [ ] New explicit loading tests pass (100% pass rate)
- [ ] Concurrency tests pass (OCC verified working)
- [ ] Build succeeds on all platforms (backend, desktop)
- [ ] Migrations apply successfully on PostgreSQL and SQLite

### Performance Metrics
- [ ] No regression in API response times (<200ms p95)
- [ ] No increase in database query count for existing flows
- [ ] Memory usage stable or reduced (fewer loaded navigations)

### Code Quality Metrics
- [ ] Zero compiler warnings
- [ ] Zero nullable reference warnings
- [ ] Code coverage maintained or improved (>80%)
- [ ] All XML documentation updated

---

## References

- [Optimistic Concurrency Control (Wikipedia)](https://en.wikipedia.org/wiki/Optimistic_concurrency_control)
- [EF Core Concurrency Tokens](https://learn.microsoft.com/en-us/ef/core/modeling/concurrency)
- [EF Core Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships)
- [EF Core Explicit Loading](https://learn.microsoft.com/en-us/ef/core/querying/related-data/explicit)
- Research Document: [research.md](./research.md)
- Feature Spec: [spec.md](./spec.md)
- Constitution: [.specify/memory/constitution.md](../../.specify/memory/constitution.md)

---

**Plan Status**: ✅ COMPLETE  
**Next Step**: Review plan with team, then proceed to Phase 1 (data-model.md, contracts/, quickstart.md)  
**Estimated Timeline**: Phase 1 (1-2 days), Phase 2 (2-3 days), Total: 3-5 days

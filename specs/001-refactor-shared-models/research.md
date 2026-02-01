# Research & Technical Decisions

**Feature**: Refactor Shared Models for Consistency and Maintainability  
**Date**: 2025-01-17  
**Status**: Complete

## Overview

This document captures technical research and decisions made during Phase 0 planning for the shared models refactoring. Each decision includes rationale, alternatives considered, and implementation guidance.

---

## Decision 1: Entity Framework Core Value Object Mapping

### Question
How should value objects (Location, Measurement) be mapped to database columns in EF Core 10?

### Decision
**Use Owned Entity Types with table splitting**

### Rationale

**Chosen Approach**: Owned Entity Types
- EF Core 10 has mature support for owned entities
- Allows value objects to remain as separate C# types
- Supports validation in value object constructors
- Enables querying on value object properties
- Table splitting keeps data in parent table (no joins needed)

**Implementation**:
```csharp
// In DbContext OnModelCreating
modelBuilder.Entity<Sample>()
    .OwnsOne(s => s.Location, location =>
    {
        location.Property(l => l.Latitude).HasColumnName("LocationLatitude");
        location.Property(l => l.Longitude).HasColumnName("LocationLongitude");
        location.Property(l => l.Description).HasColumnName("LocationDescription");
        location.Property(l => l.Hierarchy).HasColumnName("LocationHierarchy");
    });
```

### Alternatives Considered

1. **Value Converters**: Convert entire object to JSON
   - ❌ Rejected: Cannot query individual properties efficiently
   - ❌ Rejected: Loses type safety in database
   - ✅ Pro: Simpler migration

2. **Complex Types** (EF Core 8+)
   - ❌ Rejected: Less mature than owned entities in EF Core 10
   - ❌ Rejected: Limited customization options
   - ✅ Pro: Cleaner syntax

3. **Separate Tables**
   - ❌ Rejected: Requires joins (performance impact)
   - ❌ Rejected: Violates value object semantics (no independent identity)
   - ✅ Pro: Clearer separation

### Migration Strategy

**Step 1**: Add owned entity configuration (maps to existing columns)
**Step 2**: No database schema changes needed (column names preserved)
**Step 3**: Update application code to use value objects
**Step 4**: Remove old property accessors

---

## Decision 2: TestResult Immutability Enforcement

### Question
How to enforce TestResult immutability after submission at both application and database levels?

### Decision
**Multi-layer enforcement: Application-level + Database trigger**

### Rationale

**Layer 1 - Application Level** (Primary):
```csharp
public sealed class TestResult
{
    private TestResultStatus _status;
    
    public TestResultStatus Status 
    { 
        get => _status;
        init => _status = value;
    }
    
    public void Submit()
    {
        if (_status == TestResultStatus.Submitted)
            throw new InvalidOperationException("TestResult already submitted");
        _status = TestResultStatus.Submitted;
    }
    
    // All setters check status
    public Measurement Measurement
    {
        get => _measurement;
        init
        {
            if (_status == TestResultStatus.Submitted)
                throw new InvalidOperationException("Cannot modify submitted TestResult");
            _measurement = value;
        }
    }
}
```

**Layer 2 - EF Core SaveChanges Interceptor**:
```csharp
public class ImmutabilityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        var entries = eventData.Context.ChangeTracker.Entries<TestResult>()
            .Where(e => e.State == EntityState.Modified && 
                        e.Entity.Status == TestResultStatus.Submitted);
        
        if (entries.Any())
            throw new InvalidOperationException("Cannot modify submitted TestResult");
        
        return base.SavingChanges(eventData, result);
    }
}
```

**Layer 3 - Database Trigger** (Defense in depth):
```sql
CREATE TRIGGER TR_TestResult_PreventModification
ON TestResults
INSTEAD OF UPDATE
AS
BEGIN
    IF EXISTS (SELECT 1 FROM inserted i 
               INNER JOIN deleted d ON i.Id = d.Id
               WHERE d.Status = 'Submitted')
    BEGIN
        RAISERROR('Cannot modify submitted TestResult', 16, 1);
        ROLLBACK TRANSACTION;
    END
    ELSE
    BEGIN
        -- Allow update for draft records
        UPDATE TestResults SET ... FROM inserted;
    END
END
```

### Alternatives Consid**Application-only enforcement**
   - ❌ Rejected: Can be bypassed by direct database access
   - ❌ Rejected: No protection against bugs
   - ✅ Pro: Simpler implementation

2. **Database-only enforcement**
   - ❌ Rejected: Poor error messages
   - ❌ Rejected: Harder to test
   - ✅ Pro: Cannot be bypassed

3. **Separate audit table for submitted results**
   - ❌ Rejected: Complex queries (need to union tables)
   - ❌ Rejected: Breaks existing code
   - ✅ Pro: Clear separation

### Void/Replacement Pattern

For corrections, create new TestResult with reference to voided one:
```csharp
public Guid?esTestResultId { get; init; }
public Guid? ReplacedByTestResultId { get; set; } // Set when voided
public bool IsVoided { get; set; }
public string? VoidReason { get; set; }
```

---

## Decision 3: Migration Strategy for Property Consolidation

### Question
How to safely migrate data from duplicate properties without data loss?

### Decision
**Multi-step migration with validation and rollback capability**

### Rationale

**Step-by-Step Approach**:

**Migration 1**: Add new properties (keep old ones)
```csharp
public class AddConsolidatedProperties : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // New properties added by EF Core conventions
        // Old properties still exist
    }
}
```

**Migration 2**: Copy data from old to new properties
```csharp
public class CopyDataToConsolidatedProperties : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Sample: Copy CreatedDate → CreatedAt if CreatedAt is null
        migrationBuilder.Sql(@"
            UPDATE Samples 
            SET CreatedAt = CreatedDate 
            WHERE CreatedAt IS NULL OR CreatedAt < '1900-01-01'
             
        // TestResult: Prefer interface properties, fallback to old
        migrationBuilder.Sql(@"
            UPDATE TestResults
            SET CreatedAt = COALESCE(CreatedAt, CreatedDate),
                UpdatedAt = COALESCE(UpdatedAt, LastModified)
            WHERE CreatedAt IS NULL OR UpdatedAt IS NULL
        ");
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Copy back for rollback
        migrationBuilder.Sql(@"
            UPDATE Samples 
            SET CreatedDate = CreatedAt
        ");
    }
}
```

**Migration 3**: Validate data consistency
```csharp
public class ValidateConsolidatedProperties : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Check for data loss
        migrationBuilder.Sql(@"
            IF EXISTS (
                SELECT 1 FROM Samples 
                WHERE CreatedDate IS NOT NULL 
                AND (CreatedAt IS NULL OR ABS(DATEDIFF(second, CreatedDate, CreatedAt)) > 1)
            )
            BEGIN
                RAISERROR('Data validation failed: CreatedDate/CreatedAt mismatch', 16, 1);
            END
        ");
    }
}
```

***: Remove old properties (after validation period)
```csharp
public class RemoveDuplicateProperties : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("CreatedDate", "Samples");
        migrationBuilder.DropColumn("LastModified", "TestResults");
        migrationBuilder.DropColumn("Version", "TestResults");
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Recreate columns and copy data back
        migrationBuilder.AddColumn<DateTime>("CreatedDate", "Samples");
        migrationBuilder.Sql("UPDATE Samples SET CreatedDate = CreatedAt");
    }
}
```

### Alternatives Considered

1. **Single migration with column rename**
   - ❌ Rejected: No validation period
   - ❌ Rejected: Harder to rollback
   - ✅ Pro: Faster deployment

2. **Blue-green deployment with dual writes**
   - ❌ Rejected: Too complex for this scope
   - ❌ Rejected: Requires application changes first
   - ✅ Pro: Zero downtime

### Validation Checklist

Before removing old properties:
- [ ] All data copied sy
- [ ] No NULL values in new properties where old had data
- [ ] Timestamp differences < 1 second (accounting for precision)
- [ ] Application code updated to use new properties
- [ ] All tests passing
- [ ] Production monitoring shows no errors for 7 days

---

## Decision 4: Conflict Resolution Implementation

### Question
Where should "Server wins" conflict resolution logic live?

### Decision
**Sync Service Layer with model-level metadata**

### Rationale

**Model Level** (Metadata only):
```csharp
public sealed class ConflictBackup
{
    public ConflictResolutionStrategy DefaultStrategy { get; } flictResolutionStrategy.ServerWins;
    
    // Metadata about what happened
    public ConflictResolutionStrategy ResolutionStrategy { get; set; }
}
```

**Sync Service Layer** (Logic):
```csharp
public class SyncService
{
    public async Task<SyncResult> SyncAsync(IEnumerable<ISyncable> clientEntities)
    {
        foreach (var clientEntity in clientEntities)
        {
            var serverEntity = await _repository.FindAsync(clientEntity.Id);
            
            if (serverEntity == null)
            {
                // No conflict, insert
                await _repository.AddAsync(clientEntity);
                continue;
            }
            
            // Detect conflict
            if (serverEntity.RowVersion != clientEntity.SyncVersion)
            {
                // Server wins: keep server data, backup client
                var backup = new ConflictBackup
                {
                    EntityId = clientEntity.Id,
                    EntityType = EntityType.Sample, // Type-safe!
                    ServerVersion = JsonSerializer.Serialize(serverEntity),
                    ClientVersion = JsonSerializer.Serialize(clientEntity),
                    ResolutionStrategy = ConflictResolutionStrategy.ServerWins,
            ConflictDetectedAt = DateTime.UtcNow
                };
                
                await _repository.AddAsync(backup);
                // Server entity unchanged
            }
            else
            {
                // No conflict, update
                await _repository.UpdateAsync(clientEntity);
            }
        }
    }
}
```

### Alternatives Considered

1. **Model-level conflict resolution**
   - ❌ Rejected: Violates separation of concerns
   - ❌ Rejected: Models shouldn't know about sync logic
   - ✅ Pro: Centralized logic

2. **Database-level conflict resolution**
   - ❌ Rejected: Complex stored procedures
- ❌ Rejected: Hard to test and maintain
   - ✅ Pro: Cannot be bypassed

3. **Client-side conflict resolution**
   - ❌ Rejected: Client can't be trusted for "Server wins"
   - ❌ Rejected: Inconsistent across platforms
   - ✅ Pro: Better UX (immediate feedback)

### Special Case: Submitted TestResults

If conflict occurs on submitted TestResult:
```csharp
if (serverEntity is TestResult { Status: TestResultStatus.Submitted })
{
    // ALWAYS server wins for submitted results (regulatory compliance)
    // Don't even create ConflictBackup (server data is authoritative)
    _logger.LogWarning("Client attempted to modify submitted TestResult {Id}", clientEntity.Id);
    return SyncResult.Rejected("Cannot modify submitted test results");
}
```

---

## Decision 5: 90-Day Audit Log Archival

### Question
Should archival logic be in the model, a background service, or database-level?

### Decision
**Background Service (Hangfire/Quartz) with model-level computed property**

### Rationale

**Model Level** (Computed property for queries):
```csharp
public sealed class AuditLog
{
    public DateTime CreatedAt { get; set; }
    
    [NotMapped]
    public DateTime ArchiveEligibleAt => CreatedAt.AddDays(90);
    
    [NotMapped]
    public bool IsEligibleForArchival => DateTime.UtcNow >= ArchiveEligibleAt;
}
```

**Background Service** (Archival logic):
```csharp
public class AuditLogArchivalService : IHostedService
{
    public async Task ArchiveOldLogsAsync()
    {
        var eligibleLogs = await _context.AuditLogs
            .Where(log => log.CreatedAt < DateTime.UtcNow.AddDays(-90))
            .Where(log => !log.IsArchived)
            .Take(1000) // Batch size
            .ToListAsync();
        
        foreach (var log in eligibleLogs)
        {
            var archive = new AuditLogArchive
            {
                Id = log.Id,
                UserId = log.UserId,
                EntityType = log.EntityType,
                // ... copy all properties
                ArchivedDate = DateTime.UtcNow
            };
            
            await _context.AuditLogArchives.AddAsync(archive);
            log.IsArchived = true; // Mark for deletion
        }
        
        await _context.SaveChangesAsync();
        
        // Delete archived logs (separate transaction)
        await _context.AuditLogs
            .Where(log => log.IsArchived)
            .ExecuteDeleteAsync();
    }
}
```

**Scheduling** (Hangfire):
```csharp
RecurringJob.AddOrUpdate<AuditLogArchivalService>(
    "archive-audit-logs",
    service => service.ArchiveOldLogsAsync(),
    Cron.Daily(2)); // Run at 2 AM daily
```

### Alternatives Considered

1. **EF Core Temporal Tables**
   - ❌ Rejected: SQL Server specific
   - ❌ Rejected: Less control over archival timing
   - ✅ Pro: Automatic history tracking

2. **Database Stored Procedure + SQL Agent Job**
   - ❌ Rejected: Platform-specific (SQL Server)
   - ❌ Rejected: Harder to test
   - ✅ Pro: No applicadependency

3. **On-demand archival (manual trigger)**
   - ❌ Rejected: Not automated
   - ❌ Rejected: Risk of forgetting
   - ✅ Pro: Simpler implementation

### Performance Considerations

- Batch size: 1000 records per run (configurable)
- Run during low-traffic hours (2 AM)
- Use `ExecuteDeleteAsync` for efficient bulk delete
- Monitor execution time and adjust batch size
- Add index on `CreatedAt` column for efficient queries

---

## Implementation Checklist

### Phase 0 (Research) - ✅ Complete
- [x] Research EF Core value object mapping
- [x] Research immutability enforcement patterns
- [x] Research migration strategies
- [x] Research conflict resolution approaches
- [x] Research audit log archival options
- [x] Document all decisions with rationale

### Phase 1 (Design) - Next
- [ ] Create data-model.md with detailed schemas
- [ ] Create value object contracts
- [ ] Create migration compatibility guide
- [ ] Create quickstart guide
- [ ] Review designs with team

### Phase 2 (Implementation) - Future
- [ ] Implement value objects
- [ ] Refactor models
- [ ] Create migrations
- [ ] Add tests
- [ ] Update documentation

---

## References

- [EF Core Owned Entity Types](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities)
- [EF Core Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Hangfire Background Jobs](https://www.hangfire.io/)
- C# Coding Standards: `/home/abdssamie/ChemforgeProjects/Quater/.opencode/prompts/csharp-coding-style.txt`

---

**Research Status**: ✅ COMPLETE  
**Next Step**: Create Phase 1 design documents (data-model.md, contracts/, quickstart.md)

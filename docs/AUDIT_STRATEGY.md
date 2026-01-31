# Audit Logging Strategy

## Overview

The Quater system implements a **multi-layered audit tracking architecture** designed for compliance, conflict resolution, and data integrity in an offline-first water quality testing environment.

---

## Architecture Layers

### 1. **Conflict Prevention Layer** (IConcurrent)

**Purpose**: Prevent data loss from concurrent modifications using optimistic locking.

**Implementation**:
```csharp
public interface IConcurrent
{
    byte[] RowVersion { get; set; }  // Auto-incremented by database
}
```

**How it works**:
- Database automatically updates `RowVersion` on every modification
- When saving, EF Core checks if `RowVersion` matches the database
- If mismatch detected → throws `DbUpdateConcurrencyException`
- Forces user to reload and review changes before retrying

**Example**:
```
Tech A reads Sample (RowVersion = v1)
Tech B reads Sample (RowVersion = v1)
Tech A saves → RowVersion becomes v2 ✅
Tech B tries to save → CONFLICT! RowVersion mismatch ❌
```

---

### 2. **Conflict Detection Layer** (ISyncable)

**Purpose**: Track synchronization state for offline-first scenarios.

**Implementation**:
```csharp
public interface ISyncable
{
    DateTime LastSyncedAt { get; set; }
    string? SyncVersion { get; set; }
}
```

**How it works**:
- `LastSyncedAt`: Timestamp of last successful sync
- `SyncVersion`: Version identifier for conflict resolution algorithms
- Helps determine which changes are newer during sync

---

### 3. **Audit Recording Layer** (AuditLog)

**Purpose**: Immutable historical record of ALL data modifications for compliance and forensics.

**Implementation**:
```csharp
public class AuditLog : IEntity
{
    public string UserId { get; set; }           // WHO
    public string EntityType { get; set; }       // WHAT (entity type)
    public Guid EntityId { get; set; }           // WHAT (specific record)
    public AuditAction Action { get; set; }      // HOW (Create/Update/Delete/Restore)
    public string? OldValue { get; set; }        // BEFORE (JSON snapshot)
    public string? NewValue { get; set; }        // AFTER (JSON snapshot)
    public string? ChangedFields { get; set; }   // WHICH FIELDS (quick filter)
    public DateTime Timestamp { get; set; }      // WHEN
    public string? IpAddress { get; set; }       // WHERE (client location)
    public Guid? ConflictBackupId { get; set; }  // LINK to conflict details
}
```

**Key Features**:
- **Immutable**: Never updated, only inserted (append-only log)
- **Complete Snapshots**: Stores full before/after state in JSON
- **Overflow Handling**: 
  - `IsTruncated`: Flag if data exceeded 4000 chars
  - `OverflowStoragePath`: URL to blob storage for large data
- **Field-Level Tracking**: `ChangedFields` for quick filtering without JSON parsing
- **Conflict Traceability**: Links to `ConflictBackup` records

---

### 4. **Entity Metadata Layer** (IAuditable)

**Purpose**: Quick access to current creation/modification metadata without querying audit logs.

**Implementation**:
```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
```

**Why separate from AuditLog?**
- **Performance**: No need to query audit table for simple "who created this?" questions
- **Convenience**: Directly accessible on entity for UI display
- **Current State**: Shows only latest metadata, not full history

---

### 5. **Soft Delete Layer** (ISoftDelete)

**Purpose**: Preserve deleted data for audit trail and recovery.

**Implementation**:
```csharp
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

**Why needed?**
- Compliance requirements: Must retain deleted data
- Audit trail: Can see what was deleted and when
- Recovery: Can restore accidentally deleted records
- Sync: Propagates deletes to offline clients

---

## Data Redundancy Explained

### Why do we have overlapping fields?

| Field Locose | Use Case |
|---------------|---------|----------|
| `Sample.CreatedBy` (IAuditable) | Quick reference | "Show who created this sample" |
| `Sample.LastModifiedBy` (Entity) | Concurrency tracking | "Who last touched this?" |
| `AuditLog.UserId` | Complete history | "Show all changes by this user" |
| `Sample.RowVersion` (IConcurrent) | Conflict prevention | "Detect concurrent edits" |
| `Sample.LastSyncedAt` (ISyncable) | Sync coordination | "When did this device last sync?" |

**This is intentional design**, not duplication:
- **IAuditable**: Fast current-state queries
- **Entity fields**: Business logic and concurrency
- **AuditLog**: Complete immutable history
- **IConcurrent**: Database-level conflict detection
- **ISyncable**: Offline sync coordination

---

## Archival Strategy

### Hot Storage (AuditLog)
- **Retention**: Last 90 days
- **Purpose**: Active compliance queries and recent conflict resolution
- **Performance**: Optimized for frequent queries

### Cold Storage (AuditLogArchive)
- **Retention**: 90+ days (configurable based on compliance requirements)
- **Purpose**: Long-term compliance and legal discovery
- **Performance**: Optimized for storage cost, infrequent access

### Archivacess
**Recommended Implementation**:
1. **Background Job**: Runs daily at off-peak hours
2. **Selection Criteria**: `Timestamp < (Now - 90 days) AND IsArchived = false`
3. **Process**:
   - Copy records to `AuditLogArchive`
   - Set `IsArchived = true` in `AuditLog`
   - After verification, optionally delete from `AuditLog` (or keep flagged)
4. **Retention Policy**: 
   - Regulatory compliance: Check local water quality regulations
   - Recommended: 7 years for legal defensibility
   - Consider: Compressed blob storage for cost optimization

---

## Conflict Resolution Workflow

### Step 1: Conflict Detection
```
User syncs → Server detects RowVersion mismatch → Conflict!
```

### Step 2: Conflict Backup Creation
```csharp
ConflictBackup {
    EntityId = sample.Id,
    ServerVersion = JSON(server_sample),
    ClientVersion = JSON(client_sample),
    ConflictDetectedAt = Now
}
```

### Step 3: Resolution
User chooses strategy:
- **LastWriteWins**: Most recent timestamp wins
- **ServerWins**: Keep server version
- **ClientWins**: Use client version
- **Manual**: User manually merges changes

### Step 4: Audit Recording
```csharp
AuditLog {
    Action = AuditAction.ConflictResolution,
    ConflictBackupId = backup.Id,
    OldValue = version),
    NewValue = JSON(winning_version),
    ConflictResolutionNotes = "User chose ClientWins: newer field reading"
}
```

---

## Compliance Benefits

✅ **Regulatory Compliance**: Complete audit trail for water quality testing  
✅ **Data Integrity**: Prevents lost updates through optimistic locking  
✅ **Forensic Analysis**: IP tracking and complete change history  
✅ **Legal Defensibility**: Immutable append-only log with timestamps  
✅ **Conflict Transparency**: Full documentation of how conflicts were resolved  
✅ **Data Recovery**: Soft deleteconflict backups enable restoration  

---

## Performance Considera

### Indexing Strategy
```sql
-- AuditLog indexes
CREATE INDEX IX_AuditLog_EntityType_EntityId ON AuditLog(EntityType, EntityId);
CREATE INDEX IX_AuditLog_UserId_Timestamp ON AuditLog(UserId, Timestamp);
CREATE INDEX IX_AuditLog_Timestamp_IsArchived ON AuditLog(Timestamp, IsArchived);
CREATE INDEX IX_AuditLog_ChangedFields ON AuditLog(ChangedFields);

-- AuditLogArchive indexes
CREATE INDEX IX_AuditLogArchive_EntityType_EntityId ON AuditLogArchive(EntityType, EntityId);
CREATE INDEX IX_AuditLogArchive_ArchivedDate ON AuditLogArchive(ArchivedDate);
```

### Query Optimization
- Use `ChangedFields` for filtering before JSON parsing
- Query `AuditLog` first (hot storage), then `AuditLogArchive` if needed
- Consider read replicas for audit queries to avoid impacting production

### Storage Optimization
- Compress JSON before storing (gzip)
- Use blob storage for `OverflowStoragePath` when data > 3500 chars
- Archive to cheaper storage tier after 90 days

---

## Usage Examples

### Recording an Update
```csharp
var auditLog = new AuditLog
{
    UserId = currentUser.Id,
    EntityType = nameof(Sample),
    EntityId = sample.Id,
    Action = AuditAction.Update,
    OldValue = JsonSerializer.Serialize(oldSample),
    NewValue = JsonSerializer.Serialize(newSample),
    ChangedFields = "pH,Temperature,CollectorName",
    Timestamp = DateTime.UtcNow,
    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
};
```

### Querying Audit History
```csharp
// Get all changes to a specific sample
var history = await context.AuditLogs
    .Where(a => a.EntityType == nameof(Sample) && a.EntityId == sampleId)
    .OrderByDescending(a => a.Timestamp)
    .ToListAsync();

// Find who changed pH values
var phChanges = await context.AuditLogs
    .Where(a => a.ChangedFields.Contains("pH"))
    .T);
```

---

## Future Enhancements

- [ ] Implement automatic archival background job
- [ ] Add audit log compression for large entities
- [ ] Create audit log viewer UI with diff visualization
- [ ] Implement audit log export for compliance reporting
- [ ] Add audit log retention policy configuration
- [ ] Consider event sourcing for critical entities

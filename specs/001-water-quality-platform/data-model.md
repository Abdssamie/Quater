# Data Model

**Feature**: Water Quality Lab Management System  
**Branch**: `001-water-quality-platform`  
**Date**: 2026-01-25  
**Status**: Complete

This document defines the complete data model for the Quater water quality lab management system, including entities, relationships, validation rules, and state transitions.

---

## Entity Relationship Diagram

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│     Lab     │────────<│    User     │>────────│  AuditLog   │
└─────────────┘         └─────────────┘         └─────────────┘
                              │                         │
                              │                         │
                              ▼                         │
                        ┌─────────────┐                 │
                        │   Sample    │<────────────────┘
                        └─────────────┘
                              │
                              │ 1:N
                              ▼
                        ┌─────────────┐         ┌─────────────┐
                        │ TestResult  │────────>│  Parameter  │
                        └─────────────┘         └─────────────┘
                              │
                              │
                              ▼
                        ┌─────────────┐
                        │  SyncLog    │
                        └─────────────┘
```

---

## Core Entities

### 1. Sample

Represents a water sample collected from a specific location at a specific time.

**Table**: `Samples`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier (UUID) |
| `Type` | string | Required, MaxLength(50) | Sample type: "drinking_water", "wastewater", "surface_water", "groundwater", "industrial_water" |
| `LocationLatitude` | double | Required | GPS latitude coordinate |
| `LocationLongitude` | double | Required | GPS longitude coordinate |
| `LocationDescription` | string | Optional, MaxLength(200) | Human-readable location (e.g., "Municipal Well #3") |
| `LocationHierarchy` | string | Optional, MaxLength(500) | Hierarchical location path for reporting (e.g., "Morocco/Casablanca Region/Casablanca/District 5/Well #42") |
| `CollectionDate` | DateTime | Required | UTC timestamp of sample collection |
| `CollectorName` | string | Required, MaxLength(100) | Name of technician who collected sample |
| `Notes` | string | Optional, MaxLength(1000) | Additional notes about sample |
| `Status` | string | Required, MaxLength(20) | Current status: "pending", "completed", "archived" |
| `Version` | int | Required, ConcurrencyToken | Optimistic locking version number |
| `LastModified` | DateTime | Required, ConcurrencyToken | UTC timestamp of last modification |
| `LastModifiedBy` | string | Required, MaxLength(100) | User ID who last modified |
| `IsDeleted` | bool | Required, Default(false) | Soft delete flag for sync |
| `IsSynced` | bool | Required, Default(false) | Sync status flag |
| `LabId` | Guid | FK, Required | Foreign key to Lab |
| `CreatedBy` | string | Required, MaxLength(100) | User ID who created sample |
| `CreatedDate` | DateTime | Required | UTC timestamp of creation |

**Indexes:**
- `IX_Samples_LastModified` on `LastModified`
- `IX_Samples_IsSynced` on `IsSynced`
- `IX_Samples_Status` on `Status`
- `IX_Samples_LabId` on `LabId`
- `IX_Samples_CollectionDate` on `Collete`

**Validation Rules:**
- `Type` must be one of: "drinking_water", "wastewater", "surface_water", "groundwater", "industrial_water"
- `Status` must be one of: "pending", "completed", "archived"
- `LocationLatitude` must be between -90 and 90
- `LocationLongitude` must be between -180 and 180
- `CollectionDate` cannot be in the future
- `LastModified` must be >= `CreatedDate`

**State Transitions:**
```
pending → completed → archived
   ↓          ↓
   └──────────┘
   (can revert to pending)
```

---

### 2. TestResult

Represents a single water quality test performed on a sample.

**Table**: `TestResults`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier (UUID) |
| `SampleId` | Guid | FK, Required | Foreign key to Sample |
| `ParameterName` | string | Required, MaxLength(100) | Water quality parameter (e.g., "pH", "turbidity") |
| `Value` | double | Required | Measured value |
| `Unit` | string | Required, MaxLength(20) | Unit of measurement (e.g., "mg/L", "NTU") |
| `TestDate` | DateTime | Required | UTC timestamp of test |
| `TechnicianName` | string | Required, MaxLength(100) | Name of technician who performed test |
| `TestMethod` | string | Required, MaxLength(50) | Method used for testing: "Titration", "Spectrophotometry", "Chromatography", "Microscopy", "Electrode", "Culture", "Other" |
| `ComplianceStatus` | string | Required, MaxLength(20) | Compliance result: "pass", "fail", "warning" |
| `Version` | int | Required, ConcurrencyToken | Optimistic locking version number |
| `LastModified` | DateTime | Required, ConcurrencyToken | UTC timestamp of last modification |
| `LastModifiedBy` | string | Required, MaxLength(100) | User ID who last modified |
| `IsDeleted` | bool | Required, Default(false) | Soft delete flag for sync |
| `IsSynced` | bool | Required, Default(false) | Sync status flag |
| `CreatedBy` | string | Required, MaxLength(100) | User ID who created result |
| `CreatedDate` | DateTime | Required | UTC timestamp of creation |

**Indexes:**
- `IX_TestResults_SampleId` on `SampleId`
- `IX_TestResults_LastModified` on `LastModified`
- `IX_TestResults_IsSynced` on `IsSynced`
- `IX_TestResults_ComplianceStatus` on `ComplianceStatus`
- `IX_TestResults_TestDate` on `TestDate`

**Validation Rules:**
- `ComplianceStatus` must be one of: "pass", "fail", "warning"
- `Value` must be >= 0
- `TestDate` cannot be in the future
- `TestDate` must be >= associated Sample.CollectionDate
- `ParameterName` must exist in Parameters table
- `TestMethod` must be one of: "Titration", "Spectrophotometry", "Chromatography", "Microscopy", "Electrode", "Culture", "Other"

---

### 3. Parameter

Represents a water quality parameter with compliance thresholds.

**Table**: `Parameters`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier (UUID) |
| `Name` | string | Required, Unique, MaxLength(100) | Parameter name (e.g., "pH", "turbidity") |
| `Unit` | string | Required, MaxLength(20) | Unit of measurement |
| `WhoThreshold` | double | Optional | WHO drinking water standard threshold |
| `MoroccanThreshold` | double | Optional | Moroccan standard threshold (Phase 2) |
| `MinValue` | double | Optional | Minimum valid value |
| `MaxValue` | double | Optional | Maximum valid value |
| `Description` | string | Optional, MaxLength(500) | Parameter description |
| `IsActive` | bool | Required, Default(true) | Whether parameter is currently used |
| `CreatedDate` | DateTime | Required | UTC timestamp of creation |
| `LastModified` | DateTime | Required | UTC timestamp of last modification |

**Indexes:**
- `IX_Parameters_Name` on `Name` (unique)
- `IX_Parameters_IsActive` on `IsActive`

**Validation Rules:**
- `Name` must be unique
- `MinValue` must be < `MaxValue` (if both specified)
- At least one of `WhoThreshold` or `MoroccanThreshold` must be specified

**Default Parameters (MVP):**
- pH (unit: pH, WHO: 6.5-8.5)
- Turbidity (unit: NTU, WHO: 5)
- Free Chlorine (unit: mg/L, WHO: 0.2-5)
- Total Chlorine (unit: mg/L, WHO: 5)
- E. coli (unit: CFU/100mL, WHO: 0)
- Total Coliforms (unit: CFU/100mL, WHO: 0)
- Temperature (unit: °C, WHO: N/A)
- Conductivity (unit: µS/cm, WHO: N/A)
- Dissolved Oxygen (unit: mg/L, WHO: N/A)
- Hardness (unit: mg/L CaCO3, WHO: N/A)

---

### 4. User

Represents a system user with role-based access.

**Table**: `Users`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | string | PK, Required, MaxLength(100) | User ID (from ASP.NET Core Identity) |
| `Email` | string | Required, Unique, MaxLength(256) | User email address |
| `Role` | string | Required, MaxLength(20) | User role: "Admin", "Technician", "Viewer" |
| `LabId` | Guid | FK, Required | Foreign key to Lab |
| `CreatedDate` | DateTime | Required | UTC timestamp of account creation |
| `LastLogin` | DateTime | Optional | UTC timestamp of last login |
| `IsActive` | bool | Required, Default(true) | Whether account is active |

**Indexes:**
- `IX_Users_Email` on `Email` (unique)
- `IX_Users_LabId` on `LabId`
- `IX_Users_Role` on `Role`

**Validation Rules:**
- `Email` must be valid email format
- `Role` must be one of: "Admin", "Technician", "Viewer"
- `Email` must be unique across all users

**Role Permissions:**
- **Admin**: Full access (manage users, view/edit all data, generate reports)
- **Technician**: Create/edit samples and test results, view reports
- **Viewer**: Read-only access to samples, test results, and reports

---

### 5. Lab

Represents a water quality lab organization.

**Table**: `Labs`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier (UUID) |
| `Name` | string | Required, MaxLength(200) | Lab name |
| `Location` | string | Optional, MaxLength(500) | Lab physical address |
| `ContactInfo` | string | Optional, MaxLength(500) | Contact information (phone, email) |
| `CreatedDate` | DateTime | Required | UTC timestamp of lab creation |
| `IsActive` | bool | Required, Default(true) | Whether lab is active |

**Indexes:**
- `IX_Labs_Name` on `Name`

**Validation Rules:**
- `Name` must be unique per organization

---

### 6. SyncLog

Tracks synchronization etween clients and server.

**Table**: `SyncLogs`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier (UUID) |
| `DeviceId` | string | Required, MaxLength(100) | Unique device identifier |
| `UserId` | string | FK, Required, MaxLength(100) | Foreign key to User |
| `LastSyncTimestamp` | DateTime | Required | UTC timestamp of last successful sync |
| `Status` | string | Required, MaxLength(20) | Sync status: "success", "failed", "in_progress" |
| `ErrorMessage` | string | Optional, MaxLength(1000) | Error details if sync failed |
| `RecordsSynced` | int | Required, Default(0) | Number of records synced |
| `ConflictsDetected` | int | Required, Default(0) | Number of conflicts detected |
| `ConflictsResolved` | int | Required, Default(0) | Number of conflicts resolved |
| `CreatedDate` | DateTime | Required | UTC timestamp of sync attempt |

**Indexes:**
- `IX_SyncLogs_DeviceId` on `DeviceId`
- `IX_SyncLogs_UserId` on `UserId`
- `IX_SyncLogs_LastSyncTimestamp` on `LastSyncTimestamp`

**Validation Rules:**
- `Status` must be one of: "success", "failed", "in_progress"
- `ConflictsResolved` must be <= `ConflictsDetected`\n
### 7. AuditLog

Tracks all data modifications for compliance and conflict resolution.

**Table**: `AuditLogs`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier (UUID) |
| `UserId` | string | FK, Required, MaxLength(100) | Foreign key to User |
| `EntityType` | string | Required, MaxLength(50) | Type of entity modified (e.g., "Sample", "TestResult") |
| `EntityId` | Guid | Required | ID of modified entity |
| `Action` | string | Required, MaxLength(20) | Action performed: "create", "update", "delete" |
| `OldValue` | string | Optional, MaxLength(4000) | JSON serialized old value (for updates) |
| `NewValue` | string | Optional, MaxLength(4000) | JSON serialized new value |
| `ConflictResolutionNotes` | string | Optional, MaxLength(1000) | Notes when user resolves sync conflict |
| `Timestamp` | DateTime | Required | UTC timestamp of modification |
| `IpAddress` | string | Optional, MaxLength(45) | IP address of client (IPv4/IPv6) |
| `IsArchived` | bool | Required, Default(false) | Flag indicating if record is archived (for 90-day archival strategy) |

**Indexes:**
- `IX_AuditLogs_UserId` on `UserId`
- `IX_AuditLogs_EntityType_EntityId` on `EntityType, EntityId`
- `IX_AuditLogs_Timestamp` on `Timestamp`
- `IX_AuditLogs_IsArchived` on `IsArchived`

**Validation Rules:**
- `Action` must be one of: "create", "update", "delete"
- `EntityType` must be one of: "Sample", "TestResult", "Parameter", "User", "Lab"
- For "create": `OldValue` must be null
- For "delete": `NewValue` must be null
- For "update": Both `OldValue` and `NewValue` must be present

**Retention Policy:**
- Audit logs retained for 7 years (regulatory compliance requirement)
- **90-Day Archival Strategy**: 
  - Hot data: Last 90 days kept in `AuditLogs` table for fast queries
  - Cold data: Records older than 90 days moved to `AuditLogArchive` table
  - Archival process: Nightly background job runs at 2 AM UTC
  - Query strategy: Application queries hot data first; if historical data needed, query archive table
  - Performance: Keeps main table small for fast inserts and recent queries

---

### 8. AuditLogArchive

Archived audit logs older than 90 days (cold storage).

**Table**: `AuditLogArchive`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier (UUID) |
| `UserId` | string | FK, Required, MaxLength(100) | Foreign key to User |
| `EntityType` | string | Required, MaxLength(50) | Type of entity modified (e.g., "Sample", "TestResult") |
| `EntityId` | Guid | Required | ID of modified entity |
| `Action` | string | Required, MaxLength(20) | Action performed: "create", "update", "delete" |
| `OldValue` | string | Optional, MaxLength(4000) | JSON serialized old value (for updates) |
| `NewValue` | string | Optional, MaxLength(4000) | JSON serialized new value |
| `ConflictResolutionNotes` | string | Optional, MaxLength(1000) | Notes when user resolves sync conflict |
| `Timestamp` | DateTime | Required | UTC timestamp of modification |
| `IpAddress` | string | Optional, MaxLength(45) | IP address of client (IPv4/IPv6) |
| `ArchivedDate` | DateTime | Required | UTC timestamp when record was archived |

**Indexes:**
- `IX_AuditLogArchive_UserId` on `UserId`
- `IX_AuditLogArchive_EntityType_EntityId` on `EntityType, EntityId`
- `IX_AuditLogArchive_Timestamp` on `Timestamp`
- `IX_AuditLogArchive_ArchivedDate` on `ArchivedDate`

**Validation Rules:**
- Same as `AuditLogs` table
- `ArchivedDate` must be >= `Timestamp`

---

## Relationships

### One-to-Many Relationships

1. **Lab → User** (1:N)
   - One lab has many users
   - Foreign key: `User.LabId` → `Lab.Id`
   - Cascade: Restrict (cannot delete lab with active users)

2. **Lab → Sample** (1:N)
   - One lab has many samples
   - Foreign key: `Sample.LabId` → `Lab.Id`
   - Cascade: Restrict (cannot delete lab with samples)

3. **Sample → TestResult** (1:N)
   - One sample has many test results
   - Foreign key: `TestResult.SampleId` → `Sample.Id`
   - Cascade: Cascade (deleting sample deletes test results)

4. **User → AuditLog** (1:N)
   - One user creates many audit log entries
   - Foreign key: `AuditLog.UserId` → `User.Id`
   - Cascade: Restrict (preserve audit trail)

5. **User → AuditLogArchive** (1:N)
   - One user creates many archived audit log entries
   - Foreign key: `AuditLogArchive.UserId` → `User.Id`
   - Cascade: Restrict (preserve audit trail)

6. **User → SyncLog** (1:N)
   - One user has many sync log entries
   - Foreign key: `SyncLog.UserId` → `User.Id`
   - Cascade: Restrict (preserve sync history)

### Many-to-One Relationships

1. **TestResult → Parameter** (N:1)
   - Many test results reference one parameter
   - Relationship: `TestResult.ParameterName` matches `Parameter.Name`
   - No formal foreign key (string-based lookup for flexibility)

---

## Database Schema Differences

### PostgreSQL (Backend)

- Use `uuid` type for Guid fields
- Use `timestamp with time zone` for DateTime fields
- Use `text` for string fields (no length limit)
- Enable full-text search on `Sample.Notes`, `Sample.LocationDescription`
- Use `jsonb` for `AuditLog.OldValue` and `AuditLog.NewValue`

### SQLite (Desktop/Mobile)

- Use `TEXT` for Guid fields (stored as string)
- Use `TEXT` for DateTime fields (ISO 8601 format)
- Use `TEXT` for string fields with application-level length validation
- Use `TEXT` for JSON fields (`AuditLog.OldValue`, `AuditLog.NewValue`)
- Enable WAL mode for better concurrency: `PRAGMA journal_mode=WAL;`

---

## Sync Protocol

### Change Tracking

All syncable entities include:
- `Version`: Incremented on each update (optimistic locking)
- `LastModified`: UTC timestamp of last modification
- `LastModifiedBy`: User ID who made the change
- `IsDeleted`: Soft delete flag (never hard delete synced records)
- `IsSynced`: Flag indicating if local changes have been synced

### Sync Flow

1. **Pull from Server**:
   ```
   GET /api/sync?since={lastSyncTimestamp}
   Response: { changes: [...], conflicts: [...], timestamp: "..." }
   ```

2. **Conflict Detection**:
   - Compare `LastModified` timestamps
   - If local > server: Local wins (push to server)
   - If server > local: Server wins (update local)
   - If equal but different data: Conflict (user resolution required)

3. **Push to Server**:
   ```
   POST /api/sync
   Body: { changes: [...], deviceId: "...", lastSyncTimestamp: "..." }
   Response: { conflicts: [...], accepted: [...], rejected: [...] }
   ```

4. **Conflict Resolution**:
   - Show user both versions side-by-side
   - User selects which version to keep
   - Optional notes field for documentation
   - Winning version saved with updated `LastModified`
   - Both versions preserved in `AuditLog` with `ConflictResolutionNotes`

---

## Performance Considerations

### Indexing Strategy

- **Sync quies**: Index on `LastModified`, `IsSynced`
- **Filtering**: Index on `Status`, `ComplianceStatus`, `CollectionDate`, `TestDate`
- **Lookups**: Index on all foreign keys
- **Composite indexes**: `(IsSynced, LastModified)` for sync queries

### Query Optimization

- Use `AsNoTracking()` for read-only queries
- Batch inserts/updates (avoid SaveChanges in loops)
- Use compiled queries for frequently executed queries
- Implement pagination for large result sets (100 records per page)

### Data Volume Estimates

- **Samples**: ~1,000-10,000 per lab per year
- **TestResults**: ~10-50 per sample (10,000-500,000 per lab per year)
- **AuditLogs**: ~3x TestResults (30,000-1,500,000 per lab per year)
- **SyncLogs**: ~100-1,000 per device per year

---

## Migration Strategy

### Initial Migration

1. Create all tables with indexes
2. Seed `Parameters` table with WHO standards
3. Create default admin user
4. Create default lab
5. Create `AuditLogArchive` table with same schema as `AuditLogs`

### Schema Evolution

- Use EF Core migrations for schema changes
- Apply migrations programmatically at app startup
- Test migrations thoroughly (SQLite has limited ALTER TABLE support)
- Version database schema in `__EFMigrationsHistory` table

### Background Jobs

- **Audit Log Archival**: Nightly job at 2 AM UTC
  - Move records older than 90 days from `AuditLogs` to `AuditLogArchive`
  - Delete moved records from `AuditLogs` after successful archive
  - Log archival statistics (records moved, errors)
  - Implement using Hangfire or similar background job framework

---

**Data Model Status**: ✅ Complete - Ready for implementation

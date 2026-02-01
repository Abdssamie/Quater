# Simplified Model Refactoring Migration

## Phase 1: Preparation (Priority 0)

### T001: Create feature branch `001-refactor-shared-models`
- **Type**: task
- **Priority**: 0
- **Description**: Create a new feature branch for the simplified model refactoring work.

Steps:
1. Ensure main branch is up to date
2. Create branch: `git checkout -b 001-refactor-shared-models`
3. Push branch to remote: `git push -u origin 001-refactor-shared-models`

Acceptance Criteria:
- Branch created and pushed to remote
- Branch name follows project naming convention
- Ready for subsequent refactoring work

---

### T002: Document current ConflictBackup and SyncLog usage
- **Type**: task
- **Priority**: 0
- **Dependencies**: T001
- **Description**: Audit and document all current usage of ConflictBackup and SyncLog tables before removal.

Steps:
1. Search codebase for ConflictBackup references
2. Search codebase for SyncLog references
3. Document all usages in REFACTORING_NOTES.md
4. Identify any edge cases or special handling
5. Review with team if needed

Files to check:
- All .cs files in shared/, backend/, desktop/
- All EF Core configurations
- All service classes
- All test files

Acceptance Criteria:
- Complete list of all ConflictBackup usages documented
- Complete list of all SyncLog usages documented
- Edge cases identified
- Documentation reviewed

---

### T003: Backup current database schema
- **Type**: task
- **Priority**: 0
- **Dependencies**: T001
- **Description**: Create backups of current database schemas before making breaking changes.

Steps:
1. Export PostgreSQL schema: `pg_dump --schema-only quater_dev > schema_backup_postgres.sql`
2. Export SQLite schema: `sqlite3 quater.db .schema > schema_backup_sqlite.sql`
3. Document current migration state
4. Store backups in `docs/migrations/backups/` directory
5. Commit backups to git

Acceptance Criteria:
- PostgreSQL schema backed up
- SQLite schema backed up
- Current migration state documented
- Backups committed to git
- Rollback procedure documented

---

## Phase 2: Remove Model References (Priority 1)

### T004: Remove ConflictBackupId and navigation from AuditLog.cs
- **Type**: task
- **Priority**: 1
- **Dependencies**: T002
- **Description**: Remove ConflictBackup-related properties from AuditLog model.

File: `shared/Models/AuditLog.cs`

Changes:
1. Remove `ConflictBackupId` property
2. Remove `ConflictResolutionNotes` property
3. Remove `ConflictBackup` navigation property
4. Update XML documentation if needed

Acceptance Criteria:
- ConflictBackupId property removed
- ConflictResolutionNotes property removed
- ConflictBackup navigation removed
- No compilation errors
- No circular dependency warnings

---

### T005: Remove ConflictBackupId and navigation from AuditLogArchive.cs
- **Type**: task
- **Priority**: 1
- **Dependencies**: T002
- **Description**: Remove ConflictBackup-related properties from AuditLogArchive model.

File: `shared/Models/AuditLogArchive.cs`

Changes:
1. Remove `ConflictBackupId` property
2. Remove `ConflictResolutionNotes` property
3. Remove `ConflictBackup` navigation property
4. Update XML documentation if needed

Acceptance Criteria:
- ConflictBackupId property removed
- ConflictResolutionNotes property removed
- ConflictBackup navigation removed
- No compilation errors
- Archive model matches AuditLog structure

---

### T006: Remove ConflictBackup and SyncLog from EntityType enum
- **Type**: task
- **Priority**: 1
- **Dependencies**: T002
- **Description**: Remove ConflictBackup and SyncLog enum values from EntityType.

File: `shared/Enums/EntityType.cs`

Changes:
1. Remove `ConflictBackup` enum value
2. Remove `SyncLog` enum value
3. Update XML documentation
4. Check for any switch statements that need updating

Acceptance Criteria:
- ConflictBackup enum value removed
- SyncLog enum value removed
- No compilation errors
- All switch statements handle remaining values

---

### T007: Remove ConflictBackups and SyncLogs DbSets from QuaterDbContext
- **Type**: task
- **Priority**: 1
- **Dependencies**: T002
- **Description**: Remove ConflictBackups and SyncLogs DbSet properties from backend DbContext.

File: `backend/src/Quater.Backend.Data/QuaterDbContext.cs`

Changes:
1. Remove `DbSet<ConflictBackup> ConflictBackups` property
2. Remove `DbSet<SyncLog> SyncLogs` property
3. Remove any OnModelCreating configuration for these entities
4. Update XML documentation

Acceptance Criteria:
- ConflictBackups DbSet removed
- SyncLogs DbSet removed
- No compilation errors
- DbContext builds successfully

---

### T008: Remove ConflictBackups and SyncLogs DbSets from QuaterLocalContext
- **Type**: task
- **Priority**: 1
- **Dependencies**: T002
- **Description**: Remove ConflictBackups and SyncLogs DbSet properties from desktop DbContext.

File: `desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs`

Changes:
1. Remove `DbSet<ConflictBackup> ConflictBackups` property
2. Remove `DbSet<SyncLog> SyncLogs` property
3. Remove any OnModelCreating configuration for these entities
4. Update XML documentation

Acceptance Criteria:
- ConflictBackups DbSet removed
- SyncLogs DbSet removed
- No compilation errors
- DbContext builds successfully

---

## Phase 3: Remove EF Core Configurations (Priority 1)

### T009: Delete ConflictBackupConfiguration.cs
- **Type**: task
- **Priority**: 1
- **Dependencies**: T004, T005, T006, T007, T008
- **Description**: Delete the EF Core configuration file for ConflictBackup entity.

File to delete: `backend/src/Quater.Backend.Data/Configurations/ConflictBackupConfiguration.cs`

Steps:
1. Verify no references to this configuration exist
2. Delete the file
3. Verify build succeeds

Acceptance Criteria:
- File deleted
- No compilation errors
- No references to ConflictBackupConfiguration remain

---

### T010: Delete SyncLogConfiguration.cs
- **Type**: task
- **Priority**: 1
- **Dependencies**: T004, T005, T006, T007, T008
- **Description**: Delete the EF Core configuration file for SyncLog entity.

File to delete: `backend/src/Quater.Backend.Data/Configurations/SyncLogConfiguration.cs`

Steps:
1. Verify no references to this configuration exist
2. Delete the file
3. Verify build succeeds

Acceptance Criteria:
- File deleted
- No compilation errors
- No references to SyncLogConfiguration remain

---

### T011: Update AuditLogConfiguration.cs (remove ConflictBackup relationship)
- **Type**: task
- **Priority**: 1
- **Ds**: T004, T005, T006, T007, T008
- **Description**: Remove ConflictBackup relationship configuration from AuditLogConfiguration.

File: `backend/src/Quater.Backend.Data/Configurations/AuditLogConfiguration.cs`

Changes:
1. Remove HasOne/WithMany configuration for ConflictBackup relationship
2. Remove any foreign key configuration for ConflictBackupId
3. Remove any index configuration for ConflictBackupId
4. Update XML documentation

Acceptance Criteria:
- ConflictBackup relationship configuration removed
- No compilation errors
- Configuration builds successfully
- AuditLog table configuration is valid

---

### T012: Update AuditLogArchiveConfiguration.cs (remove ConflictBackup relationship)
- **Type**: task
- **Priority**: 1
- **Dependencies**: T004, T005, T006, T007, T008
- **Description**: Remove ConflictBackup relationship configuration from AuditLogArchiveConfiguration.

File: `backend/src/Quater.Backend.Data/Configurations/AuditLogArchiveConfiguration.cs`

Changes:
1. Remove HasOne/WithMany configuration for ConflictBackup relationship
2. Remove any foreign key configuration for ConflictBackupId
3. Remove any index configuration for ConflictBackupId
4. Update XML documentation

Acceptance Criteria:
- ConflictBackup relationship configuration removed
- No compilation errors
- Configuration builds successfully
- AuditLogArchive table configuration is valid

---

### T013: Verify UserConfiguration.cs (keep AuditLogs relationship)
- **Type**: task
- **Priority**: 1
- **Dependencies**: T004, T005, T006, T007, T008
- **Description**: Verify that UserConfiguration still has AuditLogs relationship and no circular dependency exists.

File: `backend/src/Quater.Backend.Data/Configurations/UserConfiguration.cs`

Verification steps:
1. Confirm AuditLogs collection relationship exists
2. Verify no circular dependency warnings
3. Verify User → AuditLog relationship is valid
4Run build and check for warnings

Acceptance Criteria:
- AuditLogs relationship present
- No circular dependency warnings
- No compilation errors
- Relationship configuration is valid

---

## Phase 4: Delete Model Files (Priority 1)

### T014: Delete ConflictBackup.cs model
- **Type**: task
- **Priority**: 1
- **Dependencies**: T009, T010, T011, T012, T013
- **Description**: Delete the ConflictBackup model file from shared models.

File to delete: `shared/Models/ConflictBackup.cs`

Steps:
1. Verify all references removed (T004-T013 complete)
2. Delete the file
3. Verify build succeeds
4. Check for any lingering references

Acceptance Criteria:
- File deleted
- No compilation errors
- No references to ConflictBackup class remain
- Build succeeds

---

### T015: Delete SyncLog.cs model
- **Type**: task
- **Priority**: 1
- **Dependencies**: T009, T010, T011, T012, T013
- **Description**: Delete the SyncLog model file from shared models.

File to delete: `shared/Models/SyncLog.cs`

Steps:
1. Verify all references removed (T004-T013 complete)
2. Delete the file
3. Verify build succeeds
4. Check for any lingering references

Acceptance Criteria:
- File deleted
- No compilation errors
- No references to SyncLog class remain
- Build succeeds

---

## Phase 5: Remove Services (Priority 1)

### T016: Delete BackupService.cs
- **Type**: task
- **Priority**: 1
- **Dependencies**: T014, T015
- **Description**: Delete the BackupService that creates ConflictBackup records.

File to delete: `backend/src/Quater.Backend.Sync/BackupService.cs`

Steps:
1. Verify no references to BackupService exist
2. Delete the file
3. Remove any DI registrations
4. Verify build succeeds

Acceptance Criteria:
- File deleted
- DI registrations removed
- No compilation errors
- No references to BackupService remain

---

### T017: Delete ConflictResolver.cs
- **Type**: task
- **Priority**: 1
- **Dependencies**: T014, T015
- **Description**: Delete the ConflictResolver service that uses ConflictBackup.

File to delete: `backend/src/Quater.Backend.Sync/ConflictResolver.cs`

Steps:
1. Verify no references to ConflictResolver exist
2. Delete the file
3. Remove any DI registrations
4. Verify build succeeds

Acceptance Criteria:
- File deleted
- DI registrations removed
- No compilation errors
- No references to ConflictResolver remain

---

### T018: Delete SyncLogService.cs
- **Type**: task
- **Priority**: 1
- **Dependencies**: T014, T015
- **Description**: Delete the SyncLogService that creates SyncLog records.

File to delete: `backend/src/Quater.Backend.Sync/SyncLogService.cs`

Steps:
1. Verify no references to SyncLogService exist
2. Delete the file
3. Remove any DI registrations
4. Verify build succeeds

Acceptance Criteria:
- File deleted
- DI registrations removed
- No compilation errors
- No references to SyncLogService remain

---

### T019: Delete IBackupService.cs interface
- **Type**: task
- **Prio: 1
- **Dependencies**: T014, T015
- **Description**: Delete the IBackupService interface.

File to delete: `backend/src/Quater.Backend.Core/Interfaces/IBackupService.cs`

Steps:
1. Verify no references to IBackupService exist
2. Delete the file
3. Remove any DI registrations
4. Verify build succeeds

Acceptance Criteria:
- File deleted
- DI registrations removed
- No compilation errors
- No references to IBackupService remain

---

## Phase 6: Update Sync Logic (Priority 1)

### T020: Simplify SyncService.cs conflict handling (remove ConflictBackup creation)
- **Type**: task
- **Priority**: 1
- **Dependencies**: T016, T017, T018, T019
- **Description**: Simplify conflict handling in SyncService by removing ConflictBackup creation logic.

File: `backend/src/Quater.Backend.Sync/SyncService.cs`

Changes:
1. Remove calls to BackupService
2. Remove ConflictBackup creation logic
3. Simplify conflict detection to rely on OCC (RowVersion)
4. Keep conflict detection and error reporting
5. Update error messages for clarity

Acceptance Criteria:
- BackupService calls removed
- ConflictBackup creation removed
- OCC conflict detection still works
- Clear error messages for conflicts
- No compilation errors

---

### T021: Add application logging for conflicts (replace SyncLog)
- **Type**: task
- **Priority**: 1
- **Dependencies**: T016, T017, T018, T019
- **Description**: Add proper application logging for sync conflicts to replace SyncLog functionality.

File: `backend/src/Quater.Backend.Sync/SyncService.cs`

Changes:
1. Add ILogger<SyncService> dependency
2. Log conflict events with appropriate log levels
3. Include relevant context (entity type, ID, user)
4. Log sync operations (start, success, failure)
5. Remove SyncLogService calls

Logging to add:
- LogInformation: Sync started/completed
- LogWarning: Conflict detected (OCC)
- LogError: Sync failures

Acceptance Criteria:
- ILogger properly injected
- Conflicts logged with context
- Sync operations logged
- SyncLogService calls removed
- No compilation errors

---

### T022: Update conflict error messages for users
- **Type**: task
- **Priority**: 1
- **Dependencies**: T020, T021
- **Description**: Update user-facing error messages for sync conflicts to be clear and actionable.

File: `backend/src/Quater.Backend.Sync/SyncService.cs`

Changes:
1. Update conflict exception messages
2. Provide clear guidance on resoRemove references to ConflictBackup
4. Explain OCC behavior clearly

Example message:
"The record has been modified by another user. Please refresh and try again. Your changes have not been lost."

Acceptance Criteria:
- Error messages are user-friendly
- Messages explain OCC behavior
- No technical jargon
- Actionable guidance provided
- No references to removed features

---

## Phase 7: Create Database Migration (Priority 1)

### T023: Create EF Core migration `RemoveConflictBackupAndSyncLog`
- **Type**: task
- **Priority**: 1
- **Dependencies**: T020, T021, T022
- **Description**: Create EF Core migration to remove ConflictBackup and SyncLog tables and related columns.

Steps:
1. Ensure all code changes complete (T004-T022)
2. Run: `dotnet ef migrations add RemoveConflictBackupAndSyncLog --project backend/src/Quater.Backend.Data`
3. Review generated migration file
4. Verify Up() method drops tables and columns
5. Verify Down() method can restore schema

Migration should:
- Drop ConflictBackups table
- Drop SyncLogs table
- Drop ConflictBackupId column from AuditLog
- Drop ConflictResolutionNotes column from AuditLog
- Drop ConflictBackupId column from AuditLogArchive
- Drop ConflictResolutionNotes column from AuditLogArchive
- Drop related indexes and foreign keys

Acceptance Criteria:
- Migration created successfully
- Up() method drops all required tables/columns
- Down() method can restore schema
- Migration file reviewed and validated

---

### T024: Verify migration Up() drops tables and columns correctly
- **Type**: task
- **Priority**: 1
- **Dependencies**: T023
- **Description**: Verify the migration Up() method correctly drops all ConflictBackup and SyncLog related schema elements.

File: `backend/src/Quater.Backend.Data/Migrations/[timestamp]_RemoveConflictBackupAndSyncLog.cs`

Verification checklist:
1. ConflictBackups table dropped
2. SyncLogs table dropped
3. AuditLog.ConflictBackupId column dropped
4. AuditLog.ConflictResolutionNotes column dropped
5. AuditLogArchive.ConflictBackupId column dropped
6. AuditLogArchive.ConflictResolutionNotes column dropped
7. Related foreign keys dropped
8. Related indexes dropped
9. Operations in correct order (FK first, then columns, then tables)

Acceptance Criteria:
- All tables dropped
- All columns dropped
- All foreigped
- All indexes dropped
- Operations in correct order
- No SQL errors in migration

---

### T025: Verify migration Down() can restore tables correctly
- **Type**: task
- **Priority**: 1
- **Dependencies**: T023
- **Description**: Verify the migration Down() method can restore ConflictBackup and SyncLog schema for rollback.

File: `backend/src/Quater.Backend.Data/Migrations/[timestamp]_RemoveConflictBackupAndSyncLog.cs`

Verification checklist:
1. ConflictBackups table recreated with all columns
2. SyncLogs table recreated with all columns
3. AuditLog.ConflictBackupId column restored
4. AuditLog.ConflictResolutionNotes column restored
5. AuditLogArchive.ConflictBackupId column restored
6. AuditLogArchive.ConflictResolutionNotes column restored
7. Foreign keys restored
8. Indexes restored
9. Operations in correct order (tables first, then columns, then FK)

Acceptance Criteria:
- All tables can be restored
- All columns can be restored
- All foreign keys can be restored
- All indexes can be restored
- Operations in correct order
- Rollback tested successfully

---

## Phase 8: Update Tests (Priority 2)

### T026: Remove ConflictBackup-related tests
- **Type**: task
- **Priority**: 2
- **Dependencies**: T023, T024, - **Description**: Remove all tests related to ConflictBackup functionality.

Steps:
1. Search for tests containing "ConflictBackup"
2. Remove or update tests as appropriate
3. Verify test suite still runs
4. Update test documentation

Files to check:
- backend/tests/**/*Test.cs
- desktop/tests/**/*Test.cs

Acceptance Criteria:
- All ConflictBackup tests removed
- Test suite compiles
- All remaining tests pass
- No references to ConflictBackup in tests

---

### T027: Remove SyncLog-related tests
- **Type**: task
- **Priority**: 2
- **Dependencies**: T023, T024, T025
- **Description**: Remove all tests related to SyncLog functionality.

Steps:
1. Search for tests containing "SyncLog"
2. Remove or update tests as appropriate
3. Verify test suite still runs
4. Update test documentation

Files to check:
- backend/tests/**/*Test.cs
- desktop/tests/**/*Test.cs

Acceptance Criteria:
- All SyncLog tests removed
- Test suite compiles
- All remaining tests pass
- No references to SyncLog in tests

---

### T028: Update sync conflict tests (verify OCC still works)
- **Type**: task
- **Priority**: 2
- **Dependencies**: T020, T021, T022
- **Description**: Update sync conflict tests to verify OCC (Optimistic Concurrency Control) still works without ConflictBackup.

Files to update:
- backend/tests/Quater.Backend.Sync.Tests/SyncServiceTests.cs

Changes:
1. Update conflict detection tests
2. Verify RowVersion comparison works
3. Test concurrent update scenarios
4. Verify proper exception thrown
5. Test error messages

Test scenarios:
- Two users modify same record
- RowVersion mismatch detected
- Proper exception thrown
- Clear error message returned

Acceptance Criteria:
- OCC tests pass
- Conflict detection works
- RowVersion comparison validated
- Error messages tested
- No ConflictBackup references

---

### T029: Add test: verify ConflictBackup type doesn't exist
- **Type**: task
- **Priority**: 2
- **Dependencies**: T014, T015
- **Description**: Add a test to verify ConflictBackup type no longer exists in the assembly.

File: Create `backend/tests/Quater.Backend.Data.Tests/ModelRefactoringTests.cs`

Test to add:
```csharp
[Fact]
public void ConflictBackup_Type_Should_Not_Exist()
{
    var assembly = typeof(AuditLog).Assembly;
    var conflictBackupType = assembly.GetTypes()
        .FirstOrDefault(t => t.Name == "ConflictBackup");
    
    Assert.Null(conflictBackupType);
}
```

Acceptance Criteria:
- Test createn- Test passes
- ConflictBackup type not found
- Test documented

---

### T030: Add test: verify SyncLog type doesn't exist
- **Type**: task
- **Priority**: 2
- **Dependencies**: T014, T015
- **Description**: Add a test to verify SyncLog type no longer exists in the assembly.

File: `backend/tests/Quater.Backend.Data.Tests/ModelRefactoringTests.cs`

Test to add:
```csharp
[Fact]
public void SyncLog_Type_Should_Not_Exist()
{
    var assembly = typeof(AuditLog).Assembly;
    var syncLogType = assembly.GetTypes()
        .FirstOrDefault(t => t.Name == "SyncLog");
    
    Assert.Null(syncLogType);
}
```

Acceptance Criteria:
- Test created
- Test passes
- SyncLog type not found
- Test documented

---

## Phase 9: Update Documentation (Priority 2)

### T031: Update AUDIT_STRATEGY.md (remove ConflictBackup references)
- **Type**: task
- **Priority**: 2
- **Dependencies**: T023, T024, T025
- **Description**: Update audit strategy documentation to remove ConflictBackup references.

File: `docs/AUDIT_STRATEGY.md` (or create if doesn't exist)

Changes:
1. Remove ConflictBackup sections
2. Update audit flow diagrams
3. Clarify AuditLog purpose (compliance only)
4. Document simplified architecture
5. Update examples

Sections to update:
- Architecture overview
- Data flow diagrams
- Conflict handling (now OCC only)
- Compliance requirements

Acceptance Criteria:
- ConflictBackup references removed
- Documentation accurate
- Architecture diagrams updated
- Examples updated
- Compliance requirements clear

---

### T032: Update MODEL_SYNC_GUIDE.md (remove ConflictBackup/SyncLog)
- **Type**: task
- **Priority**: 2
- **Dependencies**: T023, T024, T025
- **Description**: Update model sync guide to remove ConflictBackup and SyncLog references.

File: `MODEL_SYNC_GUIDE.md`

Changes:
1. Remove ConflictBackup sections
2. Remove SyncLog sections
3. Update s diagrams
4. Clarify OCC behavior
5. Update conflict resolution guidance
6. Add application logging guidance

Sections to update:
- Sync architecture
- Conflict detection
- Error handling
- Logging strategy

Acceptance Criteria:
- ConflictBackup references removed
- SyncLog references removed
- OCC behavior documented
- Logging strategy clear
- Examples updated

---

### T033: Create OCC-EXPLAINED.md (clarify how OCC prevents overwrites)
- **Type**: task
- **Priority**: 2
- **Dependencies**: T020, T021, T022
- **Description**: Create new documentation explaining how Optimistic Concurrency Control prevents data overwrites.

File: Create `docs/OCC-EXPLAINED.md`

Content to include:
1. What is OCC?
2. How RowVersion works
3. Conflict detection flow
4. User experience during conflicts
5. Why ConflictBackup is unnecessary
6. Best practices for handling conflicts
7. Code examples

Sections:
- Introduction to OCC
- RowVersion mechanism
- Conflict scenarios
- Error handling
- User guidance
- Developer guidance
- FAQ

Acceptance Criteria:
- Document created
- OCC clearly explained
- RowVersion mechanism documented
- Conflict scenarios covered
- Code examples provided
- User guidance clear

---

## Phase 10: Verification & Deployment (Priority 1)

### T034: Run all backend tests
- **Type**: task
- **Priority**: 1
- **Dependencies**: T026, T027, T028, T029, T030
- **Description**: Run complete backend test suite to verify all changes work correctly.

Steps:
1. Run: `dotnet test backend/tests/`
2. Verify all tests pass
3. Check code coverage
4. Review any warnings
5. Document results

Test categories to verify:
- Unit tests
- Integration tests
- Sync tests
- Data access tests
- Service tests

Acceptance Criteria:
- All tests pass
- No test failures
- No test errors
- Code coverage maintained
- Results documented

---

### T035: Run all desktop tests
- **Type**: task
- **Priority**: 1
- **Dependencies**: T026, T027, T028, T029, T030
- **Description**: Run complete desktop test suite to verify all changes work correctly.

Steps:
1. Run: `dotnet test desktop/tests/`
2. Verify all tests pass
3. Check code coverage
4. Review any warnings
5. Document results

Test categories to verify:
- Unit tests
- Integration tests
- Sync tests
- Data access tests
- ViewModel tests

Acceptance Criteria:
- All tests pass
- No test failures
- No test errors
- Code coverage maintained
- Results documented

---

### T036: Verify zero Rider IDE warnings
- **Type**: task
- **Priority**: 1
- **Dependencies**: T034, T035
- **Description**: Open solution in Rider IDE and verify no warnings related to refactoring.

Steps:
1. Open Quater.sln in Rider
2. Run full solution analysis
3. Check for circular dependency warnings
4. Check for unused code warnings
5. Check for any refactoring suggestions
6. Document any remaining warnings

Areas to check:
- Circular dependencies
- Unused usings
- Unused code
- Nullable refce warnings
- Code style warnings

Acceptance Criteria:
- No circular dependency warnings
- No unused code related to removed features
- No critical warnings
- Solution analysis clean
- Results documented

---

### T037: Test migration on PostgreSQL (backend)
- **Type**: task
- **Priority**: 1
- **Dependencies**: T023, T024, T025, T034
- **Description**: Test the migration on a PostgreSQL database (backend).

Steps:
1. Create test PostgreSQL database
2. Apply all existing migrations
3. Apply new RemoveConflictBackupAndSyncLog migration
4. Verify schema changes
5. Test rollback (Down migration)
6. Verify data integrity
7. Document results

Verification:
- Tables dropped successfully
- Columns dropped successfully
- Foreign keys removed
- Indexes removed
- No orphaned data
- Rollback works

Acceptance Criteria:
- Migration applies successfully
- Schema changes verified
- Rollback tested
- No data loss
- No SQL errors
- Results documented

---

### T038: Test migration on SQLite (desktop)
- **Type**: task
- **Priority**: 1
- **Dependencies**: T023, T024, T025, T035
- **Description**: Test the migration on a SQLite database (desktop).

Steps:
1. Create test SQLite database
2. Apply all existing migrations
3. Apply new RemoveConflictBackupAndSyncLog migration
4. Verify schema changes
5. Test rollback (Down migration)
6. Verify data integrity
7. Document results

Verification:
- Tables dropped successfully
- Columns dropped successfully
- Foreign keys removed
- Indexes removed
- No orphaned data
- Rollback works

Acceptance Criteria:
- Migration applies successfully
- Schema changes verified
- Rollback tested
- No data loss
- No SQL errors
- Results documented

---

### T039: Deploy to staging environment
- **Type**: task
- **Priority**: 1
- **Dependencies**: T034, T035, T036, T037, T038
- *ription**: Deploy the refactored code to staging environment for final verification.

Steps:
1. Merge feature branch to staging branch
2. Deploy backend to staging server
3. Run migration on staging database
4. Deploy desktop app to staging
5. Verify all functionality works
6. Monitor logs for errors
7. Test sync operations
8. Document deployment

Verification checklist:
- Backend deployed successfully
- Migration applied successfully
- Desktop app works
- Sync operations work
- OCC conflict detection works
- Logging works correctly
- No errors in logs

Acceptance Criteria:
- Staging deployment successful
- Migration applied
- All functionality verified
- No critical errors
- Logs monitored
- Results documented

---

### T040: Monitor for issues (2 weeks)
- **Type**: task
- **Priority**: 2
- **Dependencies**: T039
- **Description**: Monitor staging environment for 2 weeks to catch any issues before production deployment.

Monitoring tasks:
1. Check application logs daily
2. Monitor error rates
3. Check sync operation success rates
4. Monitor database performance
5. Gather user feedback
6. Document any issues found
7. Create follow-up tasks if needed

Metrics to track:
- Err rate
- Sync success rate
- Conflict detection rate
- Database query performance
- User-reported issues

Acceptance Criteria:
- 2 weeks of monitoring complete
- No critical issues found
- Error rates normal
- Sync operations stable
- Performance acceptable
- Ready for production deployment

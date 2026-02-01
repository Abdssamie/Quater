# Simplified Model Refactoring - Beads Issues Created

**Date Created:** 2026-02-01  
**Total Issues:** 40 tasks  
**Feature Branch:** `001-refactor-shared-models`

## Overview

This document summarizes the 40 Beads issues created for the simplified model refactoring migration that removes ConflictBackup and SyncLog tables while keeping AuditLog.

## Objectives

- **Remove**: ConflictBackup table (stores data that was never saved due to OCC)
- **Remove**: SyncLog table (application logs are sufficient)
- **Keep**: AuditLog table (client compliance requirement)
- **Keep**: All OCC functionality (IConcurrent, RowVersion)
- **Eliminate**: Circular dependency: User → AuditLog → ConflictBackup → Lab → User

## Issue Summary by Phase

### Phase 1: Preparation (Priority 0) - 3 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-bmr2 | T001 | Create feature branch `001-refactor-shared-models` |
| Quater-syx2 | T002 | Document current ConflictBackup and SyncLog usage |
| Quater-7pb2 | T003 | Backup current database schema |

**Ready to start:** T001 (no dependencies)

---

### Phase 2: Remove Model References (Priority 1) - 5 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-tmqg | T004 | Remove ConflictBackupId and navigation from AuditLog.cs |
| Quater-x8qt | T005 | Remove ConflictBackupId and navigation from AuditLogArchive.cs |
| Quater-lh60 | T006 | Remove ConflictBackup and SyncLog from EntityType enum |
| Quater-pxhc | T007 | Remove ConflictBackups and SyncLogs DbSets from QuaterDbContext |
| Quater-vdlk | T008 | Remove ConflictBackups and SyncLogs DbSets from QuaterLocalContext |

**Dependencies:** All depend on T002

---

### Phase 3: Remove EF Core Configurations (Priority 1) - 5 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-bmef | T009 | Delete ConflictBackupConfiguration.cs |
| Quater-tx66 | T010 | Delete SyncLogConfiguration.cs |
| Quater-fcfw | T011 | Update AuditLogConfiguration.cs (remove ConflictBackup relationship) |
| Quater-4e3s | T012 | Update AuditLogArchiveConfiguration.cs (remove ConflictBackup relationship) |
| Quater-7j7e | T013 | Verify UserConfiguration.cs (keep AuditLogs relationship) |

**Dependencies:** All depend on T004-T008

---

### Phase 4: Delete Model Files (Priority 1) - 2 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-mmo2 | T014 | Delete ConflictBackup.cs model |
| Quater-s9cj | T015 | Delete SyncLog.cs model |

**Dependencies:** Both depend on T009-T013

---

### Phase 5: Remove Services (Priority 1) - 4 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-5y1b | T016 | Delete BackupService.cs |
| Quater-qugq | T017 | Delete ConflictResolver.cs |
| Quater-3n T018 | Delete SyncLogService.cs |
| Quater-2e3v | T019 | Delete IBackupService.cs interface |

**Dependencies:** All depend on T014-T015

---

### Phase 6: Update Sync Logic (Priority 1) - 3 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-365d | T020 | Simplify SyncService.cs conflict handling (remove ConflictBackup creation) |
| Quater-8ovh | T021 | Add application logging for conflicts (replace SyncLog) |
| Quater-mhx9 | T022 | Update conflict error messages for users |

**Dependencies:** 
- T020-T021 depend on T016-T019
- T022 depends on T020-T021

---

### Phase 7: Create Database Migration (Priority 1) - 3 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-phs2 | T023 | Create EF Core migration `RemoveConflictBackupAndSyncLog` |
| Quater-j3bz | T024 | Verify migration Up() drops tables and columns correctly |
| Quater-kk00 | T025 | Verify migration Down() can restore tables correctly |

**Dependencies:** 
- T023 depends on T020-T022
- T024-T025 depend on T023

---

### Phase 8: Update Tests (Priority 2) - 5 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-b6u5 | T026 | Remove ConflictBackup-related tests |
| Quater-c8qj | T027 | Remove SyncLog-related tests |
| Quater-nvv8 | T028 | Update sync conflict tests (verify OCC still works) |
| Quater-mtfs | T029 | Add test: verify ConflictBackup type doesn't exist |
| Quater-ua69 | T030 | Add trify SyncLog type doesn't exist |

**Dependencies:** 
- T026-T027 depend on T023-T025
- T028 depends on T020-T022
- T029-T030 depend on T014-T015

---

### Phase 9: Update Documentation (Priority 2) - 3 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-xreb | T031 | Update AUDIT_STRATEGY.md (remove ConflictBackup references) |
| Quater-ba9e | T032 | Update MODEL_SYNC_GUIDE.md (remove ConflictBackup/SyncLog) |
| Quater-pud3 | T033 | Create OCC-EXPLAINED.md (clarify how OCC prevents overwrites) |

**Dependencies:** 
- T031-T032 depend on T023-T025
- T033 depends on T020-T022

---

### Phase 10: Verification & Deployment (Priority 1) - 7 tasks
| ID | Task | Description |
|----|------|-------------|
| Quater-fedq | T034 | Run all backend tests |
| Quater-4rys | T035 | Run all desktop tests |
| Quater-fugi | T036 | Verify zero Rider IDE warnings |
| Quater-44f5 | T037 | Test migration on PostgreSQL (backend) |
| Quater-opf6 | T038 | Test migration on SQLite (desktop) |
| Quater-b9a9 | T039 | Deploy to staging environment |
| Quater-qtto | T040 | Monitor for issues (2 weeks) |

**Dependencies:** 
- T034-T035 depend on T026-T030
- T036 depends on T034-T035
- T037 depends on T023-T025, T034
- T038 depends on T023-T025, T035
- T039 depends on T034-T038
- T040 depends on T039

---

## Dependency Chain Summary

```
T001 (Create branch)
  ├─→ T002 (Document usage)
  │     ├─→ T004-T008 (Remove model references)
  │           ├─→ T009-T013 (Remove EF configs)
  │                 ├─→ T014-T015 (Delete models)
  │                       ├─→ T016-T019 (Delete services)
  │                       │     ├─→ T020-T022 (Update sync logic)
  │                       │           ├─→ T023 (Create migration)
  │                       │           │     ├─→ T024-T025 (Verify migration)
  │                       │           │     │     ├─→ T026-T027 (Remove tests)
  │                       │           │     │     ├─→ T031-T032 (Update docs)
  │                       │           │     │     ├─→ T037-T038 (Test migrations)
  │                       │           │     │           ├─→ T039 (Deploy staging)
  │                       │           │     │                 └─→ T040 (Monitor)
  │                       │           │     └─→ T034-T035 (Run tests)
  │                       │           │           └─→ T036 (Verify warnings)
  │                       │           ├─→ T028 (Update OCC tests)
  │                       │           └─→ T033 (Create OCC docs)
  │                       └─→ T029-T030 (Type nce tests)
  └─→ T003 (Backup schema)
```

## Priority Breakdown

- **Priority 0 (Critical):** 3 tasks - Preparation phase
- **Priority 1 (High):** 27 tasks - Core migration work and deployment
- **Priority 2 (Medium):** 10 tasks - Tests, documentation, and monitoring

## Files to Delete (8 files)

1. `shared/Models/ConflictBackup.cs`
2. `shared/Models/SyncLog.cs`
3. `backend/src/Quater.Backend.Data/Configurations/ConflictBackupConfiguration.cs`
4. `backend/src/Quater.Backend.Data/Configurations/SyncLogConfiguration.cs`
5. `backend/src/Quater.Backend.Sync/BackupService.cs`
6. `backend/src/Quater.Backend.Sync/ConflictResolver.cs`
7. `backend/src/Quater.Backend.Sync/SyncLogService.cs`
8. `backend/src/Quater.Backend.Core/Interfaces/IBackupService.cs`

## Files to Modify (10 files)

1. `shared/Models/AuditLog.cs` - Remove ConflictBackupId, ConflictResolutionNotes, ConflictBackup navigation
2. `shared/Models/AuditLogArchive.cs` - Remove ConflictBackupId, ConflictResolutionNotes, ConflictBackup navigation
3. `shared/Models/User.cs` - Keep AuditLogs collection (no circular dependency anymore)
4. `shared/Enums/EntityType.cs` - Remove ConflictBackup and SyncLog enum values
5. `backend/src/Quater.Backend.Data/QuaterDbContext.cs` - Remove ConflictBackups and SyncLogs DbSets
6. `backend/src/Quater.Backend.Data/Configurations/AuditLogConfiguration.cs` - Remove ConflictBackup relationship config
7. `backend/src/Quater.Backend.Data/Configurations/AuditLogArchiveConfiguration.cs` - Remove ConflictBackup relationship config
8. `backend/src/Quater.Backend.Data/Configurations/UserConfiguration.cs` - Keep AuditLogs relationship
9. `backend/src/Quater.Backend.Sync/SyncService.cs` - Simplify conflict handling (no ConflictBackup creation)
10. `desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs` - Remove ConflictBackups and SyncLogs DbSets

## Acceptance Cra (All Tasks)

Each task must verify:
1. ✅ No compilation errors
2. ✅ No Rider IDE warnings
3. ✅ No circular dependencies
4. ✅ All existing tests still pass
5. ✅ AuditLog functionality preserved
6. ✅ OCC (RowVersion) still works

## Getting Started

To begin the refactoring:

```bash
# View the first task
bd show Quater-bmr2

# View all ready tasks
bd ready

# Start working on T001
bd update Quater-bmr2 --status in_progress

# When complete
bd update Quater-bmr2 --status done
```

## Viewing Dependencies

```bash
# View a specific task with dependencies
bd show Quater-phs2

# View dependency graph for a task
bd graph er-phs2

# View all blocked tasks
bd blocked
```

## Progress Tracking

```bash
# View overall status
bd status

# List all refactoring tasks
bd list --json | jq -r '.[] | select(.title | startswith("T0")) | "\(.id) - \(.status) - \(.title)"'

# Count completed tasks
bd list --json | jq -r '.[] | select(.title | startswith("T0")) | select(.status == "done")' | jq -s 'length'
```

## Notes

- All dependencies have been configured in Beads
- Tasks will automatically become "ready" when their dependencies are completed
- The migration is designed to be reversible (Down() method)
- Database backups should be taken before applying migrations
- Staging deployment should be monitored for 2 weeks before production

## Related Documentation

- `MODEL_SYNC_GUIDE.md` - Current sync architecture (to be updated in T032)
- `REFACTORING_COMPLETE.md` - Previous refactoring work
- `REFACTORING_PROGRESS.md` - Ongoing refactoring status

---

**Created by:** AI Assistant  
**Date:** 2026-02-01  
**Beads Version:** Latest  
**Project:** Quater - Laboratory Information Management System

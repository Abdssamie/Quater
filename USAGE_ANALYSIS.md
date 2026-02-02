# Usage Analysis: ConflictBackup and SyncLog

**Date:** 2026-02-02
**Task:** T002

## Overview
This document catalogs the current usage of `ConflictBackup` and `SyncLog` entities in the Quater codebase to guide their safe removal.

## ConflictBackup

### Models
- `shared/Models/ConflictBackup.cs`: The entity definition.
- `shared/Models/AuditLog.cs`: Has `ConflictBackupId` (FK) and `ConflictBackup` (Navigation property).
- `shared/Models/AuditLogArchive.cs`: Has `ConflictBackupId` (FK) and `ConflictBackup` (Navigation property).
- `shared/Enums/EntityType.cs`: Enum value `ConflictBackup`.

### Data Layer
- `backend/src/Quater.Backend.Data/QuaterDbContext.cs`: `DbSet<ConflictBackup>`.
- `backend/src/Quater.Backend.Data/Configurations/ConflictBackupConfiguration.cs`: EF Core configuration.
- `backend/src/Quater.Backend.Data/Configurations/AuditLogConfiguration.cs`: Configures relationship with `ConflictBackup`.
- `backend/src/Quater.Backend.Data/Repositories/UnitOfWork.cs`: Likely accesses the repository or DbSet.
- `backend/src/Quater.Backend.Data/Interfaces/IUnitOfWork.cs`: Interface definition.

### Services
- `backend/src/Quater.Backend.Sync/BackupService.cs`: Service handling backup logic.
- `backend/src/Quater.Backend.Core/Interfaces/IBackupService.cs`: Interface for the service.
- `backend/src/Quater.Backend.Core/Constants/ErrorMessages.cs`: Error messages related to backup.

## SyncLog

### Models
- `shared/Models/SyncLog.cs`: The entity definition.
- `shared/Models/User.cs`: Likely has a collection of `SyncLogs`.
- `shared/Enums/EntityType.cs`: Enum value `SyncLog`.

### Data Layer
- `backend/src/Quater.Backend.Data/QuaterDbContext.cs`: `DbSet<SyncLog>`.
- `desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs`: `DbSet<SyncLog>` (Local DB).
- `backend/src/Quater.Backend.Data/Configurations/SyncLogConfiguration.cs`: EF Core configuration.
- `backend/src/Quater.Backend.Data/Configurations/UserConfiguration.cs`: Configures relationship with `SyncLog`.
- `backend/src/Quater.Backend.Data/Repositories/UnitOfWork.cs`: Repository access.
- `backend/src/Quater.Backend.Data/Interfaces/IUnitOfWork.cs`: Interface.

### Services
- `backend/src/Quater.Backend.Sync/SyncLogService.cs`: Service for sync logging.
- `backend/src/Quater.Backend.Sync/SyncService.cs`: Uses `SyncLog` during synchronization.
- `backend/src/Quater.Backend.Core/Interfaces/ISyncLogService.cs`: Interface.
- `backend/src/Quater.Backend.Core/Constants/ErrorMessages.cs`: Related error messages.

### Desktop
- `desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs`: Includes `SyncLog` in local context.

## Navigation & Dependencies
- **AuditLog -> ConflictBackup**: Strong dependency. `AuditLog` references `ConflictBackup`. This is the primary circular dependency connector in some contexts (though AuditLog doesn't point back to User directly in a way that causes the cycle, the chain User -> AuditLog -> ConflictBackup -> ... might be the issue).
- **User -> SyncLog**: User has `SyncLogs`.

## Plan Verification
The proposed plan in `REFACTORING_ISSUES_CREATED.md` correctly covers these files.
- `Phase 2` addresses Model references.
- `Phase 3` addresses EF Configurations.
- `Phase 4` addresses Model files.
- `Phase 5` addresses Services.

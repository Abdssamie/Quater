# Tasks: Water Quality Lab Management System

**Feature Branch**: `001-water-quality-platform`  
**Created**: 2026-01-25  
**Status**: Phase 1 - Foundation Setup

**Methodology**: Iterative, on-demand task creation. We create essential tasks, implement them, review progress, then create the next batch based on what we learn.

---

## Task Format

```
- [ ] [TaskID] [P?] [Story?] Description with file path
```

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (US1, US2, etc.) - only for user story tasks
- **TaskID**: Sequential (T001, T002, T003...)

---

## Phase 1: Foundation Setup (Current Phase)

**Goal**: Establish project structure, install dependencies, and implement core domain models.

**Why these tasks first**: You cannot build features without a foundation. These tasks create the skeleton that all future work depends on.

---

### Setup Tasks

- [X] T001 Create root project structure with backend/, desktop/, mobile/, shared/, docker/ directories
- [X] T002 [P] Initialize .NET 10 backend solution in backend/Quater.Backend.sln with projects: Api, Core, Data, Sync
- [X] T003 [P] Initialize .NET 10 desktop solution in desktop/Quater.Desktop.sln with projects: Desktop, Data, Sync
- [X] T004 [P] Initialize React Native project in mobile/ using `npx react-native init QuaterMobile --template react-native-template-typescript`
- [X] T005 Configure yarn for mobile project (verify package.json uses yarn, create .yarnrc.yml if needed)

---

### Backend Dependencies & Configuration

- [X] T006 Install backend core packages in backend/src/Quater.Backend.Api/Quater.Backend.Api.csproj:
  - Microsoft.AspNetCore.OpenApi (10.0.0)
  - Swashbuckle.AspNetCore (7.2.0)
  - Microsoft.EntityFrameworkCore.Design (10.0.0)
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.0)
  - OpenIddict.AspNetCore (5.8.0)
  - OpenIddict.EntityFrameworkCore (5.8.0)

- [X] T007 [P] Install backend data packages in backend/src/Quater.Backend.Data/Quater.Backend.Data.csproj:
  - Microsoft.EntityFrameworkCore (10.0.0)
  - Npgsql.EntityFrameworkCore.PostgreSQL (10.0.0)
  - Microsoft.EntityFrameworkCore.Tools (10.0.0)

- [X] T008 [P] Install backend testing packages in backend/tests/Quater.Backend.Core.Tests/Quater.Backend.Core.Tests.csproj:
  - xUnit (2.9.3)
  - xUnit.runner.visualstudio (3.1.4)
  - FluentAssertions (7.0.0)
  - Moq (4.20.0)
  - Microsoft.NET.Test.Sdk (17.14.1)

- [X] T009 [P] Install QuestPDF in backend/src/Quater.Backend.Core/Quater.Backend.Core.csproj:
  - QuestPDF (2025.1.0)

- [X] T010 Configure appsettings.json in backend/src/Quater.Backend.Api/ with ConnectionStrings, OpenIddict, Logging sections

---

### Desktop Dependencies & Configuration

- [X] T011 Install desktop core packages in desktop/src/Quater.Desktop/Quater.Desktop.csproj:
  - Avalonia (11.3.8)
  - Avalonia.Desktop (11.3.8)
  - Avalonia.Themes.Fluent (11.3.8)
  - Avalonia.ReactiveUI (11.3.8)

- [X] T012 [P] Install desktop data packages in desktop/src/Quater.Desktop.Data/Quater.Desktop.Data.csproj:
  - Microsoft.EntityFrameworkCore.Sqlite (10.0.0)
  - Microsoft.EntityFrameworkCore.Design (10.0.0)

- [X] T013 [P] Install QuestPDF in desktop/src/Quater.Desktop/Quater.Desktop.csproj:
  - QuestPDF (2025.1.0)

---

### Mobile Dependencies & Configuration

- [X] T014 Install mobile core packages using yarn in mobile/:
  ```bash
  yarn add react-native-sqlite-storage
  yarn add react-native-geolocation-service
  yarn add @react-navigation/native @react-navigation/stack
  yarn add react-native-screens react-native-safe-area-context
  ```

- [ ] T015 [P] Install mobile dev dependencies using yarn in mobile/:
  ```bash
  yarn add -D @types/react @types/react-native
  yarn add -D @typescript-eslint/eslint-plugin @typescript-eslint/parser
  yarn add -D eslint prettier
  yarn add -D jest @testing-library/react-native @testing-library/jest-native
  ```

- [ ] T016 Configure TypeScript in mobile/tsconfig.json with strict mode, paths for generated API client

- [ ] T017 Configure ESLint in mobile/.eslintrc.js with TypeScript rules and React Native plugin

- [ ] T018 Configure Prettier in mobile/.prettierrc.js with project code style

---

### Domain Models - Backend Core

**Note**: These are the foundational entities that all features depend on. Implementing them now allows parallel feature development later.

- [ ] T019 [P] Create Sample entity in backend/src/Quater.Backend.Core/Models/Sample.cs with all fields from data-model.md (Id, Type, LocationLatitude, LocationLongitude, LocationDescription, LocationHierarchy, CollectionDate, CollectorName, Notes, Status, Version, LastModified, LastModifiedBy, IsDeleted, IsSynced, LabId, CreatedBy, CreatedDate)

- [ ] T020 [P] Create TestResult entity in backend/src/Quater.Backend.Core/Models/TestResult.cs with all fields from data-model.md (Id, SampleId, ParameterName, Value, Unit, TestDate, TechnicianName, TestMethod enum, ComplianceStatus, Version, LastModified, LastModifiedBy, IsDeleted, IsSynced, CreatedBy, CreatedDate)

- [ ] T021 [P] Create Parameter entity in backend/src/Quater.Backend.Core/Models/Parameter.cs with all fields from data-model.md (Id, Name, Unit, WhoThreshold, MoroccanThreshold, MinValue, MaxValue, Description, IsActive, CreatedDate, LastModified)

- [ ] T022 [P] Create User entity in backend/src/Quater.Backend.Core/Models/User.cs extending IdentityUser with additional fields (LabId, Role, CreatedDate, LastLogin, IsActive)

- [ ] T023 [P] Create Lab entity in backend/src/Quater.Backend.Core/Models/Lab.cs with all fields from data-model.md (Id, Name, Location, ContactInfo, CreatedDate, IsActive)

- [ ] T024 [P] Create SyncLog entity in backend/src/Quater.Backend.Core/Models/SyncLog.cs with all fields from data-model.md (Id, DeviceId, UserId, LastSyncTimestamp, Status, ErrorMessage, RecordsSynced, ConflictsDetected, ConflictsResolved, CreatedDate)

- [ ] T025 [P] Create AuditLog entity in backend/src/Quater.Backend.Core/Models/AuditLog.cs with all fields from data-model.md (Id, UserId, EntityType, EntityId, Action, OldValue, NewValue, ConflictResolutionNotes, Timestamp, IpAddress, IsArchived)

- [ ] T026 [P] Create AuditLogArchive entity in backend/src/Quater.Backend.Core/Models/AuditLogArchive.cs with same schema as AuditLog plus ArchivedDate

---

### Domain Models - Desktop

- [ ] T027 Create desktop Sample entity in desktop/src/Quater.Desktop.Data/Models/Sample.cs (same schema as backend)

- [ ] T028 [P] Create desktop TestResult entity in desktop/src/Quater.Desktop.Data/Models/TestResult.cs (same schema as backend)

- [ ] T029 [P] Create desktop Parameter entity in desktop/src/Quater.Desktop.Data/Models/Parameter.cs (same schema as backend)

- [ ] T030 [P] Create desktop SyncLog entity in desktop/src/Quater.Desktop.Data/Models/SyncLog.cs (same schema as backend)

---

### Database Context Setup

- [ ] T031 Create QuaterDbContext in backend/src/Quater.Backend.Data/QuaterDbContext.cs:
  - Inherit from IdentityDbContext<User>
  - Add DbSet properties for all entities
  - Configure entity relationships in OnModelCreating
  - Add indexes from data-model.md
  - Configure optimistic concurrency tokens (Version, LastModified)

- [ ] T032 Create QuaterLocalContext in desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs:
  - Inherit from DbContext
  - Add DbSet properties for Sample, TestResult, Parameter, SyncLog
  - Configure SQLite-specific settings (WAL mode)
  - Add indexes from data-model.md

---

### Enums & Value Objects

- [ ] T033 [P] Create SampleType enum in backend/src/Quater.Backend.Core/Enums/SampleType.cs (DrinkingWater, Wastewater, SurfaceWater, Groundwater, IndustrialWater)

- [ ] T034 [P] Create SampleStatus enum in backend/src/Quater.Backend.Core/Enums/SampleStatus.cs (Pending, Completed, Archived)

- [ ] T035 [P] Create TestMethod enum in backend/src/Quater.Backend.Core/Enums/TestMethod.cs (Titration, Spectrophotometry, Chromatography, Microscopy, Electrode, Culture, Other)

- [ ] T036 [P] Create ComplianceStatus enum in backend/src/Quater.Backend.Core/Enums/ComplianceStatus.cs (Pass, Fail, Warning)

- [ ] T037 [P] Create UserRole enum in backend/src/Quater.Backend.Core/Enums/UserRole.cs (Admin, Technician, Viewer)

---

### Initial Database Migration

- [ ] T038 Create initial EF Core migration for backend in backend/src/Quater.Backend.Data/:
  ```bash
  dotnet ef migrations add InitialCreate --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api
  ```

- [ ] T039 Create initial EF Core migration for desktop in desktop/src/Quater.Desktop.Data/:
  ```bash
  dotnet ef migrations add InitialCreate --project desktop/src/Quater.Desktop.Data --startup-project desktop/src/Quater.Desktop
  ```

---

### Docker Setup

- [ ] T040 Create Dockerfile for backend in docker/Dockerfile.backend with .NET 10 runtime, multi-stage build

- [ ] T041 Create docker-compose.yml in docker/ with services: backend API, PostgreSQL 15, pgAdmin (optional)

- [ ] T042 Create database initialization script in docker/init.sql with default admin user, default lab, WHO parameter standards

---

### Configuration & Infrastructure

- [ ] T043 Create Program.cs in backend/src/Quater.Backend.Api/ with:
  - WebApplicationBuilder setuce registration (DbContext, Identity, OpenIddict)
  - Middleware pipeline (CORS, Auth, Swagger)
  - Database migration on startup

- [ ] T044 Create App.axaml and App.axaml.cs in desktop/src/Quater.Desktop/ with Avalonia application setup and ReactiveUI configuration

- [ ] T045 Create App.tsx in mobile/src/ with React Navigation setup and root navigator

---

### Validation & Testing Setup

- [ ] T046 Create SampleValidator in backend/src/Quater.Backend.Core/Validators/SampleValidator.cs using FluentValidation with rules from data-model.md

- [ ] T047 [P] Create TestResultValidator in backend/src/Quater.Backend.Core/Validators/TestResultValidator.cs using FluentValidation with rules from data-model.md

- [ ] T048 Create sample unit test in backend/tests/Quater.Backend.Core.Tests/Models/SampleTests.cs to verify entity creation and validation

---

## Phase 1 Completion Checklist

Before moving to Phase 2 (feature implementation), verify:

- [ ] All projects build successfully (`dotnet build` for backend/desktop, `yarn android` for mobile)
- [ ] All dependencies installed without errors
- [ ] Database migrations created and can be applied
- [ ] Domain models match data-model.md specifications
- [ ] Docker Compose can start backend PostgreSQL
- [ ] At least one unit test runs successfully

---

## Next Steps After Phase 1

Once Phase 1 is complete, we'll create Phase 2 tasks based on what we learn. Likely next phases:

**Phase 2 Options** (decide after Phase 1):
1. **Authentication & Authorization**: ASP.NET Core Identity + OpenIddict setup
2. **User Story 1**: Mobile field sample collection (highest priority)
3. **Backend API**: REST endpoints for samples and test results
4. **Desktop UI**: Avalonia views and view models for sample management

**Decision Point**: After Phase 1, review what's working and decide which phase to tackle next based on:
- What's blocking other work
- What delivers the most value
- What you're most interested in building

---

## Parallel Execution Opportunities

Tasks marked with **[P]** can be executed in parallel if you have multiple developers or want to use AI agents:

**Parallel Group 1** (Project Setup):
- T002, T003, T004 (initialize all three projects simultaneously)

**Parallel Group 2** (Dependencies):
- T007, T008, T009 (backend packages)
- T011, T012, T013 (desktop packages)
- T014, T015 (mobile packages)

**Parallel Group 3** (Domain Models):
- T019-T026 (all backend entities)
- T027-T030 (all desktop entities)
- T033-T037 (all enums)

---

## Notes

- **Commit frequently**: After each task or logical group of tasks
- **Test as you go**: Don't wait until the end to test
- **Update this file**: Add new tasks as you discover what's needed
- **Mark completed**: Check off tasks as you finish them
- **Ask questions**: If a task is unclear, ask before implementing

---

**Current Phase**: Phase 1 - Foundation Setup  
**Total Tasks**: 48 tasks  
**Estimated Time**: 2-3 days for experienced developer  
**Next Review**: After completing Phase 1, assess progress and create Phase 2 tasks

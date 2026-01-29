# Handoff Prompt: Water Quality Platform - Phase 2+ Continuation

## Project Context

You're continuing development of **Quater**, a water quality lab management system for Moroccan labs. This is an **offline-first, multi-platform application** with backend API, desktop app, and mobile app that sync data.

**Branch:** `001-water-quality-platform`
**Current Phase:** Phase 2 Complete (77% done) → Continue with remaining tasks
**Project Root:** `/home/abdssamie/ChemforgeProjects/Quater/`

## What's Been Completed (37/48 tasks)

### ✅ Phase 1: Foundation (Complete)
- .NET 10 backend solution (Api, Core, Data, Sync projects)
- .NET 10 desktop solution (Desktop, Data, Sync projects)
- React Native mobile project initialized
- All core packages installed (EF Core, Avalonia, React Native, QuestPDF)
- Configuration files (appsettings.json, tsconfig.json, eslint, prettier)

### ✅ Phase 2: Database Setup (Complete)
- **5 Backend Enums:** SampleType, SampleStatus, TestMethod, ComplianceStatus, UserRole
- **8 Backend Entities:** Sample, TestResult, Parameter, User, Lab, SyncLog, AuditLog, AuditLogArchive
- **4 Desktop Entities:** Sample, TestResult, Parameter, SyncLog (same schema as backend)
- **QuaterDbContext:** Backend DbContext with IdentityDbContext<User>, all relationships, indexes
- **QuaterLocalContext:** Desktop DbContext with SQLite configuration, WAL mode
- **EF Core Migrations:** Initial migrations created for both backend and desktop
- **Model Documentation:** All models have sync warnings (see MODEL_SYNC_GUIDE.md)

## What's Left (11 tasks - 23%)

### High Priority Tasks (Ready to Work)

#### 1. Mobile Setup (2 tasks)
- **Quater-vaf** (T015): Install mobile dev dependencies using yarn
  - Packages: @types/react, @types/react-native, @typescript-eslint/*, eslint, prettier, jest, @testing-library/*
  - Location: `mobile/`

- **Quater-5vw** (T016): Configure TypeScript in mobile/tsconfig.json
  - Strict mode enabled
  - Paths for generated API client

#### 2. Application Entry Points (3 tasks - NOW UNBLOCKED!)
- **Quater-7hh** (T043): Create Program.cs in backend/src/Quater.Backend.Api/
  - WebApplicationBuilder service registration (DbContext, Identity, OpenIddict)
  - Middleware pipeline (CORS, Auth, Swagger)
  - Database migration on startup
  - **Note:** Minimal Program.cs exists for migrations - needs full implementation

- **Quater-k7a** (T044): Create App.axaml and App.axaml.cs in desktop/src/Quater.Desktop/
  - Avalonia application setup with ReactiveUI
  - **Note:** Minimal App.axaml.cs exists for migrations - needs full implementation

- **Quater-7sj** (T045): Create App.tsx in mobile/src/
  - React Navigation setup and root navigator

#### 3. Docker & Deployment (3 tasks)
- **Quater-dln** (T040): Create Dockerfile for backend in docker/Dockerfile.backend
  - .NET 10 runtime, multi-stage build

- **Quater-5fd** (T041): Create docker-compose.yml in docker/
  - Services: backend API, PostgreSQL 15, pgAdmin (optional)

- **Quater-hpm** (T042): Create database initialization script in docker/init.sql
  - Default admin user, default lab, WHO parameter standards

#### 4. Validation (2 tasks)
- **Quater-2sb** (T046): Create SampleValidator in backend/src/Quater.Backend.Core/Validators/
  - FluentValidation with rules from data-model.md

- **Quater-c74** (T047): Create TestResultValidator in backend/src/Quater.Backend.Core/Validators/
  - FluentValidation with rules from data-model.md

#### 5. Testing (1 task)
- **Quater-4rz** (T048): Create sample unit test in backend/tests/Quater.Backend.Core.Tests/Models/SampleTests.cs
  - Verify Sample entity creation and validation

## Critical Information

### ⚠️ Model Synchronization (VERY IMPORTANT!)

**Models are duplicated in 3 locations** (temporary until Phase 3):
1. **Backend** (MASTER): `backend/src/Quater.Backend.Core/Models/` - uses C# enums
2. **Desktop**: `desktop/src/Quater.Desktop.Data/Models/` - uses string enums for SQLite
3. **Mobile** (future): `mobile/src/models/` - TypeScript (generated from API)

**When modifying any shared model (Sample, TestResult, Parameter, SyncLog):**
1. Update backend model first (it's the master)
2. Copy changes to desktop model (convert C# enums → strings)
3. Update DbContext configurations if relationships change
4. Run migrations for BOTH backend and desktop
5. Regenerate mobile TypeScript types from OpenAPI/Swagger

**Read this first:** `MODEL_SYNC_GUIDE.md` - Complete synchronization checklist

### Project Structure
```
/home/abdssamie/ChemforgeProjects/Quater/
├── backend/
│   ├── src/
│   │   ├── Quater.Backend.Api/          # ASP.NET Core API
│   │   ├── Quater.Backend.Core/         # Entities, Enums, Validators
│   │   ├── Quater.Backend.Data/         # DbContext, Migrations
│   │   └── Quater.Backend.Sync/         # Sync logic
│   └── tests/
│       └── Quater.Backend.Core.Tests/   # Unit tests
├── desktop/
│   └── src/
│       ├── Quater.Desktop/              # Avalonia UI app
│       ├── Quater.Desktop.Data/         # DbContext, Migrations, Models
│       └── Quater.Desktop.Sync/         # Sync logic
├── mobile/
│   └── src/                             # React Native app
├── specs/001-water-quality-platform/
│   ├── spec.md                          # Requirements & user stories
│   ├── plan.md                          # All 48 tasks breakdown
│   └── data-model.md                    # Complete entity definitions (PRIMARY REFERENCE)
├── MODEL_SYNC_GUIDE.md                  # Model synchronization guide
└── .beads/                            # Beads task tracking
```

### Key Reference Files
- **Data Model (PRIMARY):** `specs/001-water-quality-platform/data-model.md` - Complete entity definitions, relationships, indexes, validation rules
- **Implementation Plan:** `specs/001-water-quality-platform/plan.md` - All 48 tasks across 6 phases
- **Requirements:** `specs/001-water-quality-platform/spec.md` - User stories, acceptance criteria
- **Model Sync Guide:** `MODEL_SYNC_GUIDE.md` - How to maintain models across platforms

### Technical Stack
- **Backend:** .NET 10, ASP.NET Core, EF Core 10.0.2, PostgreSQL 15+ty, OpenIddict
- **Desktop:** .NET 10, Avalonia UI 11.3.8, EF Core SQLite 10.0, ReactiveUI
- **Mobile:** React Native 0.83.1, TypeScript, SQLite, React Navigation
- **PDF:** QuestPDF 2025.1.0
- **Testing:** xUnit, FluentAssertions, Moq

### Build Status
- ✅ Backend solution builds successfully
- ✅ Desktop solution builds successfully
- ⚠️ Mobile not yet configured (tasks T015, T016)

## Beads Workflow (Task Management)

### Essential Commands
```bash
# Check ready tasks
bd ready --json

# Start work on a task
bd update <task-id> --status=in_progress --json

# Complete a task
bd close <task-id> --reason "description of what was done" --json

# Check project statistics
bd stats --json

# Check blocked tasks
bd blocked --json

# Show task details
bd show <task-id> --json
```

### Session Close Protocol (CRITICAL!)
Before ending your session, you MUST:
```bash
# 1. Export beads database
bd export

# 2. Sync with main (may fail on ephemeral branch - that's OK)
bd sync --from-main

# 3. Stage and commit changes
git add <files>
git commit -m "descriptive message"

# 4. Check final status
bd stats --json
git log --oneline -3
```

### Beads Best Practices
- Use `bd ready --json` to see available tasks
- Only work on tasks with `status: "open"` and no blockers
- Update task to `in_progress` when you start
- Close tasks with detailed `--reason` explaining what was done
- If you discover new work, create new tasks with `bd create`
- Always run `bd export` before session end (exports DB to .beads/issues.jsonl)

## Getting Started

### Step 1: Check Current Status
```bash
cd /home/abdssamie/ChemforgeProjects/Quater
bd ready --json
bd stats --json
git status
```

### Step 2: Choose a Task
Recommended order:
1. **Mobile setup** (T015, T016) - Unblocks mobile development
2. **Program.cs** (T043) - Unblockd API development
3. **App.axaml** (T044) - Unblocks desktop UI development
4. **Docker setup** (T040, T041, T042) - Enables deployment
5. **Validators** (T046, T047) - Adds data validation
6. **Tests** (T048) - Adds test coverage

### Step 3: Claim and Work
```bash
# Claim the task
bd update <task-id> --status=in_progress --json

# Do the work (refer to specs/001-water-quality-platform/plan.md for details)

# Build and verify
dotnet build backend/Quater.Backend.sln  # or desktop/Quater.Desktop.sln

# Close the task
bd close <task-id> --reason "detailed description" --json
```

### Step 4: Commit Changes
```bash
# Stage changes
git add <files>

# Commit with descriptive message
git commit -m "feat: implement <feature>

- Detail 1
- Detail 2
- Detail 3

Closes: <task-id>"

# Export beads
bd export
```

## Important Notes

### Database Migrations
- Backend uses PostgreSQL (connection string in appsettings.json)
- Desktop uses SQLite (quater.db file)
- Both have initial migrations already created
- If you modify models, create new migrations for BOTH platforms

### Identity & Authentication
- Backend uses ASP.NET Core Identity with custom User entity
- User extends IdentityUser with LabId, Role, CreatedDate, LastLogin, IsActive
- OpenIddict configured for OAuth2/OIDC (not yet implemented)

### Sync Architecture
- Offline-first: Desktop and mobile work without internet
- Optimistic locking: Version field + LastModified timestamp
- Soft deletes: IsDeleted flag preserves data for sync
- Conflict resolution: Last-Write-Wins with audit trail

### Audit Trail
- Hot data: AuditLog table (last 90 days)
- Cold data: AuditLogArchive table (90+ days)
- Nightly archival job (not yet implemented)
- 7-year retention for regulatory compliance

## Common Tasks

### Adding a New Ent. Create entity in `backend/src/Quater.Backend.Core/Models/`
2. Add to `QuaterDbContext.cs` with configuration
3. If shared with desktop, create in `desktop/src/Quater.Desktop.Data/Models/`
4. Add to `QuaterLocalContext.cs` with configuration
5. Create migrations for both platforms
6. Add sync warnings to model documentation

### Running Migrations
```bash
# Backend
dotnet ef migrations add <MigrationName> \
  --project backend/src/Quater.Backend.Data \
  --startup-project backend/src/Quater.Backend.Api

# Desktop
dotnet ef migrations add <MigrationName> \
  --project desktop/src/Quater.Desktop.Data \
  --startup-project desktop/src/Quater.Desktop
```

### Building Solutions
```bash
# Backend
dotnet build backend/Quater.Backend.sln

# Desktop
dotnet build desktop/Quater.Desktop.sln

# Mobile (when configured)
cd mobile && yarn android  # or yarn ios
```

## Troubleshooting

### Git Lock File Issues
```bash
rm -f /home/abdssamie/ChemforgeProjects/Quater/.git/index.lock
```

### Package Version Conflicts
- EF Core should be 10.0.2 (not 10.0.0)
- Identity should be 10.0.2
- Npgsql should be 10.0.0

### LSP Errors in IDE
- These are often false positives
- Run `dotnet build` to verify actual build status
- Restart OmniSharp if needed

## Success Criteria

### For Each Task
- [ ] Code builds without errors
- [ ] Follows existing patterns and conventions
- [ ] Includes XML documentation comments
- [ ] Updates relevant configuration files
- [ ] Creates migrations if models changed
- [ ] Tests pass (if applicable)
- [ ] Task closed in beads with detailed reason
- [ ] Changes committed to git

### For Session End
- [ ] All in-progress tasks completed or reverted to open
- [ ] `bd export` executed successfully
- [ ] All changes committed to git
- [ ] `bd stats` shows updated progress
- [ ] No uncommitted changes in `git status`

## Qus?

If you're unsure about anything:
1. Check the specs in `specs/001-water-quality-platform/`
2. Look at existing code for patterns
3. Read `MODEL_SYNC_GUIDE.md` for model changes
4. Use `bd show <task-id>` for task details
5. Check git history: `git log --oneline -10`

## Ready to Start?

Run these commands to begin:
```bash
cd /home/abdssamie/ChemforgeProjects/Quater
bd ready --json
bd stats --json
```

Pick a task from the ready list and start coding! 🚀

---

**Last Updated:** 2026-01-27
**Progress:** 37/48 tasks (77%)
**Branch:** `001-water-quality-platform`
**Next Milestone:** Complete Phase 2 remaining tasks (11 tasks)

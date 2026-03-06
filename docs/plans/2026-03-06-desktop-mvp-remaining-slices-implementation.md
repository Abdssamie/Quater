# Desktop MVP Remaining Slices Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the remaining desktop MVP slices (dashboard replacement, compliance audit workflow, sync/conflict center, and production hardening) with test-first delivery and merge-safe parallel tracks.

**Architecture:** Build shared cross-slice services first (error formatting, permissions, export, sync state), then implement feature tracks in parallel worktrees and merge back into local `main` with full regression verification. Keep offline-first behavior by preserving current view state when API calls fail and updating incrementally as data arrives.

**Tech Stack:** .NET 10, Avalonia 11, CommunityToolkit.Mvvm, generated OpenAPI clients, xUnit.

---

## Global Preflight (Do Once)

### Task 0: Prepare Baseline and Worktrees

**Files:**
- Modify: `.gitignore` (only if `.worktrees/` is not ignored)

**Step 1: Verify worktree parent directory exists**

Run: `ls -d .worktrees`
Expected: `.worktrees` directory exists.

**Step 2: Verify ignore status for worktree directory**

Run: `git check-ignore -v .worktrees`
Expected: `.worktrees` is ignored.

**Step 3: Run baseline tests on current local main**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`
Expected: PASS.

**Step 4: Create worktrees and branches**

Run:
- `git worktree add .worktrees/desktop-foundation -b feature/desktop-mvp-foundation`
- `git worktree add .worktrees/desktop-dashboard -b feature/desktop-mvp-dashboard`
- `git worktree add .worktrees/desktop-audit -b feature/desktop-mvp-audit`
- `git worktree add .worktrees/desktop-sync -b feature/desktop-mvp-sync-center`
- `git worktree add .worktrees/desktop-hardening -b feature/desktop-mvp-hardening`

Expected: all worktrees created.

**Step 5: Restore and verify in each worktree**

Run in each worktree:
- `dotnet restore Quater.sln`
- `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`

Expected: restore and tests succeed.

---

## Track 1: Shared Foundations

### Task 1F: Add Failing Tests for API Error Normalization

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Core/Api/ApiErrorFormatterTests.cs`
- Create: `desktop/src/Quater.Desktop/Core/Api/IApiErrorFormatter.cs` (skeleton only after RED verification)

**Step 1: Write failing test for API exception mapping**

Add tests for:
- unauthorized -> "Session expired"
- forbidden -> "Permission denied"
- validation/400 -> "Invalid request"
- generic API failure -> fallback message

**Step 2: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~ApiErrorFormatterTests"`
Expected: FAIL (type not implemented).

**Step 3: Implement minimal formatter interface + class**

Create `IApiErrorFormatter` and `ApiErrorFormatter` in `desktop/src/Quater.Desktop/Core/Api/`.

**Step 4: Run targeted tests and verify pass**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~ApiErrorFormatterTests"`
Expected: PASS.

**Step 5: Commit**

Run:
- `git add desktop/Quater.Desktop.Tests/Core/Api/ApiErrorFormatterTests.cs desktop/src/Quater.Desktop/Core/Api/IApiErrorFormatter.cs desktop/src/Quater.Desktop/Core/Api/ApiErrorFormatter.cs`
- `git commit -m "feat: add normalized api error formatter for desktop workflows"`

### Task 2F: Add Failing Tests for Permission Service

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Core/Auth/PermissionServiceTests.cs`
- Create: `desktop/src/Quater.Desktop/Core/Auth/Services/IPermissionService.cs` (skeleton after RED verification)

**Step 1: Write failing tests for permission checks**

Cover:
- audit workflow visibility
- sync center visibility
- create/edit/delete action permissions for samples and test results

**Step 2: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~PermissionServiceTests"`
Expected: FAIL.

**Step 3: Implement minimal permission service**

Implement role/claim-based checks using current app auth state shape.

**Step 4: Run targeted tests and verify pass**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~PermissionServiceTests"`
Expected: PASS.

**Step 5: Commit**

Run:
- `git add desktop/Quater.Desktop.Tests/Core/Auth/PermissionServiceTests.cs desktop/src/Quater.Desktop/Core/Auth/Services/IPermissionService.cs desktop/src/Quater.Desktop/Core/Auth/Services/PermissionService.cs`
- `git commit -m "feat: add desktop permission service for role-aware ux gating"`

### Task 3F: Add Failing Tests for CSV Export Service

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Core/Export/CsvExportServiceTests.cs`
- Create: `desktop/src/Quater.Desktop/Core/Export/ICsvExportService.cs` (skeleton after RED verification)

**Step 1: Write failing tests for deterministic CSV generation**

Cover:
- stable header ordering
- CSV escaping for commas/quotes/newlines
- empty input returns headers only

**Step 2: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~CsvExportServiceTests"`
Expected: FAIL.

**Step 3: Implement minimal export service**

Return CSV text from row models used by audit workflow.

**Step 4: Run targeted tests and verify pass**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~CsvExportServiceTests"`
Expected: PASS.

**Step 5: Commit**

Run:
- `git add desktop/Quater.Desktop.Tests/Core/Export/CsvExportServiceTests.cs desktop/src/Quater.Desktop/Core/Export/ICsvExportService.cs desktop/src/Quater.Desktop/Core/Export/CsvExportService.cs`
- `git commit -m "feat: add csv export service for compliance audit data"`

### Task 4F: Add Failing Tests for Sync Status Aggregation Service

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Core/Sync/SyncStatusServiceTests.cs`
- Create: `desktop/src/Quater.Desktop/Core/Sync/ISyncStatusService.cs` (skeleton after RED verification)

**Step 1: Write failing tests for sync summary mapping**

Cover:
- pending/failed/in-progress counts
- last sync timestamp status text mapping
- retry intent updates pending state

**Step 2: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SyncStatusServiceTests"`
Expected: FAIL.

**Step 3: Implement minimal sync status service**

Use AppState + lightweight in-memory model for queue and failed operations.

**Step 4: Run targeted tests and verify pass**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SyncStatusServiceTests"`
Expected: PASS.

**Step 5: Register core services and run full tests**

Modify: `desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs`

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`
Expected: PASS.

**Step 6: Commit**

Run:
- `git add desktop/Quater.Desktop.Tests/Core/Sync/SyncStatusServiceTests.cs desktop/src/Quater.Desktop/Core/Sync/ISyncStatusService.cs desktop/src/Quater.Desktop/Core/Sync/SyncStatusService.cs desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs`
- `git commit -m "feat: add shared sync status service and register core foundations"`

---

## Track 2: Dashboard Replacement (Slice 1)

### Task 1D: Add Failing Tests for Dashboard Real Metrics

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Features/Dashboard/DashboardViewModelTests.cs`

**Step 1: Write failing tests for real metric loading**

Cover:
- total samples from API
- compliance rate from latest test results
- pending alerts from non-compliant/warning results
- sync indicator sourced from sync status service
- partial failure keeps available cards

**Step 2: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~DashboardViewModelTests"`
Expected: FAIL.

**Step 3: Commit test baseline**

Run:
- `git add desktop/Quater.Desktop.Tests/Features/Dashboard/DashboardViewModelTests.cs`
- `git commit -m "test: define dashboard metric and resilience behavior"`

### Task 2D: Implement Dashboard ViewModel Data Pipeline

**Files:**
- Modify: `desktop/src/Quater.Desktop/Features/Dashboard/DashboardViewModel.cs`

**Step 1: Inject required services via primary constructor**

Inject `IApiClientFactory`, `ISyncStatusService`, `IApiErrorFormatter`, `AppState`.

**Step 2: Replace static stats with computed API-backed values**

Use `ApiSamplesGetAsync` + `ApiTestResultsGetAsync` and sync service summary.

**Step 3: Add partial failure handling**

Keep available cards when one call fails; set warning card values where needed.

**Step 4: Run targeted dashboard tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~DashboardViewModelTests"`
Expected: PASS.

**Step 5: Commit viewmodel implementation**

Run:
- `git add desktop/src/Quater.Desktop/Features/Dashboard/DashboardViewModel.cs`
- `git commit -m "feat: replace static dashboard values with live mvp metrics"`

### Task 3D: Update Dashboard UI to Real Data Surfaces

**Files:**
- Modify: `desktop/src/Quater.Desktop/Features/Dashboard/DashboardView.axaml`

**Step 1: Bind cards and status footer to live properties**

Remove placeholder text and bind to VM properties.

**Step 2: Add visible warning section for sync/alert issues**

Render warning card/row only when present.

**Step 3: Run full desktop tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`
Expected: PASS.

**Step 4: Commit**

Run:
- `git add desktop/src/Quater.Desktop/Features/Dashboard/DashboardView.axaml`
- `git commit -m "feat: update dashboard ui to display live compliance and sync status"`

---

## Track 3: Compliance Audit Workflow (Slice 4)

### Task 1A: Add Failing Tests for Audit List VM Filtering and Export

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Features/Audit/AuditListViewModelTests.cs`

**Step 1: Write failing tests for filter -> DTO mapping**

Cover entity type, action, date range, user id, paging.

**Step 2: Write failing tests for export command**

Cover CSV generation from currently filtered rows.

**Step 3: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~AuditListViewModelTests"`
Expected: FAIL.

**Step 4: Commit tests**

Run:
- `git add desktop/Quater.Desktop.Tests/Features/Audit/AuditListViewModelTests.cs`
- `git commit -m "test: define audit workflow filter and export behavior"`

### Task 2A: Implement Audit Feature Module

**Files:**
- Create: `desktop/src/Quater.Desktop/Features/Audit/List/AuditListViewModel.cs`
- Create: `desktop/src/Quater.Desktop/Features/Audit/List/AuditListView.axaml`
- Create: `desktop/src/Quater.Desktop/Features/Audit/List/AuditListView.axaml.cs`
- Modify: `desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs`
- Modify: `desktop/src/Quater.Desktop/MainWindow.axaml`

**Step 1: Implement list load and filter command path**

Use `ApiAuditLogsFilterPostAsync` with `AuditLogFilterDto`.

**Step 2: Implement export command**

Use `ICsvExportService`; show success/error via dialog.

**Step 3: Register VM + data template**

Add DI and template wiring.

**Step 4: Run targeted audit tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~AuditListViewModelTests"`
Expected: PASS.

**Step 5: Commit feature module**

Run:
- `git add desktop/src/Quater.Desktop/Features/Audit/List/AuditListViewModel.cs desktop/src/Quater.Desktop/Features/Audit/List/AuditListView.axaml desktop/src/Quater.Desktop/Features/Audit/List/AuditListView.axaml.cs desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs desktop/src/Quater.Desktop/MainWindow.axaml`
- `git commit -m "feat: add compliance audit workflow with filtering and csv export"`

### Task 3A: Wire Shell Navigation and Permission Guard for Audit

**Files:**
- Modify: `desktop/src/Quater.Desktop/Core/Shell/ShellViewModel.cs`

**Step 1: Add audit navigation item visibility logic**

Use `IPermissionService` + selected lab guard.

**Step 2: Add route command and selection sync**

Ensure proper selection behavior with current `SukiSideMenu` pattern.

**Step 3: Run shell and audit tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~ShellViewModelTests|FullyQualifiedName~AuditListViewModelTests"`
Expected: PASS.

**Step 4: Commit navigation wiring**

Run:
- `git add desktop/src/Quater.Desktop/Core/Shell/ShellViewModel.cs`
- `git commit -m "feat: expose audit workflow in shell with permission guards"`

---

## Track 4: Sync/Conflict Center (Slice 5)

### Task 1S: Add Failing Tests for Sync Center VM

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Features/Sync/SyncCenterViewModelTests.cs`

**Step 1: Write failing tests for queue summary and retry commands**

Cover refresh, retry-all-failed, retry-single.

**Step 2: Write failing tests for conflict resolution command path**

Cover keep-local / keep-server / reload outcomes.

**Step 3: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SyncCenterViewModelTests"`
Expected: FAIL.

**Step 4: Commit tests**

Run:
- `git add desktop/Quater.Desktop.Tests/Features/Sync/SyncCenterViewModelTests.cs`
- `git commit -m "test: define sync center queue and conflict resolution behavior"`

### Task 2S: Implement Sync Center Feature Module

**Files:**
- Create: `desktop/src/Quater.Desktop/Features/Sync/Center/SyncCenterViewModel.cs`
- Create: `desktop/src/Quater.Desktop/Features/Sync/Center/SyncCenterView.axaml`
- Create: `desktop/src/Quater.Desktop/Features/Sync/Center/SyncCenterView.axaml.cs`
- Create: `desktop/src/Quater.Desktop/Core/Sync/IConflictResolutionService.cs`
- Create: `desktop/src/Quater.Desktop/Core/Sync/ConflictResolutionService.cs`
- Modify: `desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs`
- Modify: `desktop/src/Quater.Desktop/MainWindow.axaml`

**Step 1: Implement VM load and retry command flow**

Use sync services for queue state and retries.

**Step 2: Implement conflict resolution actions**

Define row model with conflict metadata and row-version fields.

**Step 3: Register and template feature**

Add DI and data template mapping.

**Step 4: Run targeted sync tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SyncCenterViewModelTests"`
Expected: PASS.

**Step 5: Commit sync feature**

Run:
- `git add desktop/src/Quater.Desktop/Features/Sync/Center/SyncCenterViewModel.cs desktop/src/Quater.Desktop/Features/Sync/Center/SyncCenterView.axaml desktop/src/Quater.Desktop/Features/Sync/Center/SyncCenterView.axaml.cs desktop/src/Quater.Desktop/Core/Sync/IConflictResolutionService.cs desktop/src/Quater.Desktop/Core/Sync/ConflictResolutionService.cs desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs desktop/src/Quater.Desktop/MainWindow.axaml`
- `git commit -m "feat: add sync center with retry and conflict resolution workflows"`

### Task 3S: Wire Shell Navigation and State Propagation

**Files:**
- Modify: `desktop/src/Quater.Desktop/Core/Shell/ShellViewModel.cs`
- Modify: `desktop/src/Quater.Desktop/Core/State/AppState.cs`

**Step 1: Add sync center navigation item and visibility**

Gate by authentication and permission policy.

**Step 2: Propagate sync state text and counts consistently**

Connect shell status properties to sync service/app state updates.

**Step 3: Run sync/shell tests and full test suite**

Run:
- `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SyncCenterViewModelTests|FullyQualifiedName~ShellViewModelTests"`
- `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`

Expected: PASS.

**Step 4: Commit**

Run:
- `git add desktop/src/Quater.Desktop/Core/Shell/ShellViewModel.cs desktop/src/Quater.Desktop/Core/State/AppState.cs`
- `git commit -m "feat: wire sync center navigation and shared sync status propagation"`

---

## Track 5: Production Hardening (Slice 6)

### Task 1H: Add Failing Tests for RBAC Visibility and Command Guards

**Files:**
- Modify: `desktop/Quater.Desktop.Tests/Core/Shell/ShellViewModelTests.cs`
- Modify: `desktop/Quater.Desktop.Tests/Features/Samples/SampleListViewModelTests.cs`
- Modify: `desktop/Quater.Desktop.Tests/Features/TestResults/TestResultListViewModelTests.cs`

**Step 1: Add failing shell tests for permission-gated nav items**

Cover audit/sync visibility by permission.

**Step 2: Add failing command tests for restricted actions**

Cover create/edit/delete command guards.

**Step 3: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~ShellViewModelTests|FullyQualifiedName~SampleListViewModelTests|FullyQualifiedName~TestResultListViewModelTests"`
Expected: FAIL.

**Step 4: Commit tests**

Run:
- `git add desktop/Quater.Desktop.Tests/Core/Shell/ShellViewModelTests.cs desktop/Quater.Desktop.Tests/Features/Samples/SampleListViewModelTests.cs desktop/Quater.Desktop.Tests/Features/TestResults/TestResultListViewModelTests.cs`
- `git commit -m "test: define rbac visibility and command guard behavior"`

### Task 2H: Apply Error and Permission Hardening in Features

**Files:**
- Modify: `desktop/src/Quater.Desktop/Core/Shell/ShellViewModel.cs`
- Modify: `desktop/src/Quater.Desktop/Features/Samples/List/SampleListViewModel.cs`
- Modify: `desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListViewModel.cs`

**Step 1: Replace direct exception message rendering**

Use `IApiErrorFormatter` for user-facing errors.

**Step 2: Apply permission checks for restricted commands**

Disable/guard create/edit/delete paths where disallowed.

**Step 3: Run targeted hardening tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~ShellViewModelTests|FullyQualifiedName~SampleListViewModelTests|FullyQualifiedName~TestResultListViewModelTests"`
Expected: PASS.

**Step 4: Commit hardening**

Run:
- `git add desktop/src/Quater.Desktop/Core/Shell/ShellViewModel.cs desktop/src/Quater.Desktop/Features/Samples/List/SampleListViewModel.cs desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListViewModel.cs`
- `git commit -m "feat: harden desktop workflows with permission and normalized error handling"`

### Task 3H: Add Diagnostics/Settings View

**Files:**
- Create: `desktop/src/Quater.Desktop/Features/Diagnostics/DiagnosticsViewModel.cs`
- Create: `desktop/src/Quater.Desktop/Features/Diagnostics/DiagnosticsView.axaml`
- Create: `desktop/src/Quater.Desktop/Features/Diagnostics/DiagnosticsView.axaml.cs`
- Modify: `desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs`
- Modify: `desktop/src/Quater.Desktop/MainWindow.axaml`

**Step 1: Add failing tests for diagnostics values**

Create: `desktop/Quater.Desktop.Tests/Features/Diagnostics/DiagnosticsViewModelTests.cs`

Cover backend URL display, auth state, sync status, and log file path.

**Step 2: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~DiagnosticsViewModelTests"`
Expected: FAIL.

**Step 3: Implement diagnostics feature and template wiring**

Bind to `AppState`, `AppSettings`, and logging location.

**Step 4: Run full desktop tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`
Expected: PASS.

**Step 5: Commit**

Run:
- `git add desktop/Quater.Desktop.Tests/Features/Diagnostics/DiagnosticsViewModelTests.cs desktop/src/Quater.Desktop/Features/Diagnostics/DiagnosticsViewModel.cs desktop/src/Quater.Desktop/Features/Diagnostics/DiagnosticsView.axaml desktop/src/Quater.Desktop/Features/Diagnostics/DiagnosticsView.axaml.cs desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs desktop/src/Quater.Desktop/MainWindow.axaml`
- `git commit -m "feat: add diagnostics settings view for production support visibility"`

---

## Merge and Verify

### Task M1: Merge Tracks in Dependency Order

**Step 1: Merge foundation into local main**

Run:
- `git checkout main`
- `git merge --no-ff feature/desktop-mvp-foundation`

**Step 2: Merge feature tracks**

Run in order:
- `git merge --no-ff feature/desktop-mvp-dashboard`
- `git merge --no-ff feature/desktop-mvp-audit`
- `git merge --no-ff feature/desktop-mvp-sync-center`
- `git merge --no-ff feature/desktop-mvp-hardening`

Expected: conflicts resolved (likely in `MainWindow.axaml`, `ServiceCollectionExtensions.cs`, `ShellViewModel.cs`).

### Task M2: Final Verification and Cleanup

**Step 1: Run full desktop tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`
Expected: PASS.

**Step 2: Optional smoke run**

Run: `dotnet run --project desktop/src/Quater.Desktop/Quater.Desktop.csproj`
Expected: app launches; dashboard/audit/sync/diagnostics visible per permissions.

**Step 3: Announce completion branch workflow**

Announce: `I'm using the finishing-a-development-branch skill to complete this work.`

**Step 4: Execute finishing skill steps**

Follow superpowers `finishing-a-development-branch` options to finalize integration.

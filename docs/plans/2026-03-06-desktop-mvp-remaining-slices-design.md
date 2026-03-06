# Desktop MVP Remaining Slices Design

**Date:** 2026-03-06
**Scope:** Desktop slices 1, 4, 5, and 6

## Goal
Deliver the remaining desktop MVP capabilities with production-usable compliance, sync visibility, and role-aware UX, while preserving the existing offline-first behavior and lab-context enforcement.

## Selected Approach
Use a foundation-first approach, then execute feature tracks in parallel worktrees.

1. Build shared infrastructure used by multiple slices:
   - API error normalization
   - permission checks for UI actions
   - CSV export utility
   - sync queue/conflict state service
2. Implement the remaining slices in parallel:
   - Slice 1: dashboard replacement with real KPIs, alerts, and sync status
   - Slice 4: compliance audit workflow with advanced filters and export
   - Slice 5: sync/conflict center with retries and RowVersion conflict resolution actions
   - Slice 6: production hardening (RBAC UX, diagnostics, settings visibility)
3. Merge tracks into local main with conflict resolution and full desktop test verification.

## Architecture

### Core principles
- Desktop remains offline-first: views are populated from local/known state immediately and then refreshed from API where available.
- Lab-context-first remains mandatory: workflows that read protected resources continue to depend on selected lab context and existing header injection.
- Permission-aware UX is enforced in shell and feature view models (hide/disable actions, do not rely on UI only).
- Non-destructive failure behavior: API and sync failures never corrupt current UI state; errors are surfaced in normalized form.

### New shared services
- `ApiErrorFormatter` (core service): maps API exceptions and unknown exceptions into concise user-facing messages.
- `PermissionService` (core service): central permission checks consumed by shell and feature VMs.
- `CsvExportService` (core service): generates CSV from audit rows with deterministic column ordering.
- `SyncStatusService` + `ConflictResolutionService` (core service): aggregate pending sync count, failed ops, and conflict candidates with retry/resolve APIs.

## Component Design by Slice

### Slice 1: Dashboard replacement
- Replace static dashboard data with computed metrics from existing samples/test results APIs and app sync state.
- Metrics include at minimum: total samples, compliance rate, pending alerts, pending sync operations, last sync status, and recent high-risk results.
- Dashboard VM loads via async pipeline with cancellation support and partial failure tolerance (display available cards when one endpoint fails).

### Slice 4: Compliance audit workflow
- Add a dedicated audit feature module:
  - `AuditListViewModel` + `AuditListView`
  - filter model mapping to `AuditLogFilterDto`
  - paged list with entity/action/user/date filters
- Add export action for current filtered rows using CSV service.
- Add shell navigation item gated by selected lab and permission policy.

### Slice 5: Sync/conflict center
- Add dedicated sync center module:
  - queue summary (pending/failed/in-flight)
  - retry failed operations
  - conflict list grouped by entity
  - basic RowVersion resolution actions (keep local / keep server / reload)
- Integrate with app state so shell footer and dashboard share the same sync truth.

### Slice 6: Production hardening
- RBAC UX hardening:
  - shell navigation visibility from permission policy
  - command-level disable/guard for restricted actions
- Error hardening:
  - replace ad-hoc exception message rendering in features with normalized formatter output
- Diagnostics/settings:
  - expose backend endpoint, auth status, last sync, and log file path in a lightweight diagnostics/settings view
  - preserve existing backend URL authority normalization behavior (no extra normalization)

## Data Flow
1. Shell initializes and navigation selects dashboard.
2. Dashboard requests metrics from feature services and app state; cards update as each source resolves.
3. Audit and sync center consume dedicated services that wrap generated API clients.
4. Shared error formatter converts thrown exceptions into UI-safe messages.
5. Permission service drives shell item visibility and command guards.

## Error Handling
- API failures: show normalized errors via dialog service and keep previously loaded collection values.
- Validation failures: block submit/export commands with explicit action guidance.
- Sync conflicts: present conflict record with deterministic resolution action and refresh after resolve attempt.

## Testing Strategy
- Unit tests first for all new view model behaviors and service logic.
- Add focused tests for:
  - dashboard metric computation and partial failure behavior
  - audit filter mapping and export output shape
  - sync center retry/conflict command paths
  - permission-driven visibility and command guards
  - error normalization mappings
- Run targeted tests per feature, then full desktop test suite after each merged track.

## Delivery and Merge Strategy
- Create isolated worktrees from local `main` for each track.
- Keep changes mergeable with small commits per task.
- Merge foundations first, then feature tracks, then hardening track, with full test verification before and after each merge.

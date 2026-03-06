# Desktop MVP Production Design (Compliance-First)

**Date:** 2026-03-06
**Scope:** Desktop + minor backend blocker fixes only

## Objective
Ship a production-usable desktop MVP for a real lab organization today, with strong compliance and reliability workflows.

## Delivery Model
- Use a dedicated MVP workstream with isolated branches/worktrees.
- Build in small vertical slices (UI + local data + sync + API + tests).
- Use subagents per slice for discovery, implementation, and review.
- Keep backend changes narrow: only desktop blocker fixes.

## MVP Slices
1. Replace dashboard (do not reuse current design) with real KPI/alert/sync data.
2. Complete sample operations (list/detail/create/edit/delete + filters/search).
3. Complete test result operations (CRUD + compliance visibility).
4. Add compliance audit workflow (advanced filters + export).
5. Add sync/conflict center (queue, retries, RowVersion conflict resolution).
6. Apply production hardening (RBAC UX, normalized errors, diagnostics/settings).

## Architecture Decisions
- Offline-first: local SQLite is source for UI state; sync pipeline reconciles with backend.
- Lab-context-first: enforce active lab and always send `X-Lab-Id` for protected routes.
- Permission-aware UX: hide/disable restricted actions by role.
- Conflict UX: explicit resolution actions with traceable outcomes.

## Backend Change Guardrails
**Allowed**
- OpenAPI/header contract accuracy fixes.
- Endpoint payload mismatch fixes that block desktop workflows.
- Narrow query/filter support needed for compliance UX.

**Not Allowed**
- Major auth redesign.
- Broad domain/schema refactors not required for MVP unblock.

## Release Gates
- Onboarding/login/lab selection works end-to-end.
- Samples and test results are fully operational.
- Audit workflow supports practical compliance use (filters + export).
- Sync failures/conflicts are recoverable in-app.
- Role restrictions are visible and enforced.

## Execution Constraints
- Small features one at a time.
- Each slice must be mergeable/revertable independently.
- Subagent-driven execution and review for every slice.

# Desktop MVP Slices 2 and 3 Parallel Execution Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:writing-plans to create executable task plans from this design.

**Goal:** Deliver desktop MVP Slice 2 (Sample operations) and Slice 3 (Test result operations) in parallel, using isolated worktrees and subagent-driven execution.

**Architecture:** We split work into two isolated branches/worktrees to avoid cross-slice collisions and keep each slice independently mergeable/revertable. Each slice gets its own executable plan and verification loop. Shared concerns (lab context, API headers, error handling) follow existing app infrastructure (`AppState`, `ApiClientHooks`, generated API clients) and are only changed where required by the slice.

**Tech Stack:** Avalonia 11, CommunityToolkit.Mvvm, EF Core SQLite (desktop local DB), generated OpenAPI clients (`Quater.Desktop.Api`), xUnit.

---

## Scope

### Slice 2 (Samples)
- Complete sample list/detail/create/edit/delete UX.
- Add practical filters/search on sample list.
- Keep offline-first desktop behavior through local repository updates.

### Slice 3 (Test Results)
- Add test result feature entry points and list/detail/create/edit/delete UX.
- Surface compliance status clearly in list/detail views.
- Preserve lab context and API header requirements.

## Parallel Execution Model

- Worktree A: `feature/desktop-slice-2-samples`
- Worktree B: `feature/desktop-slice-3-test-results`
- Both based from current `main` tip.
- Both run the same baseline verification command before implementation.
- Each slice executes first 3 tasks, reports verification output, then waits for feedback.

## Integration and Risk Controls

- No implementation on `main`.
- Keep backend changes out unless a desktop-blocking contract mismatch is proven.
- If shared file edits conflict (for example navigation wiring), land first slice and rebase the second before merge.
- Maintain independent test verification per slice plus a final desktop test pass after merge.

## Verification Standard

- Per task: run targeted tests (or add tests then run).
- Per batch: run `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`.
- Report implemented changes + command output, then pause for human review.

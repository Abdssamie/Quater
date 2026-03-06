# Desktop MVP Slices 2 and 3 Parallel Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement desktop MVP Slice 2 (Samples operations) and Slice 3 (Test results operations) in parallel sessions with isolated worktrees and explicit verification checkpoints.

**Architecture:** Create two isolated worktrees from `main`, each with its own executable task track and commits. Slice 2 extends existing samples UI/repository and adds API-backed CRUD plus filtering/search. Slice 3 introduces a new test-results feature module (list/detail CRUD + compliance visibility), then wires navigation and API integration with existing `X-Lab-Id` header hooks.

**Tech Stack:** .NET 10, Avalonia 11, CommunityToolkit.Mvvm, EF Core SQLite, generated OpenAPI clients, xUnit.

---

## Global Preflight (Do Once Before Parallel Sessions)

### Task 0: Prepare Isolated Worktrees and Baseline

**Files:**
- Modify: `.gitignore` (only if `.worktrees/` is not ignored)

**Step 1: Verify project-local worktree directory and ignore status**

Run: `ls -d .worktrees && git check-ignore -v .worktrees`
Expected: `.worktrees` exists and is ignored by `.gitignore`.

**Step 2: Verify baseline test health on current tip**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`
Expected: PASS (currently 46 passed).

**Step 3: Create two isolated worktrees**

Run:
- `git worktree add .worktrees/desktop-slice-2 -b feature/desktop-slice-2-samples`
- `git worktree add .worktrees/desktop-slice-3 -b feature/desktop-slice-3-test-results`

Expected: both worktrees created with new branches.

**Step 4: Verify each worktree independently**

Run in each worktree:
- `dotnet restore Quater.sln`
- `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`

Expected: restore succeeds and tests pass.

**Step 5: Commit only if `.gitignore` had to be fixed**

Run:
- `git add .gitignore`
- `git commit -m "chore: ignore local worktree directory"`

Expected: commit created only when required.

---

## Parallel Track A: Slice 2 - Samples Operations

### Task 1A: Add Failing Tests for Sample List ViewModel Filters/Search/Delete

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Features/Samples/SampleListViewModelTests.cs`
- Modify: `desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj` (only if folder include changes are needed)

**Step 1: Write failing tests for search + filter forwarding**

Add tests that assert `SampleListViewModel` passes all filter inputs (status/date/search/lab) into repository query abstraction.

**Step 2: Write failing tests for delete behavior**

Add tests for:
- confirmation false -> no delete
- confirmation true + delete success -> item removed + total decremented
- delete failure -> collection unchanged

**Step 3: Run targeted tests and confirm failure**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SampleListViewModelTests"`
Expected: FAIL (missing repository/query support and/or behavior gaps).

**Step 4: Commit tests**

Run:
- `git add desktop/Quater.Desktop.Tests/Features/Samples/SampleListViewModelTests.cs`
- `git commit -m "test: define sample list viewmodel behavior for filters and delete"`

### Task 2A: Implement Sample Query Contract + Repository Support

**Files:**
- Modify: `desktop/src/Quater.Desktop.Data/Repositories/ISampleRepository.cs`
- Modify: `desktop/src/Quater.Desktop.Data/Repositories/SampleRepository.cs`
- Create: `desktop/src/Quater.Desktop.Data/Repositories/SampleQuery.cs`
- Modify: `desktop/Quater.Desktop.Tests/Repositories/SampleRepositoryDeleteTests.cs` (if signatures need adaptation)

**Step 1: Add `SampleQuery` model**

Create `SampleQuery` with fields:
- `SampleStatus? Status`
- `DateTime? StartDate`
- `DateTime? EndDate`
- `string SearchText` (default `string.Empty`)
- `Guid? LabId`

**Step 2: Update repository interface to accept query object**

Add method:
`Task<IReadOnlyList<Sample>> GetFilteredAsync(SampleQuery query, CancellationToken ct = default);`

Retain old overload only if needed for compatibility; prefer new call path in UI.

**Step 3: Implement query filtering in repository**

In `SampleRepository`:
- apply status/date filters
- apply search against `CollectorName`, `Location.Description`, and optional `Notes`
- apply lab filter when provided
- return ordered descending by `CollectionDate`

**Step 4: Run tests**

Run:
- `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SampleListViewModelTests|FullyQualifiedName~SampleRepositoryDeleteTests"`

Expected: tests progress toward PASS.

**Step 5: Commit repository/query changes**

Run:
- `git add desktop/src/Quater.Desktop.Data/Repositories/ISampleRepository.cs desktop/src/Quater.Desktop.Data/Repositories/SampleRepository.cs desktop/src/Quater.Desktop.Data/Repositories/SampleQuery.cs desktop/Quater.Desktop.Tests/Repositories/SampleRepositoryDeleteTests.cs`
- `git commit -m "feat: add structured sample query filtering in desktop repository"`

### Task 3A: Wire SampleListViewModel to New Query Path and Load by Active Lab

**Files:**
- Modify: `desktop/src/Quater.Desktop/Features/Samples/List/SampleListViewModel.cs`

**Step 1: Build query object in `LoadSamplesCoreAsync`**

Compose `SampleQuery` from `StatusFilter`, `StartDateFilter`, `EndDateFilter`, `SearchText`, and `AppState.CurrentLabId` (nullable when empty).

**Step 2: Use repository query overload**

Replace positional call with `GetFilteredAsync(query, ct)`.

**Step 3: Keep existing UX behavior**

Maintain current loading/error handling, total count update, clear/apply commands.

**Step 4: Run targeted tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SampleListViewModelTests"`
Expected: PASS.

**Step 5: Commit ViewModel changes**

Run:
- `git add desktop/src/Quater.Desktop/Features/Samples/List/SampleListViewModel.cs`
- `git commit -m "feat: apply lab-aware query filtering in sample list viewmodel"`

### Task 4A: Implement Sample Create/Edit Dialog Flow and API-backed CRUD

**Files:**
- Create: `desktop/src/Quater.Desktop/Features/Samples/Edit/SampleEditorViewModel.cs`
- Create: `desktop/src/Quater.Desktop/Features/Samples/Edit/SampleEditorView.axaml`
- Create: `desktop/src/Quater.Desktop/Features/Samples/Edit/SampleEditorView.axaml.cs`
- Modify: `desktop/src/Quater.Desktop/Features/Samples/List/SampleListViewModel.cs`
- Modify: `desktop/src/Quater.Desktop/Features/Samples/List/SampleListView.axaml`
- Modify: `desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs`

**Step 1: Add failing tests for create/edit command flow**

Extend `SampleListViewModelTests` for:
- `CreateSampleCommand` opens editor workflow and refreshes list on save
- `EditSampleCommand` loads selected sample and updates on save

**Step 2: Implement editor ViewModel and view**

Include editable fields aligned to `CreateSampleDto`/`UpdateSampleDto`:
- type, lat/lon/description/hierarchy
- collection date, collector name, notes
- status (edit only)

**Step 3: Implement API calls in list VM**

Use generated `ISamplesApi` with `IApiClientFactory`:
- create: `ApiSamplesPostAsync`
- update: `ApiSamplesIdPutAsync`
- delete remains `DeleteAsync` locally for now unless API delete can be safely integrated without offline regression

If API fails, surface normalized error via dialog and keep list consistent.

**Step 4: Run tests and smoke verification**

Run:
- `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SampleListViewModelTests"`
- `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`

Expected: PASS.

**Step 5: Commit feature slice**

Run:
- `git add desktop/src/Quater.Desktop/Features/Samples/Edit/SampleEditorViewModel.cs desktop/src/Quater.Desktop/Features/Samples/Edit/SampleEditorView.axaml desktop/src/Quater.Desktop/Features/Samples/Edit/SampleEditorView.axaml.cs desktop/src/Quater.Desktop/Features/Samples/List/SampleListViewModel.cs desktop/src/Quater.Desktop/Features/Samples/List/SampleListView.axaml desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs desktop/Quater.Desktop.Tests/Features/Samples/SampleListViewModelTests.cs`
- `git commit -m "feat: complete desktop sample create and edit workflows"`

---

## Parallel Track B: Slice 3 - Test Results Operations

### Task 1B: Add Failing Tests for Test Results ViewModel CRUD + Compliance Visibility

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Features/TestResults/TestResultListViewModelTests.cs`

**Step 1: Write failing tests for list loading**

Tests cover loading from API and mapping compliance/status fields into UI models.

**Step 2: Write failing tests for create/edit/delete command paths**

Tests assert correct API method usage and list refresh/update behavior.

**Step 3: Run targeted tests and confirm fail**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~TestResultListViewModelTests"`
Expected: FAIL (feature not implemented).

**Step 4: Commit tests**

Run:
- `git add desktop/Quater.Desktop.Tests/Features/TestResults/TestResultListViewModelTests.cs`
- `git commit -m "test: define desktop test result list CRUD behavior"`

### Task 2B: Create Test Results Feature Module (ViewModel + View)

**Files:**
- Create: `desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListViewModel.cs`
- Create: `desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListView.axaml`
- Create: `desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListView.axaml.cs`
- Modify: `desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs`
- Modify: `desktop/src/Quater.Desktop/MainWindow.axaml`

**Step 1: Implement list VM skeleton with commands**

Commands:
- load/refresh
- create/edit/delete
- optional sample filter (`Guid? SelectedSampleId`) to support by-sample operations

**Step 2: Implement API integration**

Use `ITestResultsApi` methods:
- list: `ApiTestResultsGetAsync` and/or `ApiTestResultsBySampleSampleIdGetAsync`
- create: `ApiTestResultsPostAsync`
- update: `ApiTestResultsIdPutAsync`
- delete: `ApiTestResultsIdDeleteAsync`

**Step 3: Add DataGrid with compliance visibility**

Columns include parameter, value, unit, test date, technician, method, and a clearly visible compliance badge/text.

**Step 4: Register feature and data template**

Add VM registration in DI and add `DataTemplate` mapping in `MainWindow.axaml`.

**Step 5: Run targeted tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~TestResultListViewModelTests"`
Expected: PASS or narrowed failures.

**Step 6: Commit feature scaffolding**

Run:
- `git add desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListViewModel.cs desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListView.axaml desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListView.axaml.cs desktop/src/Quater.Desktop/Core/ServiceCollectionExtensions.cs desktop/src/Quater.Desktop/MainWindow.axaml`
- `git commit -m "feat: add desktop test results list feature with compliance indicators"`

### Task 3B: Add Test Result Create/Edit Dialog and Command Wiring

**Files:**
- Create: `desktop/src/Quater.Desktop/Features/TestResults/Edit/TestResultEditorViewModel.cs`
- Create: `desktop/src/Quater.Desktop/Features/TestResults/Edit/TestResultEditorView.axaml`
- Create: `desktop/src/Quater.Desktop/Features/TestResults/Edit/TestResultEditorView.axaml.cs`
- Modify: `desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListViewModel.cs`
- Modify: `desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListView.axaml`

**Step 1: Add/extend failing tests for editor flow**

Cover:
- create dialog submit -> post API -> list refresh
- edit dialog submit -> put API -> row update

**Step 2: Implement editor VM/view**

Fields aligned with DTOs:
- sample id
- parameter name/value/unit
- test date
- technician name
- test method

**Step 3: Integrate editor with list commands**

Wire create/edit commands to editor lifecycle and refresh/update behavior.

**Step 4: Run targeted tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~TestResultListViewModelTests"`
Expected: PASS.

**Step 5: Commit editor workflow**

Run:
- `git add desktop/src/Quater.Desktop/Features/TestResults/Edit/TestResultEditorViewModel.cs desktop/src/Quater.Desktop/Features/TestResults/Edit/TestResultEditorView.axaml desktop/src/Quater.Desktop/Features/TestResults/Edit/TestResultEditorView.axaml.cs desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListViewModel.cs desktop/src/Quater.Desktop/Features/TestResults/List/TestResultListView.axaml desktop/Quater.Desktop.Tests/Features/TestResults/TestResultListViewModelTests.cs`
- `git commit -m "feat: add desktop test result create and edit workflows"`

### Task 4B: Wire Shell Navigation to Test Results and Lab Context Guard

**Files:**
- Modify: `desktop/src/Quater.Desktop/Core/Shell/ShellViewModel.cs`
- Modify: `desktop/src/Quater.Desktop/MainWindow.axaml`

**Step 1: Add Test Results side-menu item**

Mirror Samples visibility rule (`HasSelectedLab`) and point content to current view.

**Step 2: Add navigation path**

Ensure selecting Test Results invokes `NavigateTo` with `TestResultListViewModel` route.

**Step 3: Verify authenticated/no-lab behavior**

Confirm item hidden without selected lab.

**Step 4: Run full desktop tests**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj`
Expected: PASS.

**Step 5: Commit navigation integration**

Run:
- `git add desktop/src/Quater.Desktop/Core/Shell/ShellViewModel.cs desktop/src/Quater.Desktop/MainWindow.axaml`
- `git commit -m "feat: expose test results workflow in desktop shell navigation"`

---

## Batch Execution Instructions (for executing-plans)

- Execute first 3 tasks in each track as one batch in each parallel session.
- After each batch, report:
  - implemented items
  - verification commands and output
  - `Ready for feedback.`
- Stop immediately on blockers or repeated verification failures.

## Merge and Finish

After both tracks complete and are green:
- Rebase later-finished branch on latest `main`.
- Resolve conflicts (expected likely in `MainWindow.axaml` and DI registration files).
- Re-run full desktop tests.
- Announce: `I'm using the finishing-a-development-branch skill to complete this work.`
- Invoke `superpowers:finishing-a-development-branch` and follow its integration options.

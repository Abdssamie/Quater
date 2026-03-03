# Test Coverage for Audit Gap Behaviors Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:writing-plans to implement the plan.

**Goal:** Add targeted tests that prove the five audit-gap behaviors (async auth timing, unauthorized gating, cancellation propagation, soft-delete filters, token key cleanup) and prevent regressions.

**Architecture:** Use targeted integration tests for EF Core query filters (SQLite in-memory) and focused unit tests for async/auth and token storage behavior. Avoid brittle reflection or UI automation by leaning on public APIs and deterministic assertions.

**Tech Stack:** .NET 10, xUnit, Moq, EF Core SQLite (in-memory), Avalonia Dispatcher test helpers.

---

## Scope

We will add failing tests that express the desired behavior (red first, then green after fixes if needed):

1. **AuthSessionManager.InitializeAsync** should not return before `AppState` reflects authenticated state.
2. **AuthSessionManager.HandleUnauthorizedAsync** should not allow a second call to proceed before logout mutations apply.
3. **ApiClient.InterceptRequest** should not surface `OperationCanceledException` to the request pipeline when token retrieval is canceled.
4. **Global query filters** should exclude soft-deleted `Lab`, `Parameter`, and `TestResult` rows by default and allow `IgnoreQueryFilters` to retrieve them.
5. **SecureFileTokenStore.ClearAsync** should remove both token file and key file.

## Test Locations

- `desktop/Quater.Desktop.Tests/Auth/AuthSessionManagerTests.cs`
- `desktop/Quater.Desktop.Tests/Api/ApiClientHooksTests.cs`
- `desktop/Quater.Desktop.Tests/Auth/SecureFileTokenStoreTests.cs`
- `desktop/Quater.Desktop.Tests/Repositories/SoftDeleteQueryFilterTests.cs` (new)

## Design Constraints

- No brittle reflection for internal/private methods.
- No UI automation or real network calls.
- Deterministic assertions only (no timing-based races).
- SQLite in-memory for EF Core filter coverage, seeded via raw SQL when required.

## Expected Outcomes

- Tests fail for the five gaps in the current code.
- Tests define desired behavior clearly and can be used to drive fixes.
- No flakiness introduced.

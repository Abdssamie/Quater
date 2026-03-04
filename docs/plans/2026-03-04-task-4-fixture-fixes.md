# Task 4 Fixture Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Restore Task 2/3 state and complete Task 4 fixture fixes with correct tests and commits.

**Architecture:** Remove the TestResult-Parameter relationship mapping and ensure services resolve parameters via Measurement.ParameterId with query filters ignored. Adjust test fixtures to align with removed navigation and resolve MockLabContextAccessor ambiguity. Verify with filtered tests and split commits.

**Tech Stack:** .NET 10, EF Core, xUnit

---

### Task 1: Restore Task 2 configuration state

**Files:**
- Modify: `backend/src/Quater.Backend.Data/Configurations/TestResultConfiguration.cs`

**Step 1: Write the failing test**

N/A (configuration change only; covered by integration test run later)

**Step 2: Run test to verify it fails**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: FAIL due to current out-of-scope configuration and service behavior

**Step 3: Write minimal implementation**

Remove the relationship mapping block that uses `HasOne<Parameter>()` so no relationship mapping remains.

**Step 4: Run test to verify it passes**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: Still may fail until Task 2/3 service changes and fixtures are complete

**Step 5: Commit**

Defer commit until Task 2/3 service changes are done (combined commit).

---

### Task 2: Restore Task 3 service state

**Files:**
- Modify: `backend/src/Quater.Backend.Services/TestResultService.cs`

**Step 1: Write the failing test**

N/A (service behavior validated by existing integration tests)

**Step 2: Run test to verify it fails**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: FAIL due to parameter lookup and filters

**Step 3: Write minimal implementation**

Update `GetByIdAsync` and `GetBySampleIdAsync` to resolve parameters by `Measurement.ParameterId` and apply `IgnoreQueryFilters()` on the Parameters query.

**Step 4: Run test to verify it passes**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: PASS once fixture fixes are complete

**Step 5: Commit**

```bash
git add backend/src/Quater.Backend.Data/Configurations/TestResultConfiguration.cs backend/src/Quater.Backend.Services/TestResultService.cs
git commit -m "fix(data): remove TestResult-Parameter mapping; restore parameter lookups"
```

---

### Task 3: Fix test fixtures and ambiguity

**Files:**
- Modify: `backend/tests/Quater.Backend.Core.Tests/Helpers/MockDataFactory.cs`
- Modify: `backend/tests/Quater.Backend.Core.Tests/Services/SampleServiceIntegrationTests.cs`
- Modify: `backend/tests/Quater.Backend.Core.Tests/Services/AuditLogServiceIntegrationTests.cs`

**Step 1: Write the failing test**

N/A (fixture changes validated by integration tests)

**Step 2: Run test to verify it fails**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: FAIL if fixtures or ambiguity still present

**Step 3: Write minimal implementation**

- Ensure no `TestResult.Parameter` assignment in `MockDataFactory`.
- Resolve `MockLabContextAccessor` ambiguity using explicit namespace qualification or using alias in the two test files.

**Step 4: Run test to verify it passes**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add backend/tests/Quater.Backend.Core.Tests/Helpers/MockDataFactory.cs backend/tests/Quater.Backend.Core.Tests/Services/SampleServiceIntegrationTests.cs backend/tests/Quater.Backend.Core.Tests/Services/AuditLogServiceIntegrationTests.cs
git commit -m "test(core): fix fixtures for removed TestResult.Parameter navigation"
```

---

### Task 4: Verification

**Files:**
- Test: `backend/tests/Quater.Backend.Core.Tests/`

**Step 1: Run test**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: PASS

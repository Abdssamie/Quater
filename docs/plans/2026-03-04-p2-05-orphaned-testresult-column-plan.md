# P2-05 Orphaned TestResult Column Removal Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove the orphaned `TestResult_Measurement_ParameterId` column and its FK/index via EF migrations by eliminating the shadow FK mapping from the model, while preserving TestResult service behavior.

**Architecture:** The fix removes the `TestResult.Parameter` navigation and its FK mapping so EF no longer creates the shadow `Measurement_ParameterId` property. Services will resolve parameter names through `Measurement.ParameterId` lookups instead of navigation properties. A new EF migration will drop the orphaned column/FK/index.

**Tech Stack:** C# 14, EF Core 10, ASP.NET Core 10, PostgreSQL, xUnit

---

### Task 1: Remove TestResult.Parameter navigation from the model

**Files:**
- Modify: `shared/Models/TestResult.cs`

**Step 1: Write a failing test**

No new test required; existing service tests should continue to cover parameter lookups.

**Step 2: Remove the navigation property**

In `TestResult.cs`, remove the `Parameter? Parameter` navigation:

```csharp
// Navigation properties
public Sample Sample { get; init; } = null!;
```

**Step 3: Run relevant tests to ensure no compile errors**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: FAIL (until service updates remove the navigation usage).

**Step 4: Commit**

```bash
git add shared/Models/TestResult.cs
git commit -m "refactor(models): remove TestResult.Parameter navigation to eliminate shadow FK"
```

---

### Task 2: Remove FK mapping to Parameter in TestResultConfiguration

**Files:**
- Modify: `backend/src/Quater.Backend.Data/Configurations/TestResultConfiguration.cs`

**Step 1: Remove the relationship mapping**

Delete the relationship block:

```csharp
// Configure relationship to Parameter via Measurement.ParameterId
entity.HasOne(e => e.Parameter)
    .WithMany()
    .HasForeignKey("Measurement_ParameterId")
    .OnDelete(DeleteBehavior.Restrict);
```

**Step 2: Run relevant tests**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: FAIL (until service updates remove the navigation usage).

**Step 3: Commit**

```bash
git add backend/src/Quater.Backend.Data/Configurations/TestResultConfiguration.cs
git commit -m "refactor(data): remove TestResult-Parameter relationship mapping"
```

---

### Task 3: Update TestResultService to use Measurement.ParameterId lookups

**Files:**
- Modify: `backend/src/Quater.Backend.Services/TestResultService.cs`

**Step 1: Update GetByIdAsync**

Replace the `Include(tr => tr.Parameter)` and navigation usage with a parameter lookup using `Measurement.ParameterId`:

```csharp
var testResult = await context.TestResults
    .AsNoTracking()
    .IgnoreQueryFilters()
    .Where(tr => tr.Id == id && !tr.IsDeleted)
    .FirstOrDefaultAsync(ct);

if (testResult == null)
    throw new NotFoundException(ErrorMessages.TestResultNotFound);

var parameterName = await context.Parameters
    .AsNoTracking()
    .Where(p => p.Id == testResult.Measurement.ParameterId)
    .Select(p => p.Name)
    .FirstOrDefaultAsync(ct);

if (parameterName == null)
    throw new InvalidOperationException(
        $"Data integrity error: TestResult {id} references non-existent Parameter {testResult.Measurement.ParameterId}");

return testResult.ToDto(parameterName);
```

**Step 2: Update GetBySampleIdAsync**

Remove `Include(tr => tr.Parameter)` and build parameter name dictionary from a lookup:

```csharp
var items = await query
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(ct);

var parameterIds = items.Select(tr => tr.Measurement.ParameterId).Distinct().ToList();
var parameterDict = await context.Parameters
    .AsNoTracking()
    .Where(p => parameterIds.Contains(p.Id))
    .ToDictionaryAsync(p => p.Id, p => p.Name, ct);
```

**Step 3: Run service tests**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: PASS.

**Step 4: Commit**

```bash
git add backend/src/Quater.Backend.Services/TestResultService.cs
git commit -m "fix(services): resolve TestResult parameter names via Measurement.ParameterId"
```

---

### Task 4: Update TestResultServiceIntegrationTests if needed

**Files:**
- Modify: `backend/tests/Quater.Backend.Core.Tests/Services/TestResultServiceIntegrationTests.cs`

**Step 1: Adjust any assertions relying on the navigation property**

Use `Measurement.ParameterId` lookups if the tests reference `Parameter` navigation.

**Step 2: Run tests**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: PASS.

**Step 3: Commit**

```bash
git add backend/tests/Quater.Backend.Core.Tests/Services/TestResultServiceIntegrationTests.cs
git commit -m "test(core): update TestResultService integration tests for parameter lookup"
```

---

### Task 5: Generate migration to drop orphaned column/FK/index

**Files:**
- Create: `backend/src/Quater.Backend.Data/Migrations/*DropOrphanedTestResultMeasurementParameterIdColumn*.cs`
- Modify: `backend/src/Quater.Backend.Data/Migrations/QuaterDbContextModelSnapshot.cs`

**Step 1: Generate migration**

Run:

```bash
dotnet ef migrations add DropOrphanedTestResultMeasurementParameterIdColumn \
  --project backend/src/Quater.Backend.Data \
  --startup-project backend/src/Quater.Backend.Api
```

**Step 2: Verify migration contents**

Confirm the migration drops:
- Column: `TestResult_Measurement_ParameterId`
- FK: `FK_TestResults_Parameters_TestResult_Measurement_ParameterId`
- Index: `IX_TestResults_TestResult_Measurement_ParameterId`

**Step 3: Run tests**

Run: `dotnet test backend/tests/Quater.Backend.Core.Tests/ --filter "TestResultServiceIntegrationTests"`
Expected: PASS.

**Step 4: Commit**

```bash
git add backend/src/Quater.Backend.Data/Migrations \
  backend/src/Quater.Backend.Data/Migrations/QuaterDbContextModelSnapshot.cs
git commit -m "fix(data): drop orphaned TestResult_Measurement_ParameterId column (P2-05)"
```

---

### Task 6: Full verification

**Step 1: Run full backend tests**

Run: `dotnet test backend/ -q`
Expected: PASS (all tests).

**Step 2: Commit any remaining changes**

If all tasks are complete and no pending files remain, ensure the branch is clean.

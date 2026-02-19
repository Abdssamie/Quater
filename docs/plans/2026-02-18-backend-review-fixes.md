# Backend Review Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix 4 important issues and 7 minor issues identified in backend review before desktop development starts.

**Architecture:** Targeted fixes across value objects, service layer, controllers, and data layer. No breaking changes to API contract.

**Tech Stack:** C# 13, .NET 10, EF Core, xUnit, FluentAssertions

---

## Task 1: Fix Longitude Validation Bug (I1)

**Files:**
- Modify: `shared/ValueObjects/Location.cs:42`
- Test: `shared/Tests/ValueObjects/LocationTests.cs` (if exists, else create)

**Step 1: Write failing test for 180° longitude**

```csharp
[Fact]
public void Constructor_Longitude180_ShouldNotThrow()
{
    // Arrange & Act
    var act = () => new Location(0, 180);
    
    // Assert
    act.Should().NotThrow();
}

[Fact]
public void Constructor_LongitudeGreaterThan180_ShouldThrow()
{
    // Arrange & Act
    var act = () => new Location(0, 180.1);
    
    // Assert
    act.Should().Throw<ArgumentOutOfRangeException>()
        .WithMessage("*longitude*");
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~Location"`
Expected: FAIL - 180° currently throws

**Step 3: Fix the validation logic**

In `shared/ValueObjects/Location.cs:42`, change:
```csharp
// Before:
if (longitude is < -180 or 180)

// After:
if (longitude is < -180 or > 180)
```

**Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~Location"`
Expected: PASS

**Step 5: Commit**

```bash
git add shared/ValueObjects/Location.cs shared/Tests/ValueObjects/LocationTests.cs
git commit -m "fix: correct longitude validation to accept 180°"
```

---

## Task 2: Move ICurrentUserService to Core Layer (I2)

**Files:**
- Create: `backend/src/Quater.Backend.Core/Interfaces/ICurrentUserService.cs`
- Modify: `backend/src/Quater.Backend.Data/Interceptors/AuditTrailInterceptor.cs:371`
- Modify: `backend/src/Quater.Backend.Services/CurrentUserService.cs` (update using statement)

**Step 1: Create interface in Core layer**

Create `backend/src/Quater.Backend.Core/Interfaces/ICurrentUserService.cs`:
```csharp
namespace Quater.Backend.Core.Interfaces;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
}
```

**Step 2: Remove interface from AuditTrailInterceptor**

In `backend/src/Quater.Backend.Data/Interceptors/AuditTrailInterceptor.cs:371`, delete the interface definition and add using:
```csharp
using Quater.Backend.Core.Interfaces;
```

**Step 3: Update CurrentUserService using statement**

In `backend/src/Quater.Backend.Services/CurrentUserService.cs`, update:
```csharp
using Quater.Backend.Core.Interfaces;
```

**Step 4: Build to verify no compilation errors**

Run: `dotnet build`
Expected: Success

**Step 5: Run tests to verify nothing broke**

Run: `dotnet test`
Expected: 264/264 passing

**Step 6: Commit**

```bash
git add backend/src/Quater.Backend.Core/Interfaces/ICurrentUserService.cs backend/src/Quater.Backend.Data/Interceptors/AuditTrailInterceptor.cs backend/src/Quater.Backend.Services/CurrentUserService.cs
git commit -m "refactor: move ICurrentUserService to Core layer"
```

---

## Task 3: Recalculate Compliance on Update (I3)

**Files:**
- Modify: `backend/src/Quater.Backend.Services/TestResultService.cs` (UpdateAsync method)
- Modify: `backend/src/Quater.Backend.Core/DTOs/TestResultDto.cs` (remove ComplianceStatus from UpdateTestResultDto)

**Step 1: Write test for compliance recalculation on update**

In `backend/tests/Quater.Backend.Core.Tests/Services/TestResultServiceTests.cs`:
```csharp
[Fact]
public async Task UpdateAsync_RecalculatesComplianceStatus()
{
    // Arrange
    var parameter = await CreateParameterAsync("pH", "pH", 0, 14);
    var sample = await CreateSampleAsync();
    var testResult = await _service.CreateAsync(new CreateTestResultDto
    {
        SampleId = sample.Id,
        ParameterName = "pH",
        Value = 8.5,
        Unit = "pH",
        TestMethod = TestMethod.Laboratory,
        TestedAt = DateTime.UtcNow
    }, _userId);
    
    // Act - update with value that should change compliance
    var updated = await _service.UpdateAsync(testResult.Id, new UpdateTestResultDto
    {
        SampleId = sample.Id,
        ParameterName = "pH",
        Value = 9.5, // Should trigger different compliance
        Unit = "pH",
        TestMethod = TestMethod.Laboratory,
        TestedAt = DateTime.UtcNow
    }, _userId);
    
    // Assert - compliance should be recalculated, not from DTO
    updated.ComplianceStatus.Should().NotBe(testResult.ComplianceStatus);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter "UpdateAsync_RecalculatesComplianceStatus"`
Expected: FAIL or test doesn't exist yet

**Step 3: Remove ComplianceStatus from UpdateTestResultDto**

In `backend/src/Quater.Backend.Core/DTOs/TestResultDto.cs`:
```csharp
public record UpdateTestResultDto
{
    [Required]
    public Guid SampleId { get; init; }
    
    [Required]
    [MaxLength(100)]
    public string ParameterName { get; init; } = string.Empty;
    
    [Required]
    public double Value { get; init; }
    
    [Required]
    [MaxLength(50)]
    public string Unit { get; init; } = string.Empty;
    
    [Required]
    public TestMethod TestMethod { get; init; }
    
    [Required]
    public DateTime TestedAt { get; init; }
    
    [MaxLength(500)]
    public string? Notes { get; init; }
    
    // REMOVED: public ComplianceStatus ComplianceStatus { get; init; }
}
```

**Step 4: Update TestResultService.UpdateAsync to recalculate**

In `backend/src/Quater.Backend.Services/TestResultService.cs`, modify UpdateAsync:
```csharp
public async Task<TestResultDto> UpdateAsync(Guid id, UpdateTestResultDto dto, Guid userId, CancellationToken ct = default)
{
    var existing = await context.TestResults
        .Include(tr => tr.Sample)
        .FirstOrDefaultAsync(tr => tr.Id == id && !tr.IsDeleted, ct)
        ?? throw new NotFoundException(ErrorMessages.TestResultNotFound);

    var parameter = await context.Parameters
        .FirstOrDefaultAsync(p => p.Name == dto.ParameterName && !p.IsDeleted, ct)
        ?? throw new NotFoundException(ErrorMessages.ParameterNotFound);

    existing.UpdateFromDto(dto, parameter, userId);
    
    // Recalculate compliance status
    var complianceStatus = await complianceCalculator.CalculateComplianceAsync(
   rameterName, 
        dto.Value, 
        ct);
    existing.ComplianceStatus = complianceStatus;

    try
    {
        await context.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException)
    {
        throw new ConflictException(ErrorMessages.ConcurrencyConflict);
    }

    return MapToDto(existing);
}
```

**Step 5: Run tests**

Run: `dotnet test`
Expected: All tests pass

**Step 6: Commit**

```bash
git add backend/src/Quater.Backend.Core/DTOs/TestResultDto.cs backend/src/Quater.Backend.Services/TestResultService.cs backend/tests/Quater.Backend.Core.Tests/TestResultServiceTests.cs
git commit -m "fix: recalculate compliance status on test result update"
```

---

## Task 4: Remove Dead Exception Handlers (I4)

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Controllers/LabsController.cs`
- Modify: `backend/src/Quater.Backend.Api/Controllers/ParametersController.cs`

**Step 1: Remove catch block from LabsController.Create**

In `backend/src/Quater.Backend.Api/Controllers/LabsController.cs`, find Create method and remove:
```csharp
// REMOVE THIS:
catch (InvalidOperationException ex)
{
    return BadRequest(new { message = ex.Message });
}
```

**Step 2: Remove catch block from ParametersController.Create**

In `backend/src/Quater.Backend.Api/Controllers/ParametersController.cs`, find Create method and remove:
```csharp
// REMOVE THIS:
catch (InvalidOperationException ex)
{
    return BadRequest(new { message = ex.Message });
}
```

**Step 3: Build to verify**

Run: `dotnet build`
Expected: Success

**Step 4: Run tests**

Run: `dotnet test`
Expected: 264/264 passing

**Step 5: Commit**

```bash
git add backend/src/Quater.Backend.Api/Controllers/LabsController.cs backend/src/Quater.Backend.Api/Controllers/ParametersController.cs
git commit -m "refactor: remove dead InvalidOpernException handlers"
```

---

## Task 5: Add Soft-Delete Filter to ParameterService.GetByIdAsync (M1)

**Files:**
- Modify: `backend/src/Quater.Backend.Services/ParameterService.cs`

**Step 1: Add !p.IsDeleted filter**

In `backend/src/Quater.Backend.Services/ParameterService.cs`, find GetByIdAsync and update:
```csharp
public async Task<ParameterDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
{
    var parameter = await context.Parameters
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    return parameter == null ? null : MapToDto(parameter);
}
```

**Step 2: Run tests**

Run: `dotnet testFullyQualifiedName~ParameterService"`
Expected: All pass

**Step 3: Commit**

```bash
git add backend/src/Quater.Backend.Services/ParameterService.cs
git commit -m "fix: add soft-delete filter to ParameterService.GetByIdAsync"
```

---

## Task 6: Remove Redundant RlsSessionInterceptor (M2)

**Files:**
- Delete: `backend/src/Quater.Backend.Data/Interceptors/RlsSessionInterceptor.cs` (if exists)
- Modify: `backend/src/Quater.Backend.Data/QuaterDbContext.cs` (remove interceptor registration if present)

**Step 1: Check if RlsSessionInterceptor exists**

Run: `find backend/src/Quater.Backend.Data -name "*RlsSession*"`

**Step 2: If exists, remove from DbContext configuration**

In `backend/src/Quater.Backend.Data/QuaterDbContext.cs` or `Program.cs`, remove interceptor registration.

**Step 3: Delete the file**

Run: `git rm backend/src/Quater.Backend.Data/Interceptors/RlsSessionInterceptor.cs`

**Step 4: Run tests**

Run: `dotnet test`
Expected: 264/264 passing

**Step 5: Commit**

```bash
git commit -m "refactor: remove redundant RlsSessionInterceptor"
```

---

## Task 7: Optimize ComplianceCalculator Batch Query (M3)

**Files:**
- Modify: `backend/src/Quater.Backend.Services/ComplianceCalculator.cs`

**Step 1: Refactor CalculateBatchComplianceAsync to use IN clause**

In `backend/src/Quater.Backend.Services/ComplianceCalculator.cs`:
```csharp
public async Task<Dictionary<string, ComplianceStatus>> CalculateBatchComplianceAsync(
    IEnumerable<(string ParameterName, double Value)> measurements,
    CancellationToken ct = default)
{
    var parameterNames = measurements.Select(m => m.ParameterName).Distinct().ToList();
    
    // Single query with IN clause
    var parameters = await context.Parameters
        .AsNoTracking()
        .Where(p => parameterNames.Contains(p.Name) && !p.IsDeleted)
        .ToDictionaryAsync(p => p.Name, ct);

    var results = new Dictionary<string, ComplianceStatus>();
    
    foreach (var (parameterName, value) in measurements)
    {
        if (!parameters.TryGetValue(parameterName, out var parameter))
        {
            results[parameterName] = ComplianceStatus.Pass;
            continue;
        }

        var status = CalculateCompliance(parameter, value);
        results[parameterName] = status;
    }

    return results;
}
```

**Step 2: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~ComplianceCalculator"`
Expected: All pass

**Step 3: Commit**

```bash
git add backend/src/Quater.Backend.Services/CianceCalculator.cs
git commit -m "perf: optimize batch compliance calculation with single query"
```

---

## Task 8: Remove Lock from Email Template Rendering (M4)

**Files:**
- Modify: `backend/src/Quater.Backend.Infrastructure.Email/ScribanEmailTemplateService.cs`

**Step 1: Refactor to use ConcurrentDictionary**

In `backend/src/Quater.Backend.Infrastructure.Email/ScribanEmailTemplateService.cs`:
```csharp
private readonly ConcurrentDictionary<string, Template> _templateCache = new();

public async Task<string> RenderAsync(string templateName, object model)
{
    var template = _templateCache.GetOrAdd(templateName, name =>
    {
        var templatePathPath.Combine(_templatesPath, $"{name}.html");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template not found: {name}");
        
        var templateContent = File.ReadAllText(templatePath);
        return Template.Parse(templateContent);
    });

    return await Task.Run(() => template.Render(model));
}
```

**Step 2: Update using statements**

Add: `using System.Collections.Concurrent;`

**Step 3: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~Email"`
Expected: All pass

**Step 4: Commit**

```bash
git add backend/src/Quater.Backend.Infrastructure.Email/ScribanEmailTemplateService.cs
git commit -m "perf: remove lock from email template rendering"
```

---

## Task 9: Remove Redundant Soft-Delete Filters (M5)

**Files:**
- Modify: `backend/src/Quater.Backend.Services/LabService.cs`

**Step 1: Remove explicit !IsDeleted filter from GetAllAsync**

In `backend/src/Quater.Backend.Services/LabService.cs`:
```csharp
public async Task<PagedResult<LabDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default var query = context.Labs.AsNoTracking();
    // Remove: .Where(l => !l.IsDeleted)
  var totalCount = await query.CountAsync(ct);
    var labs = await query
        .OrderBy(l => l.Name)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PagedResult<LabDto>(
        labs.Select(MapToDto).ToList(),
        totalCount,
        pageNumber,
        pageSize);
}
```

**Step 2: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~LabService"`
Expected: All pass

**Step 3: Commit**

```bash
git add backend/src/Quater.Backend.Services/LabService.cs
git commit -m "refactor: remove redundant soft-delete filter (global filter applies)"
```

---

## Task 10: Remove Redundant AssignedAt Assignment (M7)

**Files:**
- Modify: `shared/Models/UserLab.cs` OR `backend/src/Quater.Backend.Services/UserLabService.cs`

**Step 1: Choose one approach**

Option A: Remove default value from property
```csharp
// In shared/Models/UserLab.cs
public DateTime AssignedAt { get; set; } // Remove = DateTime.UtcNow
```

Option B: Remove explicit assignment in service
```cshaUserLabService.cs CreateAsync
var userLab = new UserLab
{
    UserId = userId,
    LabId = labId,
    Role = role
    // Remove: AssignedAt = DateTime.UtcNow
};
```

**Recommendation:** Keep service assignment, remove property default (more explicit).

**Step 2: Run tests**

Run: `dotnet test --filter "FullyQualifiedName~UserLab"`
Expected: All pass

**Step 3: Commit**

```bash
git add shared/Models/UserLab.cs
git commit -m "refactor: remove redundant AssignedAt default value"
```

---

## Verification

After all tasks complete:

```bash
# Run full test suite
dotnet test

# Expected: 264/264 passing

# Build entire solution
dotnet build

# Expected: Success, 0 warnings
```

---

## Notes

- **M6 (API integration tests)** is intentionally skipped - service layer has comprehensive coverage, API tests would be redundant for MVP
- **C1 (DPoP)** requires architectural decision from product owner - not included in this plan
- All fixes maintain backward compatibility with existing API contract
- No database migrations required

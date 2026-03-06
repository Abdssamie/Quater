# Phase 2 Correctness Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix all 17 Phase 2 correctness issues (P2-01 through P2-17) identified in the architecture review, covering data-integrity bugs in interceptors, orphaned schema columns, middleware correctness, architecture violations, and a god-class decomposition.

**Architecture:** Quater is a C# ASP.NET Core 10 / PostgreSQL water-quality lab management system. Backend layers: `Quater.Backend.Api` (controllers, middleware, authorization), `Quater.Backend.Services` (business logic), `Quater.Backend.Data` (EF Core interceptors, migrations, seeders), `Quater.Backend.Core` (DTOs, interfaces, validators, mapping extensions), `Quater.Backend.Infrastructure.Email`. All source is under `backend/src/`, tests under `backend/tests/`.

**Tech Stack:** C# 14, ASP.NET Core 10, EF Core 10, PostgreSQL, xUnit + TestContainers, FluentValidation, Scriban, OpenIddict, Serilog

**Code style rules (AGENTS.md):**
- Primary constructors for DI
- `string.Empty` (not `""`)
- `[]` for empty collection expressions
- `CancellationToken ct = default`
- `dotnet test backend/ -q` to run all tests

**Branch:** `fix/phase-2-important` (worktree at `.worktrees/phase-2-important`)

**Baseline:** 279 tests passing (199 Core.Tests + 80 Api.Tests) on `main`.

---

## Task 1: P2-01 + P2-02 + P2-03 — Interceptor correctness

**Issues addressed:**
- P2-01: `AuditTrailInterceptor` stores captured audit data in `AsyncLocal` on a singleton — data can be lost across async execution context boundaries
- P2-02: `SoftDeleteInterceptor.ApplySoftDelete` validates that `DeletedBy` exists but never sets it
- P2-03: Soft-deletes are logged as `AuditAction.Update` instead of a distinct action

**Files:**
- Modify: `backend/src/Quater.Backend.Data/Interceptors/AuditTrailInterceptor.cs`
- Modify: `backend/src/Quater.Backend.Data/Interceptors/SoftDeleteInterceptor.cs`
- Modify: `backend/src/Quater.Backend.Data/Extensions/ServiceCollectionExtensions.cs` (register SoftDeleteInterceptor with factory)
- Modify: `backend/src/Quater.Shared/Enums/AuditAction.cs` (add `SoftDelete` value)
- Modify: `backend/tests/Quater.Backend.Core.Tests/Data/AuditTrailInterceptorTests.cs`
- Modify: `backend/tests/Quater.Backend.Core.Tests/Data/SoftDeleteInterceptorTests.cs`

**Step 1: Write failing tests (run first to confirm failure)**

In `AuditTrailInterceptorTests.cs`, add tests:
```csharp
[Fact]
public async Task ConcurrentSaves_AuditLogsAreNotCrossContaminated()
{
    // Arrange: two separate DbContext instances (simulating two requests)
    // Act: save different entities on both concurrently
    // Assert: each save produces exactly its own audit log entries, not mixed
}

[Fact]
public async Task SoftDelete_IsLoggedAsSoftDeleteAction_NotUpdate()
{
    // Arrange: entity implementing ISoftDeletable
    // Act: delete the entity (interceptor converts to soft-delete)
    // Assert: AuditLog.Action == AuditAction.SoftDelete
}
```

In `SoftDeleteInterceptorTests.cs`, add test:
```csharp
[Fact]
public async Task SoftDelete_SetsDeletedByToCurrentUserId()
{
    // Act: soft-delete an entity
    // Assert: entity.DeletedBy == currentUserId.ToString()
}
```

Run: `dotnet test backend/ -q --filter "AuditTrailInterceptorTests|SoftDeleteInterceptorTests"`
Expected: new tests FAIL.

**Step 2: Fix P2-01 — Replace AsyncLocal with ConditionalWeakTable**

In `AuditTrailInterceptor.cs`, replace the two `AsyncLocal` instance fields:
```csharp
// REMOVE these:
private readonly AsyncLocal<bool> _isCapturing = new();
private readonly AsyncLocal<List<AuditLogData>> _auditDataCapture = new();

// ADD this:
private readonly ConditionalWeakTable<DbContext, List<AuditLogData>> _capturedData = new();
```

Update `SavingChanges` and `SavingChangesAsync` to:
- Check `_capturedData.TryGetValue(context, out _)` for the recursion guard
- Call `CaptureAuditData(context)` which stores results in `_capturedData.GetOrCreateValue(context)`

Update `SavedChangesAsync`/`SavedChanges` to:
- Call `_capturedData.TryGetValue(context, out var data)` to read
- Call `_capturedData.Remove(context)` after processing

Update `AddAuditLogsToContext` signature to `(DbContext context, List<AuditLogData> auditData)` — take the list as a parameter instead of reading instance state.

Add `using System.Runtime.CompilerServices;` at the top.

**Step 3: Fix P2-02 — SoftDeleteInterceptor sets DeletedBy**

`SoftDeleteInterceptor` is a singleton but `ICurrentUserService` is scoped. Use constructor injection of `IServiceProvider` and create a scope per invocation:

```csharp
public sealed class SoftDeleteInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    private static void ApplySoftDelete(DbContext context, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        var userId = currentUser.UserId?.ToString() ?? string.Empty;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted) continue;

            var isDeletedProp = entry.Entity.GetType().GetProperty("IsDeleted");
            if (isDeletedProp is null) continue;

            entry.State = EntityState.Modified;
            isDeletedProp.SetValue(entry.Entity, true);

            var deletedAtProp = entry.Entity.GetType().GetProperty("DeletedAt");
            deletedAtProp?.SetValue(entry.Entity, DateTime.UtcNow);

            var deletedByProp = entry.Entity.GetType().GetProperty("DeletedBy");
            if (!string.IsNullOrEmpty(userId))
                deletedByProp?.SetValue(entry.Entity, userId);
        }
    }
}
```

Update DI registration in `ServiceCollectionExtensions.cs` — change from singleton to factory pattern or ensure `IServiceProvider` injection works. Since `SaveChangesInterceptor` must be registered with AddDbContext, use:
```csharp
services.AddSingleton<SoftDeleteInterceptor>();
// and inject IServiceProvider via constructor
```

**Step 4: Fix P2-03 — SoftDelete logged as SoftDelete not Update**

Add `SoftDelete = 3` to `Quater.Shared.Enums/AuditAction.cs`.

In `AuditTrailInterceptor.CaptureAuditData`, when processing `EntityState.Modified` entries, check if `IsDeleted` changed from `false` to `true`:
```csharp
var action = AuditAction.Update;
var isDeletedProp = entry.OriginalValues.Properties
    .FirstOrDefault(p => p.Name == "IsDeleted");
if (isDeletedProp is not null)
{
    var wasDeleted = entry.OriginalValues[isDeletedProp] is true;
    var isNowDeleted = entry.CurrentValues[isDeletedProp] is true;
    if (!wasDeleted && isNowDeleted)
        action = AuditAction.SoftDelete;
}
```

**Step 5: Run tests**

Run: `dotnet test backend/ -q`
Expected: all 279+ tests PASS (new tests now also pass).

**Step 6: Commit**
```bash
git add -A
git commit -m "fix(data): replace AsyncLocal with ConditionalWeakTable in AuditTrailInterceptor; set DeletedBy in SoftDeleteInterceptor; log soft-deletes as SoftDelete action (P2-01, P2-02, P2-03)"
```

---

## Task 2: P2-04 + P2-05 — Schema/migration corrections

**Issues addressed:**
- P2-04: `AuditLogArchive` stores `EntityType` and `Action` as integers; `AuditLog` stores them as strings — cross-table queries require type-casting
- P2-05: `StagingReadyMigration` introduced an orphaned column `TestResult_Measurement_ParameterId` that differs from the EF model's `Measurement_ParameterId` canonical name; dangling FK and index in the live schema

**Files:**
- Modify: `backend/src/Quater.Backend.Data/Configurations/AuditLogArchiveConfiguration.cs`
- Create migration: run `dotnet ef migrations add FixAuditLogArchiveEnumConversion --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api`
- Verify: `backend/src/Quater.Backend.Data/Migrations/QuaterDbContextModelSnapshot.cs` for `Measurement_ParameterId`
- Create migration: run `dotnet ef migrations add DropOrphanedTestResultMeasurementParameterIdColumn --project ...`

**Step 1: Fix P2-04 — AuditLogArchive enum columns**

In `AuditLogArchiveConfiguration.cs`, add `HasConversion<string>()` to both enum columns. Check what properties are mapped — look at lines 12–57. Add:
```csharp
entity.Property(e => e.EntityType)
    .HasConversion<string>();

entity.Property(e => e.Action)
    .HasConversion<string>();
```

**Step 2: Generate corrective migration for P2-04**

```bash
dotnet ef migrations add FixAuditLogArchiveEnumConversion \
  --project backend/src/Quater.Backend.Data \
  --startup-project backend/src/Quater.Backend.Api
```

Review the generated migration. The `Up()` should ALTER the integer columns to `text`. The SQL will be:
```sql
ALTER TABLE "AuditLogArchive"
  ALTER COLUMN "EntityType" TYPE text USING "EntityType"::text,
  ALTER COLUMN "Action" TYPE text USING "Action"::text;
```

EF Core may not generate the `USING` clause. If so, edit the migration `Up()` manually to add:
```csharp
migrationBuilder.Sql("""
    ALTER TABLE "AuditLogArchive"
      ALTER COLUMN "EntityType" TYPE text
        USING CASE "EntityType"
          WHEN 0 THEN 'Sample'
          WHEN 1 THEN 'TestResult'
          WHEN 2 THEN 'Parameter'
          WHEN 3 THEN 'Lab'
          WHEN 4 THEN 'User'
          ELSE "EntityType"::text
        END,
      ALTER COLUMN "Action" TYPE text
        USING CASE "Action"
          WHEN 0 THEN 'Create'
          WHEN 1 THEN 'Update'
          WHEN 2 THEN 'Delete'
          ELSE "Action"::text
        END;
    """);
```

(Verify exact enum integer → string mapping by checking `AuditAction` and the entity type enum.)

**Step 3: Fix P2-05 — Drop orphaned TestResult_Measurement_ParameterId column**

First, verify the snapshot. In `QuaterDbContextModelSnapshot.cs` around line 808:
```csharp
t.Property("Measurement_ParameterId").HasColumnName("TestResult_Measurement_ParameterId")
```

This means EF maps the shadow property `Measurement_ParameterId` to the DB column `TestResult_Measurement_ParameterId`. The `StagingReadyMigration` added BOTH a `TestResult_Measurement_ParameterId` column via `AddColumn` AND the existing owned-entity mapping already had one from `TestResultConfiguration`. There may be duplicate FK/index.

Generate a migration to clean up:
```bash
dotnet ef migrations add DropOrphanedTestResultMeasurementParameterIdColumn \
  --project backend/src/Quater.Backend.Data \
  --startup-project backend/src/Quater.Backend.Api
```

If EF generates an empty migration (model already matches snapshot), manually edit `Up()` to drop the orphaned artifacts from `StagingReadyMigration`:
```csharp
// Drop the duplicate FK and index if they exist
migrationBuilder.Sql("""
    DO $$
    BEGIN
        IF EXISTS (
            SELECT 1 FROM information_schema.table_constraints
            WHERE constraint_name = 'FK_TestResults_Parameters_TestResult_Measurement_ParameterId'
        ) THEN
            ALTER TABLE "TestResults"
                DROP CONSTRAINT "FK_TestResults_Parameters_TestResult_Measurement_ParameterId";
        END IF;
        IF EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE indexname = 'IX_TestResults_TestResult_Measurement_ParameterId'
        ) THEN
            DROP INDEX "IX_TestResults_TestResult_Measurement_ParameterId";
        END IF;
    END $$;
    """);
```

**Step 4: Run tests**

Run: `dotnet test backend/ -q`
Expected: all tests PASS.

**Step 5: Commit**
```bash
git add -A
git commit -m "fix(data): add string conversion for AuditLogArchive enum columns; drop orphaned TestResult_Measurement_ParameterId column (P2-04, P2-05)"
```

---

## Task 3: P2-06 + P2-07 — Middleware & email service fixes

**Issues addressed:**
- P2-06: `GlobalExceptionHandlerMiddleware` logs each exception twice; `InvalidOperationException`/`ArgumentException` map to 400 and leak framework exception messages
- P2-07: `ScribanEmailTemplateService` (Services layer) holds a `lock` for the full duration of Scriban rendering

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Middleware/GlobalExceptionHandlerMiddleware.cs`
- Modify: `backend/src/Quater.Backend.Services/ScribanEmailTemplateService.cs`
- Modify: `backend/tests/Quater.Backend.Api.Tests/Middleware/` (add/extend middleware tests)
- Modify: `backend/tests/Quater.Backend.Infrastructure.Email.Tests/` (email template tests)

**Step 1: Write failing tests for P2-06**

In the Api.Tests project, add a test class `GlobalExceptionHandlerMiddlewareTests.cs` (check if it already exists in `backend/tests/Quater.Backend.Api.Tests/Middleware/`):

```csharp
[Fact]
public async Task InvalidOperationException_Returns500_NotLeakingMessage()
{
    // Arrange middleware pipeline that throws InvalidOperationException
    // Assert status code == 500 (not 400)
    // Assert response body does NOT contain the exception message
}

[Fact]
public async Task ArgumentException_Returns500_NotLeakingMessage()
{
    // Same pattern for ArgumentException
}

[Fact]
public async Task Exception_IsLoggedOnlyOnce()
{
    // Use a mock ILogger to verify LogError is called exactly once
}
```

Run: `dotnet test backend/ -q --filter "GlobalExceptionHandlerMiddlewareTests"`
Expected: FAIL.

**Step 2: Fix P2-06 — Remove double-logging and fix 400 mappings**

In `GlobalExceptionHandlerMiddleware.cs`:

1. Remove the `_logger.LogError` call from `InvokeAsync` (line 45 area) — keep only the one in `HandleExceptionAsync`.
2. In `HandleExceptionAsync`, remove the `InvalidOperationException` and `ArgumentException` cases from the switch (or move them to the default/500 branch):

```csharp
// REMOVE these cases:
InvalidOperationException ioe => (StatusCodes.Status400BadRequest, ioe.Message),
ArgumentException ae => (StatusCodes.Status400BadRequest, ae.Message),

// They fall through to default => (500, "An unexpected error occurred")
```

**Step 3: Fix P2-07 — Narrow lock scope in ScribanEmailTemplateService**

In `backend/src/Quater.Backend.Services/ScribanEmailTemplateService.cs`, the `RenderAsync` method currently holds `lock (_cacheLock)` for the entire render duration.

Replace with double-checked locking — lock only around the cache lookup/insert:

```csharp
public async Task<string> RenderAsync<TModel>(string templateName, TModel model)
{
    if (!_templateCache.TryGetValue(templateName, out var template))
    {
        lock (_cacheLock)
        {
            if (!_templateCache.TryGetValue(templateName, out template))
            {
                throw new KeyNotFoundException($"Email template '{templateName}' not found.");
            }
        }
    }

    // Render OUTSIDE the lock — Template is immutable after parsing
    var context = new TemplateContext();
    var scriptObject = new ScriptObject();
    scriptObject.Import(model, renamer: member => member.Name.ToLowerInvariant());
    context.PushGlobal(scriptObject);
    return await template.RenderAsync(context);
}
```

Note: `RegisterTemplate` already holds the lock during initial population; no changes needed there.

**Step 4: Run tests**

Run: `dotnet test backend/ -q`
Expected: all tests PASS (including new middleware tests).

**Step 5: Commit**
```bash
git add -A
git commit -m "fix(api): remove double-logging and fix 500 mapping for framework exceptions in GlobalExceptionHandlerMiddleware; narrow lock scope in ScribanEmailTemplateService (P2-06, P2-07)"
```

---

## Task 4: P2-08 — UserInvitationService decomposition

**Issue:** `UserInvitationService` is a 422-line god class with 9 injected dependencies owning: invitation state machine, user creation, email composition, token generation/hashing, and lab membership management simultaneously.

**Decomposition plan:**
- Extract `IInvitationTokenService` — token generation, hashing, expiry check
- Extract `IUserOnboardingService` — user creation via `UserManager`, password setting
- `UserInvitationService` retains: state machine, DTO mapping, orchestration
- Email composition stays in `UserInvitationService` (uses `IEmailTemplateService` + `IEmailQueue`) — it's already thin here
- Admin-lab permission checks delegate to existing `IUserLabService`

**Files:**
- Create: `backend/src/Quater.Backend.Core/Interfaces/IInvitationTokenService.cs`
- Create: `backend/src/Quater.Backend.Core/Interfaces/IUserOnboardingService.cs`
- Create: `backend/src/Quater.Backend.Services/InvitationTokenService.cs`
- Create: `backend/src/Quater.Backend.Services/UserOnboardingService.cs`
- Modify: `backend/src/Quater.Backend.Services/UserInvitationService.cs`
- Modify: `backend/src/Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs` (register new services)
- Modify: `backend/tests/Quater.Backend.Core.Tests/Services/UserInvitationServiceTests.cs`

**Step 1: Define interfaces**

`IInvitationTokenService.cs`:
```csharp
namespace Quater.Backend.Core.Interfaces;

public interface IInvitationTokenService
{
    string GenerateToken();
    string HashToken(string token);
    bool IsExpired(DateTime expiresAt);
}
```

`IUserOnboardingService.cs`:
```csharp
namespace Quater.Backend.Core.Interfaces;

public interface IUserOnboardingService
{
    Task<(bool success, IEnumerable<string> errors)> SetInitialPasswordAsync(
        User user, string password, CancellationToken ct = default);
}
```

**Step 2: Implement InvitationTokenService**

Extract the token generation logic from `UserInvitationService` (search for `RandomNumberGenerator`, `SHA256`, `ComputeHash`):

```csharp
namespace Quater.Backend.Services;

using System.Security.Cryptography;
using System.Text;
using Quater.Backend.Core.Interfaces;

public sealed class InvitationTokenService : IInvitationTokenService
{
    private const int TokenByteLength = 32;
    private const int TokenExpiryHours = 72;

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool IsExpired(DateTime expiresAt) => DateTime.UtcNow > expiresAt;
}
```

**Step 3: Implement UserOnboardingService**

Extract `AddPasswordAsync` logic from `UserInvitationService.AcceptInvitationAsync`:

```csharp
namespace Quater.Backend.Services;

using Microsoft.AspNetCore.Identity;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;

public sealed class UserOnboardingService(UserManager<User> userManager) : IUserOnboardingService
{
    public async Task<(bool success, IEnumerable<string> errors)> SetInitialPasswordAsync(
        User user, string password, CancellationToken ct = default)
    {
        var result = await userManager.AddPasswordAsync(user, password);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }
}
```

**Step 4: Refactor UserInvitationService**

Replace injections of `UserManager<User>` with `IUserOnboardingService`. Remove inline token/hash/expiry code in favour of `IInvitationTokenService`. The constructor should reduce from 9 to 7 injected dependencies.

The 9-dep constructor becomes:
```csharp
public sealed class UserInvitationService(
    QuaterDbContext context,
    IUserOnboardingService userOnboardingService,
    IUserLabService userLabService,
    IEmailQueue emailQueue,
    IEmailTemplateService emailTemplateService,
    IOptions<EmailSettings> emailSettings,
    ILogger<UserInvitationService> logger,
    ICurrentUserService currentUserService,
    IInvitationTokenService tokenService,
    TimeProvider timeProvider) : IUserInvitationService
```

**Step 5: Register new services**

In `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IInvitationTokenService, InvitationTokenService>();
services.AddScoped<IUserOnboardingService, UserOnboardingService>();
```

**Step 6: Run existing tests**

The `UserInvitationServiceTests.cs` tests must all still pass. Run:
```
dotnet test backend/ -q --filter "UserInvitationService"
```
Expected: all existing tests PASS.

**Step 7: Commit**
```bash
git add -A
git commit -m "refactor(services): decompose UserInvitationService god class into IInvitationTokenService and IUserOnboardingService (P2-08)"
```

---

## Task 5: P2-09 + P2-12 — Architecture layer violations

**Issues addressed:**
- P2-09: `ComplianceCalculator` (named pure business logic) injects `QuaterDbContext` directly — untestable without a real DB; also duplicates the three-branch compliance rule across single and batch methods
- P2-12: `LabContextAuthorizationHandler` (API layer) injects `QuaterDbContext` directly — violates Clean Architecture; untestable without EF context

**Files:**
- Create: `backend/src/Quater.Backend.Core/Interfaces/IParameterRepository.cs`
- Create: `backend/src/Quater.Backend.Data/Repositories/ParameterRepository.cs`
- Modify: `backend/src/Quater.Backend.Services/ComplianceCalculator.cs`
- Modify: `backend/src/Quater.Backend.Api/Authorization/LabContextAuthorizationHandler.cs`
- Modify: `backend/src/Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs`
- Modify: `backend/tests/Quater.Backend.Core.Tests/Services/ComplianceCalculatorTests.cs`

**Step 1: Write unit test for ComplianceCalculator using mock**

In `ComplianceCalculatorTests.cs`, replace any integration tests with pure unit tests using `NSubstitute` or `Moq` (check what's in the test project's csproj). The test should pass a mock `IParameterRepository` without a DB.

**Step 2: Define IParameterRepository**

```csharp
namespace Quater.Backend.Core.Interfaces;

public interface IParameterRepository
{
    Task<IReadOnlyList<Parameter>> GetByNamesAsync(
        IEnumerable<string> names, CancellationToken ct = default);
}
```

**Step 3: Implement ParameterRepository**

```csharp
namespace Quater.Backend.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;

public sealed class ParameterRepository(QuaterDbContext context) : IParameterRepository
{
    public async Task<IReadOnlyList<Parameter>> GetByNamesAsync(
        IEnumerable<string> names, CancellationToken ct = default) =>
        await context.Parameters
            .Where(p => names.Contains(p.Name) && !p.IsDeleted && p.IsActive)
            .ToListAsync(ct);
}
```

**Step 4: Extract shared compliance logic in ComplianceCalculator**

Replace the duplicated three-branch logic in both `CalculateComplianceAsync` and `CalculateBatchComplianceAsync` with:

```csharp
private static ComplianceStatus DetermineCompliance(Parameter p, double value)
{
    if (p.MinValue.HasValue && value < p.MinValue.Value) return ComplianceStatus.Fail;
    if (p.MaxValue.HasValue && value > p.MaxValue.Value) return ComplianceStatus.Fail;
    if (p.Threshold.HasValue && value > p.Threshold.Value) return ComplianceStatus.Warning;
    return ComplianceStatus.Pass;
}
```

Change constructor from `ComplianceCalculator(QuaterDbContext context)` to `ComplianceCalculator(IParameterRepository parameterRepository)`.

**Step 5: Fix LabContextAuthorizationHandler**

Change injection from `QuaterDbContext` to `IUserLabService`. Replace the direct DB query:
```csharp
// Before:
var userLab = await _context.UserLabs
    .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LabId == labId, ct);

// After:
var members = await _userLabService.GetUsersByLabAsync(labId, ct);
var userLab = members.FirstOrDefault(m => m.UserId == userId);
```

Note: `IUserLabService.GetUsersByLabAsync` may return DTOs rather than entities — adjust role comparison accordingly. If `GetUsersByLabAsync` doesn't return enough information (raw `UserRole` enum), add an overload or a dedicated method to `IUserLabService`:
```csharp
Task<UserLab?> GetMembershipAsync(Guid userId, Guid labId, CancellationToken ct = default);
```

Update `IUserLabService` interface and `UserLabService` implementation accordingly.

**Step 6: Register IParameterRepository**

In `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IParameterRepository, ParameterRepository>();
```

Remove `using Quater.Backend.Data;` from `LabContextAuthorizationHandler.cs` (confirm no other Data references remain).

**Step 7: Run tests**

Run: `dotnet test backend/ -q`
Expected: all tests PASS. `ComplianceCalculatorTests` should now run as pure unit tests.

**Step 8: Commit**
```bash
git add -A
git commit -m "fix(architecture): introduce IParameterRepository to decouple ComplianceCalculator from DbContext; inject IUserLabService into LabContextAuthorizationHandler (P2-09, P2-12)"
```

---

## Task 6: P2-10 + P2-15 + P2-16 — Small service correctness

**Issues addressed:**
- P2-10: `DatabaseSeeder.SeedParametersAsync` uses `AnyAsync()` with the global soft-delete filter — if all parameters are soft-deleted, seeder throws `DbUpdateException` on re-insert
- P2-15: `RevokeInvitationAsync` throws `InvitationAlreadyAccepted` for ANY non-Pending status (e.g., `Revoked`, `Expired`)
- P2-16: `UserLabService` hardcodes three error strings instead of using `ErrorMessages` constants

**Files:**
- Modify: `backend/src/Quater.Backend.Data/Seeders/DatabaseSeeder.cs`
- Modify: `backend/src/Quater.Backend.Services/UserInvitationService.cs`
- Modify: `backend/src/Quater.Backend.Services/UserLabService.cs`
- Modify: `backend/src/Quater.Backend.Core/Constants/ErrorMessages.cs`

**Step 1: Fix P2-10 — IgnoreQueryFilters in SeedParametersAsync**

In `DatabaseSeeder.cs` around line 51, find:
```csharp
if (await context.Parameters.AnyAsync()) return;
```

Replace with:
```csharp
if (await context.Parameters.IgnoreQueryFilters().AnyAsync()) return;
```

**Step 2: Fix P2-16 — Add ErrorMessages constants**

In `ErrorMessages.cs`, add three new constants in the appropriate section:
```csharp
public const string UserAlreadyLabMember = "User is already a member of this lab.";
public const string UserNotLabMember = "User is not a member of this lab.";
```

(Note: `UserNotLabMember` may already exist at line 30 — check and reuse if so.)

**Step 3: Update UserLabService to use ErrorMessages constants**

In `UserLabService.cs`, replace all three hardcoded strings:
- `"User is already a member of this lab"` → `ErrorMessages.UserAlreadyLabMember`
- `"User is not a member of this lab"` (both occurrences) → `ErrorMessages.UserNotLabMember`

Add `using Quater.Backend.Core.Constants;` if not already present.

**Step 4: Fix P2-15 — Correct error messages in RevokeInvitationAsync**

In `UserInvitationService.cs`, find `RevokeInvitationAsync`. Change the single-throw for any non-Pending status to a `switch` expression:

```csharp
_ => invitation.Status switch
{
    InvitationStatus.Accepted => throw new BadRequestException(ErrorMessages.InvitationAlreadyAccepted),
    InvitationStatus.Revoked  => throw new BadRequestException(ErrorMessages.InvitationRevoked),
    InvitationStatus.Expired  => throw new BadRequestException(ErrorMessages.InvitationExpired),
    _ => throw new BadRequestException($"Invitation cannot be revoked in status {invitation.Status}.")
}
```

**Step 5: Run tests**

Run: `dotnet test backend/ -q`
Expected: all tests PASS.

**Step 6: Commit**
```bash
git add -A
git commit -m "fix(services): use IgnoreQueryFilters in SeedParametersAsync; correct RevokeInvitation error messages; replace hardcoded UserLabService error strings with ErrorMessages constants (P2-10, P2-15, P2-16)"
```

---

## Task 7: P2-11 — PasswordController timing protection

**Issue:** `ResetPassword` applies `ApplyTimingProtectionAsync` only on the early-return path (user not found), not on the success or failure paths. An attacker can distinguish valid vs invalid email addresses by response time difference.

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Controllers/PasswordController.cs`
- Modify or create: `backend/tests/Quater.Backend.Api.Tests/Controllers/PasswordControllerTests.cs`

**Step 1: Write a failing test**

Add a test that calls `ResetPassword` with a VALID email and confirms the response time is at least `SecurityConstants.TimingProtectionDelayMs` milliseconds (showing timing protection fires on all paths). Use `Stopwatch` in the test.

Alternatively, verify via a mock that `ApplyTimingProtectionAsync` is called once regardless of which branch executes.

**Step 2: Fix ResetPassword**

In `PasswordController.cs`, find `ResetPassword` (lines 117–162). Currently the structure is:
```csharp
var sw = Stopwatch.StartNew();
var user = await _userManager.FindByEmailAsync(request.Email);
if (user is null)
{
    await ApplyTimingProtectionAsync(sw);
    return Ok();
}
// ... success path returns Ok() WITHOUT timing protection
```

Move `await ApplyTimingProtectionAsync(sw)` to be called unconditionally before ANY return, using a `try/finally` or by restructuring the method so the single `await ApplyTimingProtectionAsync(sw)` is the last thing before `return`:

```csharp
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    var sw = Stopwatch.StartNew();
    try
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return Ok();

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok();
    }
    finally
    {
        await ApplyTimingProtectionAsync(sw);
    }
}
```

**Step 3: Run tests**

Run: `dotnet test backend/ -q`
Expected: all tests PASS.

**Step 4: Commit**
```bash
git add -A
git commit -m "fix(security): apply timing protection unconditionally in PasswordController.ResetPassword (P2-11)"
```

---

## Task 8: P2-13 + P2-17 — AuthController refactoring

**Issues addressed:**
- P2-13: `authorization_code` and `refresh_token` branches in `AuthController.Token` each contain an identical ~25-line `ClaimsPrincipal` assembly block
- P2-17: User lookups use `u.Id.ToString() == userId` — forces client-side string comparison; should use typed `Guid` comparison

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Controllers/AuthController.cs`
- Modify: `backend/src/Quater.Backend.Api/Controllers/AuthorizationController.cs` (line 92 for P2-17)
- Add tests as needed

**Step 1: Fix P2-17 — Typed Guid comparison**

In `AuthController.cs` at lines 80–81 and 183–184, find patterns like:
```csharp
var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
```

Replace with:
```csharp
if (!Guid.TryParse(userId, out var userGuid))
    return Forbid();
var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
```

Apply the same fix to `AuthorizationController.cs` line 92.

**Step 2: Extract CreateClaimsPrincipalAsync helper**

In `AuthController.cs`, extract the duplicated ~25-line claims block into a private helper:

```csharp
private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(
    User user,
    IEnumerable<string> scopes)
{
    var identity = new ClaimsIdentity(
        authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        nameType: OpenIddictConstants.Claims.Name,
        roleType: OpenIddictConstants.Claims.Role);

    // ... all the claim additions currently duplicated in both branches
    // SetDestinations for each claim
    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(scopes);
    principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());
    foreach (var claim in principal.Claims)
        claim.SetDestinations(GetDestinations(claim));
    return principal;
}
```

Call this from both the `authorization_code` and `refresh_token` branches, replacing the duplicated code.

**Step 3: Run tests**

Run: `dotnet test backend/ -q`
Expected: all tests PASS.

**Step 4: Commit**
```bash
git add -A
git commit -m "refactor(api): extract CreateClaimsPrincipalAsync helper in AuthController to eliminate duplicated claims assembly; replace string Guid comparison with typed Guid.TryParse (P2-13, P2-17)"
```

---

## Task 9: P2-14 — UserService lab-switching delegation

**Issue:** `UserService.UpdateAsync` directly mutates `UserLab` rows (remove + recreate) and `CreateAsync` constructs a `UserLab` inline. Lab membership management belongs in `IUserLabService`, not `UserService`, and there are two divergent code paths for adding a user to a lab.

**Files:**
- Modify: `backend/src/Quater.Backend.Services/UserService.cs`
- Modify: `backend/src/Quater.Backend.Core/Interfaces/IUserLabService.cs` (may need new overload)
- Modify: `backend/src/Quater.Backend.Services/UserLabService.cs` (if new overload needed)
- Modify: `backend/tests/Quater.Backend.Core.Tests/Services/` (UserService tests)

**Step 1: Review current UserService.UpdateAsync lab logic**

`UpdateAsync` (lines 143–220) removes `UserLab` rows and re-adds them. `CreateAsync` (lines 105–141) constructs a `UserLab` and adds it. Both paths need to go through `IUserLabService`.

**Step 2: Ensure IUserLabService has needed methods**

`IUserLabService` already has:
- `AddUserToLabAsync(Guid userId, Guid labId, UserRole role, CancellationToken)`
- `RemoveUserFromLabAsync(Guid userId, Guid labId, CancellationToken)`
- `UpdateUserRoleInLabAsync(Guid userId, Guid labId, UserRole newRole, CancellationToken)`

These should cover what `UserService` needs.

**Step 3: Inject IUserLabService into UserService**

Change the constructor from `UserService(QuaterDbContext, UserManager<User>, ILogger<UserService>)` to add `IUserLabService`:

```csharp
public sealed class UserService(
    QuaterDbContext context,
    UserManager<User> userManager,
    IUserLabService userLabService,
    ILogger<UserService> logger) : IUserService
```

**Step 4: Replace direct UserLab mutations**

In `CreateAsync`, replace:
```csharp
var userLab = new UserLab { UserId = user.Id, LabId = dto.LabId, Role = dto.Role };
context.UserLabs.Add(userLab);
```
With:
```csharp
await _userLabService.AddUserToLabAsync(user.Id, dto.LabId, dto.Role, ct);
```

In `UpdateAsync`, replace the remove/re-add block with calls to `RemoveUserFromLabAsync` and `AddUserToLabAsync` (or `UpdateUserRoleInLabAsync` if only the role changed).

**Step 5: Run tests**

Run: `dotnet test backend/ -q`
Expected: all tests PASS.

**Step 6: Commit**
```bash
git add -A
git commit -m "refactor(services): delegate all UserLab mutations in UserService to IUserLabService (P2-14)"
```

---

## Final verification

After all 9 tasks are complete:

```bash
dotnet test backend/ -q
```

Expected: all tests pass (279+ total).

Then use `superpowers:finishing-a-development-branch` to merge to `main`.

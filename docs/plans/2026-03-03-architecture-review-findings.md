# Architecture Review Findings — 2026-03-03

> **Status:** Findings only. No implementation yet.
> **Source:** Full multi-agent architecture review of `Quater.Backend.*`
> **Scope:** Controllers, Middleware, Services, Data Layer, Core (DTOs/Validators/Mapping), Program.cs/Startup.cs

---

## Verdict

Partial targeted rewrite required — **do not ship to production in current state.**
The domain model, migration history, and OpenIddict/Serilog/EF Core scaffolding are solid enough to build on. The critical failure is that multiple security subsystems (DPoP, RLS, optimistic concurrency, anti-enumeration timing) exist as code but **provide zero actual protection**. A user today can read any other tenant's data.

Remediation is organised into three phases below.

---

## Phase 1 — Security Blockers (must fix before any production traffic)

### P1-01 · RLS session variables are never set (CRITICAL)
**Severity:** Critical  
**Files:** `Quater.Backend.Data/Constants/RlsConstants.cs`, no `DbConnectionInterceptor` exists  
**Problem:** `app.current_lab_id` and `app.is_system_admin` PostgreSQL session variables are defined but never assigned. There is no `DbConnectionInterceptor` that executes `SET LOCAL` on each connection. The RLS policies therefore evaluate against `NULL`, making them either always-false (blocking all data) or irrelevant (if the app connects as the table owner, which bypasses RLS entirely).  
**Fix:** Implement a `DbConnectionInterceptor` (override `ConnectionOpened`/`ConnectionOpenedAsync`) that executes:
```sql
SET LOCAL app.current_lab_id = '<labId>';
SET LOCAL app.is_system_admin = 'true'|'false';
```
using the current lab context from a scoped service.

---

### P1-02 · TestResults RLS policy is trivially always-true (CRITICAL)
**Severity:** Critical  
**Files:** `Quater.Backend.Data/Migrations/20260207020423_IntroduceUserLabTable.cs` lines 82–90  
**Problem:** The `lab_isolation_policy` on `TestResults` uses:
```sql
EXISTS (SELECT 1 FROM "Samples" WHERE "Samples"."Id" = "TestResults"."SampleId")
```
This is always true for any row with a matching sample — it does not apply the lab-ID filter. Every authenticated user sees all test results across all tenants.  
**Fix:** Replace the policy body with:
```sql
current_setting('app.is_system_admin', true) = 'true'
OR EXISTS (
    SELECT 1 FROM "Samples"
    WHERE "Samples"."Id" = "TestResults"."SampleId"
      AND "Samples"."LabId" = NULLIF(current_setting('app.current_lab_id', true), '')::uuid
)
```

---

### P1-03 · DPoP is an unimplemented stub (CRITICAL)
**Severity:** Critical  
**Files:** `Quater.Backend.Api/Middleware/DPoPMiddleware.cs`, `Quater.Backend.Services/Security/DPoPProofValidator.cs`  
**Problem:** Both files contain only a placeholder constant. The code comment in `DPoPMiddleware.cs` correctly self-labels this as CRITICAL. All access tokens are currently unbound bearer tokens with no replay protection.  
**Fix:** Either implement RFC 9449 DPoP validation correctly, or explicitly remove the middleware and document the security downgrade decision. Do not leave it as a silent no-op.

---

### P1-04 · AcceptInvitationAsync has no transaction — invitation token reuse possible (CRITICAL)
**Severity:** Critical  
**Files:** `Quater.Backend.Services/UserInvitationService.cs` lines 149–208  
**Problem:** Three writes (`AddPasswordAsync`, `UpdateAsync`, `SaveChangesAsync`) are executed without a wrapping transaction. If the process crashes or an exception is thrown after step 2 succeeds but before step 3 commits, the user account is active with a password but `invitation.Status` remains `Pending`. The token can then be accepted a second time.  
**Fix:** Wrap all three writes in `await using var transaction = await context.Database.BeginTransactionAsync(ct)` and commit only after all three succeed.

---

### P1-05 · DatabaseSeeder uses EnsureCreatedAsync instead of MigrateAsync (HIGH)
**Severity:** High  
**Files:** `Quater.Backend.Data/Seeders/DatabaseSeeder.cs` line 36  
**Problem:** `EnsureCreatedAsync` bypasses the migration chain entirely. Fresh deployments produce a schema from the model snapshot, silently missing all RLS policies, renamed columns, and table structure changes introduced in later migrations (`IntroduceUserLabTable`, `AddUserInvitations`, etc.).  
**Fix:** Replace `await context.Database.EnsureCreatedAsync()` with `await context.Database.MigrateAsync()`.

---

### P1-06 · SampleService.GetAllAsync and AuditLogService have no tenant filter (HIGH)
**Severity:** High  
**Files:** `Quater.Backend.Services/SampleService.cs` line 31, `Quater.Backend.Services/AuditLogService.cs` (all query methods)  
**Problem:** Both services query their respective tables with no lab ID filter. A user in Lab A can enumerate Lab B's samples and audit logs.  
**Fix:**
- `SampleService.GetAllAsync`: add `labId` as a mandatory parameter and filter `WHERE LabId = labId`.
- `AuditLogService`: inject `ILabContextAccessor` and apply a lab ID filter in all query methods, or make `labId` a required parameter.

---

### P1-07 · LabService.GetAllAsync returns soft-deleted labs (HIGH)
**Severity:** High  
**Files:** `Quater.Backend.Services/LabService.cs` line 31  
**Problem:** The query omits `.Where(l => !l.IsDeleted)`. Every other method in `LabService` filters on `IsDeleted`. Soft-deleted labs are returned to admin callers.  
**Fix:** Add `.Where(l => !l.IsDeleted)` to the `GetAllAsync` query, or verify and rely on the EF global query filter defined in `LabConfiguration.cs:75`.

---

### P1-08 · ParameterService missing soft-delete/active filters in three methods (HIGH)
**Severity:** High  
**Files:** `Quater.Backend.Services/ParameterService.cs` lines 30, 41, 104  
**Problem:**
- `GetByNameAsync` (line 30): no `IsDeleted` or `IsActive` filter — returns deleted/inactive parameters.
- `GetAllAsync` (line 41): no `IsDeleted` filter.
- `UpdateAsync` (line 104): no `IsDeleted` check — allows updating a soft-deleted parameter back to a live state.  
**Fix:** Add `&& !p.IsDeleted` (and `&& p.IsActive` where appropriate) to all three queries. For `UpdateAsync`, throw `NotFoundException` if `existing.IsDeleted` is true.

---

### P1-09 · FluentValidation is entirely non-functional (HIGH)
**Severity:** High  
**Files:** `Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs` (~line 410)  
**Problem:** Validators are registered via `AddValidatorsFromAssemblyContaining` but `AddFluentValidationAutoValidation()` is never called. All FluentValidation rules for DTOs are dead code — only Data Annotations fire.  
**Fix:** Add `services.AddFluentValidationAutoValidation()` immediately after `AddValidatorsFromAssemblyContaining`. Then audit which critical DTOs still lack validators (see P2 items).

---

### P1-10 · Optimistic concurrency silently broken for Samples and TestResults (HIGH)
**Severity:** High  
**Files:** `Quater.Backend.Core/Extensions/SampleMappingExtensions.cs` line 30, `Quater.Backend.Core/Extensions/TestResultMappingExtensions.cs` line 31  
**Problem:** Both `ToDto` mappers emit `Version = 1` hardcoded with a comment saying the field was removed from the model. The `UpdateSampleDtoValidator` and `UpdateTestResultDtoValidator` accept `Version >= 0`. Every read returns version 1; every update submits version 1; the server can never detect a concurrent modification.  
**Fix:** Either fully remove `Version` from read DTOs, write DTOs, and validators, or reinstate proper `RowVersion`/`xmin` tracking. Also consider switching `RowVersion` columns to Npgsql's `UseXminAsConcurrencyToken()` (PostgreSQL's built-in `xmin` system column) which auto-increments on every row write without application involvement.

---

## Phase 2 — Correctness Issues

### P2-01 · AuditTrailInterceptor uses AsyncLocal on a singleton — audit corruption risk (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Data/Interceptors/AuditTrailInterceptor.cs` lines 42–43  
**Problem:** `AsyncLocal<bool>` and `AsyncLocal<List<AuditLogData>>` are instance fields on a singleton interceptor. Under concurrent requests, the capture list can be overwritten between `CaptureAuditData` and `AddAuditLogsToContext`, corrupting or dropping audit log entries.  
**Fix:** Replace the `AsyncLocal` fields with a local `List<AuditLogData>` variable declared inside `SavingChanges`/`SavingChangesAsync` and passed directly to `AddAuditLogsToContext`.

---

### P2-02 · SoftDeleteInterceptor never populates DeletedBy (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Data/Interceptors/SoftDeleteInterceptor.cs` lines 99–105  
**Problem:** The interceptor validates that the `DeletedBy` property exists on the entity but never assigns a value to it. Every soft-deleted record has `DeletedBy = null`.  
**Fix:** Inject `ICurrentUserService` into `SoftDeleteInterceptor` and set `deletedByProperty.SetValue(entry.Entity, userId.ToString())` in `ApplySoftDelete`.

---

### P2-03 · Soft-delete operations logged as AuditAction.Update, not Delete (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Data/Interceptors/SoftDeleteInterceptor.cs` line 84, `Quater.Backend.Data/Interceptors/AuditTrailInterceptor.cs` line 204  
**Problem:** `SoftDeleteInterceptor` converts `EntityState.Deleted` to `EntityState.Modified` before `AuditTrailInterceptor` runs, so soft-deletes are recorded as `AuditAction.Update`. The audit log cannot distinguish deletions from field updates.  
**Fix:** Either run interceptors in a defined order and detect the `IsDeleted` transition explicitly in `AuditTrailInterceptor`, or add a `AuditAction.SoftDelete` enum value and check for `IsDeleted` changing from `false` to `true` in the captured change set.

---

### P2-04 · AuditLogArchive stores enums as integers; AuditLog stores them as strings (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Data/Configurations/AuditLogArchiveConfiguration.cs`  
**Problem:** `AuditLog` has `HasConversion<string>()` on `EntityType` and `Action`. `AuditLogArchive` does not — these columns are stored as integers. Cross-table audit queries require type-casting; archival jobs moving rows must convert types.  
**Fix:** Add `HasConversion<string>()` to `EntityType` and `Action` in `AuditLogArchiveConfiguration`. Generate a corrective migration to cast the existing integer columns to `text`.

---

### P2-05 · TestResult migration column name drift — orphaned column in schema (HIGH)
**Severity:** High  
**Files:** `Quater.Backend.Data/Migrations/20260204080821_StagingReadyMigration.cs`, `Quater.Backend.Data/Configurations/TestResultConfiguration.cs`  
**Problem:** `StagingReadyMigration` added a column `TestResult_Measurement_ParameterId` with its own FK, while the EF configuration maps to `Measurement_ParameterId`. A fresh migration-chain deployment has an orphaned column and a dangling FK. EF queries use the wrong-named column.  
**Fix:** Audit `QuaterDbContextModelSnapshot.cs` for the canonical column name, then generate a corrective migration that drops `TestResult_Measurement_ParameterId` and its FK.

---

### P2-06 · GlobalExceptionHandlerMiddleware double-logs and maps InvalidOperationException to 400 (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Api/Middleware/GlobalExceptionHandlerMiddleware.cs`  
**Problems:**
1. Exception is logged at line 45 (`InvokeAsync`) and again at line 53 (`HandleExceptionAsync`) — two `LogError` entries per exception.
2. `InvalidOperationException` and `ArgumentException` map to HTTP 400 and expose `exception.Message` to the client. These are framework exceptions that should map to 500 with the message hidden in production.  
**Fix:** Remove the second `LogError` in `HandleExceptionAsync`. Change `InvalidOperationException` and `ArgumentException` to fall into the `_` catch-all (500).

---

### P2-07 · ScribanEmailTemplateService holds cache lock during template render (HIGH)
**Severity:** High (performance)  
**Files:** `Quater.Backend.Infrastructure.Email/ScribanEmailTemplateService.cs` lines 31–50  
**Problem:** `lock (_cacheLock)` is held for the full duration of Scriban rendering, not just the cache dictionary lookup. All concurrent email sends are serialized.  
**Fix:** Release the lock after the dictionary lookup (template is read-only after initial parse). Use `ConcurrentDictionary` to eliminate the lock entirely, or narrow the lock scope to the cache read only.

---

### P2-08 · UserInvitationService is a 416-line god class (HIGH)
**Severity:** High (maintainability)  
**Files:** `Quater.Backend.Services/UserInvitationService.cs`  
**Problem:** 9 injected dependencies. Owns: invitation state machine, user creation, lab membership management, admin permission checks, email composition, token generation/hashing, and DTO mapping simultaneously.  
**Fix:** Decompose into:
- `IInvitationTokenService` — token generation, hashing, expiry
- `IUserOnboardingService` — user creation + password via `userManager`
- Extract email composition to `IInvitationEmailService`
- Move admin-lab permission checks to `ILabAuthorizationService` or into the existing authorization handler

---

### P2-09 · ComplianceCalculator directly queries DbContext — untestable (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Services/ComplianceCalculator.cs`  
**Problem:** Named as pure business logic but injects `QuaterDbContext` and queries it directly. Cannot be unit tested without a real or in-memory database. Also duplicates the three-branch compliance rule in both single and batch methods.  
**Fix:** Extract a private static `DetermineCompliance(Parameter p, double value)` method for the shared logic. Inject a `IParameterRepository` interface instead of `QuaterDbContext` directly.

---

### P2-10 · SeedParametersAsync uses AnyAsync() without IgnoreQueryFilters (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Data/Seeders/DatabaseSeeder.cs` line 51  
**Problem:** `context.Parameters.AnyAsync()` respects the soft-delete global query filter. If all seeded parameters are soft-deleted, the seeder re-inserts them and throws a `DbUpdateException` on startup due to the unique index on `Parameters.Name`.  
**Fix:** Use `context.Parameters.IgnoreQueryFilters().AnyAsync()`.

---

### P2-11 · PasswordController.ResetPassword timing protection not applied on all branches (HIGH)
**Severity:** High (security)  
**Files:** `Quater.Backend.Api/Controllers/PasswordController.cs`  
**Problem:** `ForgotPassword` correctly applies `ApplyTimingProtectionAsync` unconditionally. `ResetPassword` applies it only on the early-return (user not found) path, not on the success or failure paths. An attacker can distinguish valid vs invalid emails by response time.  
**Fix:** Apply `ApplyTimingProtectionAsync` unconditionally at the end of `ResetPassword` regardless of which branch was taken.

---

### P2-12 · LabContextAuthorizationHandler injects DbContext directly (MEDIUM)
**Severity:** Medium (layer boundary)  
**Files:** `Quater.Backend.Api/Authorization/LabContextAuthorizationHandler.cs` line 29  
**Problem:** API layer depends on Data layer (`QuaterDbContext`) directly, violating Clean Architecture. Handler cannot be unit tested without an EF Core context.  
**Fix:** Inject `IUserLabService` (or a lightweight `IUserLabRepository`) instead of `QuaterDbContext`.

---

### P2-13 · AuthController.Token has duplicated 25-line claims-assembly block (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Api/Controllers/AuthController.cs`  
**Problem:** The `authorization_code` and `refresh_token` grant branches each contain an identical ~25-line block that builds a `ClaimsPrincipal`. Future changes to claim assembly must be applied twice and can silently diverge.  
**Fix:** Extract a private `CreateClaimsPrincipalAsync(User user, IEnumerable<string> scopes)` helper and call it from both branches.

---

### P2-14 · UserService.UpdateAsync contains lab-switching logic (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Services/UserService.cs`  
**Problem:** `UpdateAsync` directly removes and recreates `UserLab` rows. Lab membership management belongs in `IUserLabService`, not `UserService`. `CreateAsync` also constructs a `UserLab` inline instead of delegating, creating two divergent code paths for adding a user to a lab.  
**Fix:** Delegate all `UserLab` mutations in `UserService` to `IUserLabService`.

---

### P2-15 · AcceptInvitationAsync and RevokeInvitationAsync use wrong error messages for status (LOW)
**Severity:** Low  
**Files:** `Quater.Backend.Services/UserInvitationService.cs` line 220  
**Problem:** `RevokeInvitationAsync` throws `ErrorMessages.InvitationAlreadyAccepted` for any non-Pending status, including `Revoked` and `Expired`. A caller revoking an expired invitation receives a misleading message.  
**Fix:** Use a `switch` expression over `InvitationStatus` to return the appropriate error message, matching the pattern already used in `GetByTokenAsync`.

---

### P2-16 · UserLabService hardcodes error strings instead of using ErrorMessages (LOW)
**Severity:** Low  
**Files:** `Quater.Backend.Services/UserLabService.cs` lines 33, 62, 75  
**Problem:** Three strings (`"User is already a member of this lab"`, `"User is not a member of this lab"` twice) are hardcoded rather than using `ErrorMessages` constants.  
**Fix:** Add the missing constants to `Quater.Backend.Core/Constants/ErrorMessages.cs` and reference them.

---

### P2-17 · AuthController user lookup uses string comparison instead of typed Guid query (MEDIUM)
**Severity:** Medium  
**Files:** `Quater.Backend.Api/Controllers/AuthController.cs` lines 80–81, 183–184; `AuthorizationController.cs` line 92  
**Problem:** `u.Id.ToString() == userId` forces EF Core to call `ToString()` on every row's `Id` client-side or generate an inefficient SQL `CAST`, preventing index use on the primary key.  
**Fix:** Parse `userId` to `Guid` once with `Guid.TryParse`, then compare typed: `u.Id == userGuid`.

---

## Phase 3 — Hygiene and Standards

### P3-01 · Console.WriteLine debug calls left in production code (LOW)
**Files:** `Controllers/UsersController.cs`, `Middleware/GlobalExceptionHandlerMiddleware.cs`, `Middleware/LabContextMiddleware.cs`, `Startup.cs` lines 129–134, `Services/UserService.cs` line 22  
**Fix:** Remove all `Console.WriteLine` calls. `Startup.cs` should use `app.UseSerilogRequestLogging()` instead of the inline middleware.

---

### P3-02 · Three different default page sizes across the codebase (LOW)
**Files:** `CommonDto.cs` (`PaginationQueryDto` default = 10), `PaginationHelper.cs` (default = 20), `AuditLogFilterDto.cs` (default = 50)  
**Fix:** Define `public static class Pagination { public const int DefaultPageSize = 20; public const int MaxPageSize = 100; }` in `AppConstants.cs` and reference it everywhere. Remove the duplicate `PagedResponse<T>` type from `Quater.Backend.Core/Models/PagedResponse.cs` — `PagedResult<T>` in `CommonDto.cs` already covers it.

---

### P3-03 · pageSize has no upper-bound enforcement in 12 service methods (MEDIUM)
**Files:** All CRUD services — `AuditLogService`, `LabService`, `ParameterService`, `SampleService`, `TestResultService`, `UserService`  
**Problem:** `PaginationHelper` with `Math.Clamp(pageSize, 1, MaxPageSize)` exists but none of the services use it. A caller can pass `pageSize=10000` for an unconstrained full table scan.  
**Fix:** Use `PaginationHelper.ToPagedResponseAsync()` across all services, or apply `Math.Clamp(pageSize, 1, AppConstants.Pagination.MaxPageSize)` at the top of every paginated method.

---

### P3-04 · FluentValidation and Data Annotations coexist with divergent constraints (LOW)
**Files:** `Core/Validators/ParameterValidator.cs` vs `Core/Validators/CreateParameterDtoValidator.cs`  
**Problem:** Unit max length (50 vs 20), description max length (1000 vs 500), threshold polarity (`>0` vs `>=0`) differ between entity validator and DTO validator for the same fields.  
**Fix:** After enabling FluentValidation auto-validation (P1-09), audit and align constraints. Remove Data Annotations where FluentValidation covers the same rule.

---

### P3-05 · NotNull() on non-nullable double Value is a dead validation rule (LOW)
**Files:** `Core/Validators/CreateTestResultDtoValidator.cs` line 20, `UpdateTestResultDtoValidator.cs` line 17  
**Fix:** Remove `.NotNull()` on `double` properties. Add a meaningful range rule if zero or negative measurements are invalid.

---

### P3-06 · Multiple DateTime.UtcNow usages instead of injected TimeProvider (LOW)
**Files:** `Quater.Backend.Data/Interceptors/AuditTrailInterceptor.cs` line 164, `Quater.Backend.Data/Interceptors/SoftDeleteInterceptor.cs` line 91, `Quater.Backend.Services/UserLabService.cs` line 41, `Quater.Backend.Api/Controllers/AuthController.cs` line 126  
**Fix:** Inject `TimeProvider` into all four and replace `DateTime.UtcNow` with `_timeProvider.GetUtcNow()`.

---

### P3-07 · GetDestinations helper duplicated verbatim between AuthController and AuthorizationController (LOW)
**Files:** `Controllers/AuthController.cs` lines 277–291, `Controllers/AuthorizationController.cs` lines 169–183  
**Fix:** Move the static method to `Helpers/AuthHelpers.cs` and delete one copy.

---

### P3-08 · Startup.Configure runs DB migration and seeding in the middleware pipeline (MEDIUM)
**Files:** `Quater.Backend.Api/Startup.cs` lines 180–204  
**Problem:** Migration and seeding happen inside `Configure()` using `.GetAwaiter().GetResult()` (sync-over-async). Errors are caught and swallowed, potentially leaving the app running with an inconsistent database.  
**Fix:** Move `DatabaseSeeder.SeedAsync` and `OpenIddictSeeder.SeedAsync` to an `IHostedService.StartAsync` implementation. Call them with proper `await` and let startup failures propagate.

---

### P3-09 · Health endpoint hard-casts IEmailQueue to BackgroundEmailQueue (LOW)
**Files:** `Quater.Backend.Api/Startup.cs` line 170  
**Problem:** `(BackgroundEmailQueue)queue` will throw `InvalidCastException` if the implementation is ever changed.  
**Fix:** Add an `int ApproximateCount { get; }` property to `IEmailQueue` and remove the hard cast.

---

### P3-10 · OpenIddictSeeder bypasses IConfiguration for environment variable reads (LOW)
**Files:** `Quater.Backend.Api/Seeders/OpenIddictSeeder.cs` line 20  
**Problem:** Uses `Environment.GetEnvironmentVariable` instead of `IConfiguration`, bypassing the configuration pipeline, secrets files, and test configuration overrides.  
**Fix:** Pass `IConfiguration` as a parameter to `SeedAsync` and read values via `configuration["OpenIddict:ClientId"]`.

---

### P3-11 · UserInvitationsController has no class-level [Authorize] (LOW)
**Files:** `Controllers/UserInvitationsController.cs`  
**Problem:** No class-level `[Authorize]` baseline. Any new endpoint added without an explicit attribute is publicly accessible by default. All other controllers have a class-level attribute.  
**Fix:** Add `[Authorize]` at class level; explicitly mark the two public endpoints with `[AllowAnonymous]`.

---

### P3-12 · LabDto has duplicate CreatedDate and CreatedAt fields (LOW)
**Files:** `Core/DTOs/LabDto.cs` lines 14 and 16, `Core/Extensions/LabMappingExtensions.cs` lines 22–24  
**Problem:** Both fields are assigned `lab.CreatedAt` in the mapper, serializing two identical values to API clients.  
**Fix:** Remove `CreatedDate`. Keep only `CreatedAt`.

---

### P3-13 · SampleDto and TestResultDto expose infrastructure fields to clients (LOW)
**Files:** `Core/DTOs/SampleDto.cs`, `Core/DTOs/TestResultDto.cs`  
**Problem:** `IsDeleted`, `IsSynced`, `Version` (hardcoded 1), raw `CreatedBy`/`LastModifiedBy` GUIDs are exposed in read DTOs. Clients should never see `IsDeleted` (deleted records should simply not appear) and `IsSynced` is a server-side sync flag with no meaning to web consumers.  
**Fix:** Remove `IsDeleted`, `IsSynced` from both DTOs. Replace raw `CreatedBy`/`LastModifiedBy` GUIDs with display name strings resolved at mapping time, or remove them if not needed.

---

### P3-14 · static class System shadows System namespace (LOW)
**Files:** `Quater.Backend.Core/Constants/System.cs`  
**Problem:** `public static class System` shadows the `System` namespace within any file that uses `Quater.Backend.Core.Constants`.  
**Fix:** Rename to `SystemConstants` or `SystemUser`.

---

### P3-15 · Roles string constants and UserRole enum are parallel redundant systems (LOW)
**Files:** `Quater.Backend.Core/Constants/Roles.cs`, `shared/Enums/UserRole.cs`  
**Problem:** Two ways to express the same role values; no clear rule for which to use in new code.  
**Fix:** Pick one (prefer the `UserRole` enum per AGENTS.md) and delete or deprecate the string constant class.

---

### P3-16 · NotFoundException leaks internal GUIDs to API responses (LOW)
**Files:** `Quater.Backend.Core/Exceptions/NotFoundException.cs` lines 40–43  
**Problem:** `NotFoundException(string entityName, Guid entityId)` embeds the entity ID in the message, which propagates to the HTTP response.  
**Fix:** Remove the `entityId` parameter from the message. Expose only the entity type name.

---

### P3-17 · TimeSpan.Parse without TryParse for lockout config (LOW)
**Files:** `Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs` line 201  
**Problem:** A malformed `DefaultLockoutTimeSpan` value throws a `FormatException` during service registration with no meaningful configuration error message.  
**Fix:** Use `TimeSpan.TryParse` with a fallback and a logged warning, or add the key to `ConfigurationValidationExtensions.ValidateConfiguration`.

---

### P3-18 · Parameter.MinValue/MaxValue/Threshold stored as double instead of decimal (LOW)
**Files:** `Quater.Backend.Data/Configurations/ParameterConfiguration.cs`, Parameter model  
**Problem:** Binary floating-point cannot represent regulatory compliance thresholds exactly (e.g., WHO 0.010 mg/L arsenic limit). Boundary comparisons have rounding errors.  
**Fix:** Change the properties to `decimal` and apply `HasColumnType("numeric(18,6)")` in EF configuration. Generate a migration.

---

### P3-19 · Empty FinalMigrationsMvp migration pollutes migration history (LOW)
**Files:** `Quater.Backend.Data/Migrations/20260216175554_FinalMigrationsMvp.cs`  
**Problem:** Both `Up` and `Down` are empty. This is a no-op migration.  
**Fix:** If no deployment has run this migration in production, remove it with `dotnet ef migrations remove`. Otherwise document it explicitly as intentional.

---

### P3-20 · X-XSS-Protection security header is deprecated and exploitable (LOW)
**Files:** `Quater.Backend.Api/Middleware/SecurityHeadersMiddleware.cs` line 65  
**Problem:** `X-XSS-Protection: 1; mode=block` is deprecated in all modern browsers (Chrome 78+) and has documented security issues in older browsers.  
**Fix:** Set it to `"0"` (disabled) and rely on `Content-Security-Policy` per OWASP 2023 guidance.

---

## Summary by Severity

| Severity | Count | Phase |
|---|---|---|
| Critical | 3 | P1 |
| High | 7 | P1, P2 |
| Medium | 10 | P1, P2 |
| Low | 17 | P2, P3 |
| **Total** | **37** | |

## Recommended Fix Order (Phase 1 first)

1. P1-01 RLS session variables never set
2. P1-02 TestResults RLS policy always-true
3. P1-03 DPoP stub decision
4. P1-04 AcceptInvitationAsync transaction
5. P1-05 EnsureCreatedAsync → MigrateAsync
6. P1-06 Tenant filters on SampleService + AuditLogService
7. P1-07 LabService.GetAllAsync soft-delete filter
8. P1-08 ParameterService missing filters
9. P1-09 AddFluentValidationAutoValidation
10. P1-10 Optimistic concurrency (Version hardcoded)
11. P2-05 TestResult migration column name drift
12. P2-01 AuditTrailInterceptor AsyncLocal corruption
13. P2-02 SoftDeleteInterceptor DeletedBy never set
14. P2-07 ScribanEmailTemplateService lock scope
15. P2-03 Soft-delete logged as Update
16. P2-11 PasswordController timing protection
17. P2-08 UserInvitationService decomposition
18. P2-13 AuthController duplicated claims block
19. P2-06 GlobalExceptionHandlerMiddleware double-log + wrong 400 mapping
20. P2-09 ComplianceCalculator DbContext dependency
21. Remaining P2 and P3 items in any order

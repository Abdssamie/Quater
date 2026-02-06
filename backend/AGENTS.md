# Backend Agent Instructions - Quater Water Quality Lab Management System

## Build, Lint & Test Commands

### Backend (.NET 10)

```bash
# Build
dotnet build backend/Quater.Backend.sln
dotnet build backend/Quater.Backend.sln --configuration Release

# Run tests
dotnet test backend/tests/Quater.Backend.Api.Tests/
dotnet test backend/tests/Quater.Backend.Core.Tests/
dotnet test backend/tests/Quater.Backend.Sync.Tests/

# Run single test
dotnet test --filter "FullyQualifiedName=Quater.Backend.Api.Tests.SampleControllerTests.CreateSample_ValidData_ReturnsCreated"
dotnet test --filter "FullyQualifiedName~SampleController"  # All tests in class

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Lint (if using dotnet-format)
dotnet format backend/Quater.Backend.sln --verify-no-changes
dotnet format backend/Quater.Backend.sln  # Auto-fix

# Run API locally
cd backend/src/Quater.Backend.Api
dotnet run
```

---

## C# Code Style Guidelines (.NET 10 - C# 13)

### 🏛️ Type System & Domain Modeling

**Discriminated Unions (Workarounds)**: Use abstract record hierarchies for domain states.
```csharp
public abstract record OrderState;
public sealed record Pending(DateTime Created) : OrderState;
public sealed record Shipped(DateTime Dispatched, string TrackingId) : OrderState;
public sealed record Cancelled(string Reason) : OrderState;
```

**Strongly Typed IDs**: Use `readonly record struct` to prevent accidental ID assignment.
```csharp
public readonly record struct SampleId(Guid Value);
public readonly record struct CustomerId(Guid Value);
public readonly record struct LocationCoordinate(double Latitude, double Longitude);
```

**Collection Expressions**: Always use `[]` for initialization (C# 12+).
```csharp
public sealed record CustomerDto(CustomerId Id, string Name, string Email)
{
    public string[] Tags { get; init; } = []; // Collection expression
}

// Arrays
int[] numbers = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob", "Charlie"];
```

**Primary Constructors**: Use for all records and classes to reduce boilerplate.
```csharp
public sealed class OrderService(IOrderRepository repo, ILogger<OrderService> logger) : IOrderService
{
    public async Task CreateAsync(OrderDto dto, CancellationToken ct)
    {
        logger.LogInformation("Creating order");
        await repo.SaveAsync(dto.ToDomain(), ct);
    }
}
```

**File-Scoped Namespaces**: Always use to save indentation.
```csharp
namespace Quater.Backend.Core.Models;

public sealed class Sample { /* ... */ }
```

### 🚀 Performance & Memory Safety

**System.Threading.Lock**: In .NET 9+, use the new `Lock` type for better performance.
```csharp
private readonly Lock _gate = new();

public void Process()
{
    lock (_gate)
    {
        // Thread-safe code
    }
}
```

**SearchValues**: Use `SearchValues<T>` for high-frequency string/byte searching.
```csharp
private static readonly SearchValues<char> ValidSchemeChars = 
    SearchValues.Create("abcdefghijklmnopqrstuvwxyz0123456789+-.");

public bool IsValidScheme(ReadOnlySpan<char> scheme)
{
    return scheme.ContainsAny(ValidSchemeChars);
}
```

**ValueTask Optimization**: Use `ValueTask<T>` for methods that frequently return cached results.
```csharp
public ValueTask<Sample?> GetCachedSampleAsync(Guid id, CancellationToken ct)
{
    if (_cache.TryGetValue(id, out var sample))
        return ValueTask.FromResult<Sample?>(sample);
    
    return new ValueTask<Sample?>(LoadFromDatabaseAsync(id, ct));
}
```

**Frozen Collections**: Use `ToFrozenDictionary()` and `ToFrozenSet()` for read-only lookups.
```csharp
private static readonly FrozenDictionary<string, decimal> Thresholds = 
    new Dictionary<string, decimal>
    {
        ["pH"] = 8.5m,
        ["Turbidity"] = 5.0m,
        ["Chlorine"] = 5.0m
    }.ToFrozenDictionary();
```

**Span & Memory**: Use `ReadOnlySpan<char>` for high-performance string parsing.
```csharp
public static bool TryParseSampleId(ReadOnlySpan<char> input, out Guid id)
{
    return Guid.TryParse(input, out id);
}
```

### 🧩 Functional Patterns & Logic

**Switch Expressions**: Use for all assignment logic and pattern matching.
```csharp
public decimal CalculateDiscount(Customer customer) => customer switch
{
    { IsPremium: true } => 0.2m,
    { OrderCount: > 10 } => 0.1m,
    _ => 0m
};

public string GetSampleStatus(Sample sample) => sample switch
{
    { TestResults.Count: 0 } => "Pending",
    { TestResults: var results } when results.All(r => r.IsCompliant) => "Compliant",
    _ => "Non-Compliant"
};
```

**Ternary for Simple Logic**: Use ternary operators for simple assignments.
```csharp
var status = isCompliant ? "Pass" : "Fail";
var discount = isPremium ? basePrice * 0.8m : basePrice;
```

**Result Pattern**: Avoid try-catch for business logic. Use `Result<T, TError>` for explicit failures.
```csharp
public sealed record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

public async Task<Result<Sample>> CreateSampleAsync(CreateSampleDto dto, CancellationToken ct)
{
    if (dto is null)
        return Result<Sample>.Failure("Sample data is required");
    
    var sample = new Sample { /* ... */ };
    await _repository.AddAsync(sample, ct);
    
    return Result<Sample>.Success(sample);
}
```

**Pure Logic Extraction**: Move complex math/logic into static methods for testability.
```csharp
public static class ComplianceCalculator
{
    public static bool IsCompliant(decimal value, decimal threshold) => value <= threshold;
    
    public static ComplianceStatus DetermineStatus(IEnumerable<TestResult> results) =>
        results.All(r => r.IsCompliant) ? ComplianceStatus.Pass : ComplianceStatus.Fail;
    
    public static decimal CalculateAverage(IEnumerable<decimal> values) =>
        values.Any() ? values.Average() : 0m;
}
```

**Higher-Order Functions**: Pass `Func<>` or `Action<>` to separate iteration from logic.
```csharp
public async Task ProcessBatchAsync<T>(
    IEnumerable<T> items,
    Func<T, CancellationToken, Task> processor,
    CancellationToken ct)
{
    foreach (var item in items)
    {
        await processor(item, ct);
    }
}
```

### 🌐 Asynchronous & Concurrency

**CancellationToken Propagation**: Every async method must accept and pass `CancellationToken`.
```csharp
public async Task<Sample?> GetSampleAsync(Guid id, CancellationToken ct)
{
    return await _dbContext.Samples
        .FirstOrDefaultAsync(s => s.Id == id, ct);
}
```

**Task.WhenEach (.NET 9+)**: Process multiple tasks as they complete.
```csharp
public async Task ProcessInParallelAsync(IEnumerable<Task<Data>> tasks, CancellationToken ct)
{
    await foreach (var task in Task.WhenEach(tasks))
    {
        var result = await task; // Guaranteed completed
        await HandleAsync(result, ct);
    }
}
```

**Avoid Eliding Tasks**: Always use `async/await` for proper exception handling.
```csharp
// ❌ BAD: Elides task, loses stack trace
public Task<Sample> GetSampleAsync(Guid id) => _repository.GetAsync(id);

// ✅ GOOD: Proper async/await
public async Task<Sample> GetSampleAsync(Guid id, CancellationToken ct)
{
    return await _repository.GetAsync(id, ct);
}
```

**Async All the Way**: Never use `.Result` or `.Wait()`.
```csharp
// ❌ BAD: Blocks thread
var sample = GetSampleAsync(id).Result;

// ✅ GOOD: Async all the way
var sample = await GetSampleAsync(id, ct);
```

**Nullable Reference Types**: Must be enabled. Treat warnings as errors.
```csharp
#nullable enable

public sealed class Sample
{
    public Guid Id { get; init; }
    public string SampleType { get; init; } = string.Empty; // Required
    public string? Notes { get; init; } // Optional
}
```

**Guard Clauses**: Use `ArgumentNullException.ThrowIfNull()` and `ArgumentException.ThrowIfNullOrWhiteSpace()`.
```csharp
public async Task<Result<Order>> ProcessAsync(OrderRequest req, CancellationToken ct)
{
    ArgumentNullException.ThrowIfNull(req);
    ArgumentException.ThrowIfNullOrWhiteSpace(req.CustomerId, nameof(req.CustomerId));
    
    var order = await _repo.GetAsync(req.Id, ct);
    return order is null ? Result<Order>.Failure("Not Found") : Result<Order>.Success(order);
}
```

### 🧪 Testing & Observability

**TimeProvider**: Always inject `TimeProvider` instead of `DateTime.Now` for deterministic testing.
```csharp
public sealed class SampleService(ISampleRepository repo, TimeProvider timeProvider)
{
    public async Task<Sample> CreateAsync(CreateSampleDto dto, CancellationToken ct)
    {
        var sample = new Sample
        {
            CreatedAt = timeProvider.GetUtcNow(),
            // ...
        };
        return await repo.AddAsync(sample, ct);
    }
}

// In tests
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
var service = new SampleService(repo, fakeTime);
```

**Structured Logging**: Use `nameof` and log properties for searchability.
```csharp
_logger.LogInformation(
    "Sample {SampleId} created by {UserId} at {Location}",
    sample.Id,
    userId,
    sample.Location);

_logger.LogError(
    exception,
    "Failed to process sample {SampleId}",
    sampleId);
```

**Explicit Nameof**: Use `nameof` for all logging, guard clauses, and property references.
```csharp
public SampleService(IRepository<Sample> repository, ILogger<SampleService> logger)
{
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### 📐 Code Organization

**Composition Over Inheritance**: Use interfaces and decorator pattern.
```csharp
public sealed class LoggingSampleService(ISampleService inner, ILogger logger) : ISampleService
{
    public async Task<Sample> CreateAsync(CreateSampleDto dto, CancellationToken ct)
    {
        logger.LogInformation("Creating sample");
        var result = await inner.CreateAsync(dto, ct);
        logger.LogInformation("Sample {Id} created", result.Id);
        return result;
    }
}
```

**Minimalist Constructors**: If dependencies > 5, the class likely violates SRP.

**Static Abstract Interfaces**: Use for factory patterns or polymorphic logic.
```csharp
public interface IEntity<TSelf> where TSelf : IEntity<TSelf>
{
    static abstract TSelf Create(Guid id);
}

public sealed record Sample : IEntity<Sample>
{
    public static Sample Create(Guid id) => new Sample { Id = id };
}
```

**Implicit Usings**: Enable in `.csproj` to keep files clean.
```xml
<PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

### Naming Conventions

- **PascalCase**: Classes, methods, properties, public fields (`SampleService`, `CreateSample()`)
- **camelCase**: Local variables, parameters, private fields (`sampleId`, `_dbContext`)
- **Interfaces**: Prefix with `I` (`ISampleRepository`, `ISyncService`)
- **Async methods**: Suffix with `Async` (`GetSampleAsync()`, `SyncDataAsync()`)

### Formatting

- **Indentation**: 4 spaces (no tabs)
- **Braces**: Allman style (opening brace on new line)
- **Line length**: Max 120 characters

### Imports Order

```csharp
// System namespaces first (if not using implicit usings)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Third-party packages
using Microsoft.EntityFrameworkCore;
using FluentValidation;

// Project namespaces
using Quater.Backend.Core.Models;
using Quater.Backend.Core.Interfaces;
```

---

## 🎯 Quater Project-Specific Coding Standards

These standards were established during the enterprise code quality refactoring and must be followed for all new code and refactoring work.

### ✅ Exception Handling & Error Management

**DO: Use Custom Exceptions for Business Logic**

Never use generic exceptions like `InvalidOperationException` or `ArgumentException` for business logic errors. Always use our custom exceptions:

```csharp
// ✅ GOOD: Custom exceptions with proper HTTP status mapping
public async Task<User> GetByIdAsync(Guid id, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(id, ct);
    if (user is null)
        throw new NotFoundException(ErrorMessages.UserNotFound);
    
    return user;
}

public async Task<Lab> CreateAsync(CreateLabDto dto, CancellationToken ct)
{
    var existing = await _repository.GetByCodeAsync(dto.Code, ct);
    if (existing is not null)
        throw new ConflictException(ErrorMessages.LabCodeAlreadyExists);
    
    // Create lab...
}

// ❌ BAD: Generic exceptions
public async Task<User> GetByIdAsync(Guid id, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(id, ct);
    if (user is null)
        throw new InvalidOperationException("User not found"); // Wrong!
    
    return user;
}
```

**Available Custom Exceptions** (in `Quater.Backend.Core.Exceptions/`):

- `NotFoundException` → 404 Not Found
- `BadRequestException` → 400 Bad Request
- `ConflictException` → 409 Conflict (duplicates, concurrency issues)
- `ForbiddenException` → 403 Forbidden
- `SyncException` → 500 Internal Server Error (sync-specific errors)

**Centralized Error Messages**

Always use constants from `ErrorMessages.cs` instead of hardcoding strings:

```csharp
// ✅ GOOD: Centralized error messages
throw new NotFoundException(ErrorMessages.SampleNotFound);
throw new ConflictException(ErrorMessages.ParameterCodeAlreadyExists);

// ❌ BAD: Hardcoded error messages
throw new NotFoundException("Sample not found"); // Wrong!
throw new ConflictException("Parameter code already exists"); // Wrong!
```

### ✅ Enums vs Magic Strings

**DO: Use Enums for Fixed Value Sets**

Never use magic strings for status values, types, or any fixed set of values. Always use enums:

```csharp
// ✅ GOOD: Enum for status
public async Task LogSyncAsync(string entityType, SyncStatus status, CancellationToken ct)
{
    var log = new SyncLog
    {
        EntityType = entityType,
        Status = status, // SyncStatus enum
        Timestamp = DateTime.UtcNow
    };
    await _repository.AddAsync(log, ct);
}

// Usage
await LogSyncAsync(nameof(Sample), SyncStatus.Synced, ct);
await LogSyncAsync(nameof(TestResult), SyncStatus.Failed, ct);

// ❌ BAD: Magic strings
public async Task LogSyncAsync(string entityType, string status, CancellationToken ct)
{
    var log = new SyncLog
    {
        EntityType = entityType,
        Status = status, // string - prone to typos!
        Timestamp = DateTime.UtcNow
    };
    await _repository.AddAsync(log, ct);
}

// Usage - typos not caught at compile time!
await LogSyncAsync("Sample", "success", ct); // Wrong!
await LogSyncAsync("TestResult", "failed", ct); // Wrong!
```

**Available Enums** (in `shared/Enums/`):

- `SyncStatus` → Synced, Failed, InProgress, Pending
- `SampleType` → Drinking, Wastewater, Surface, Groundwater
- `UserRole` → Admin, Technician, Viewer
- `TestStatus` → Pending, InProgress, Completed, Failed

### ✅ Type-Safe Entity References

**DO: Use `nameof()` for Entity Type Serialization**

When storing entity type names (e.g., in sync logs), always use `nameof()` for type safety and refactoring support:

```csharp
// ✅ GOOD: Type-safe with nameof()
await _syncLogService.CreateAsync(new CreateSyncLogDto
{
    EntityType = nameof(Sample), // Refactoring-safe
    EntityId = sample.Id,
    Status = SyncStatus.Synced
}, ct);

// ❌ BAD: Magic string
await _syncLogService.CreateAsync(new CreateSyncLogDto
{
    EntityType = "Sample", // Breaks if class is renamed!
    EntityId = sample.Id,
    Status = SyncStatus.Synced
}, ct);
```

### ✅ Constants & Configuration

**DO: Use Constants from `AppConstants.cs`**

All application-wide constants are centralized in `Quater.Backend.Core.Constants/AppConstants.cs`:

```csharp
// ✅ GOOD: Use centralized constants
public async Task<PagedResult<Sample>> GetPagedAsync(int page, int pageSize, CancellationToken ct)
{
    var validatedPageSize = Math.Min(pageSize, AppConstants.Pagination.MaxPageSize);
    var skip = (page - 1) * validatedPageSize;
    
    // Query logic...
}

// ❌ BAD: Magic numbers
public async Task<PagedResult<Sample>> GetPagedAsync(int page, int pageSize, CancellationToken ct)
{
    var validatedPageSize = Math.Min(pageSize, 100); // Magic number!
    var skip = (page - 1) * validatedPageSize;
    
    // Query logic...
}
```

**Available Constant Classes**:

- `AppConstants` → Pagination, rate limiting, file upload, sync, validation, security
- `ErrorMessages` → All error messages
- `Roles` → Role name constants (Admin, Technician, Viewer)
- `Policies` → Authorization policy names
- `ClaimTypes` → Custom claim type constants

### ✅ Clean Architecture Boundaries

**DO: Respect Layer Dependencies**

Follow the dependency flow: **Core → Services → Data → API**

```
┌─────────────────────────────────────────┐
│  API Layer (Controllers, Middleware)    │
│  - Depends on: Core, Services           │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│  Services Layer (Business Logic)        │
│  - Depends on: Core, Data               │
└─────────────────────────────────────────┘
                  ↓
┌───────────────────────────────┐
│  Data Layer (Repositories, DbContext)   │
│  - Depends on: Core                     │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│  Core Layer (Interfaces, DTOs, Models)  │
│  - Depends on: Nothing (pure)           │
└─────────────────────────────────────────┘
```

**Rules**:
- Core layer has NO dependencies on other layers
- Services implement interfaces defined in Core
- Controllers only call Services, never Repositories directly
- DTOs are defined in Core, not in API layer

### ❌ Anti-Patterns to Avoid

**DON'T: Create Verbose Constant Classes**

```csharp
// ❌ BAD: Overly nested constants
public static class SyncConstants
{
    public static class Status
    {
        public const string Success = "success";
        public const string Failed = "failed";
        public const string InProgress = "in_progress";
    }
}

// Usage is verbose and error-prone
log.Status = SyncConstants.Status.Success;

// ✅ GOOD: Use enum instead
public enum SyncStatus
{
    Synced,
    Failed,
    InProgress,
    Pending
}

// Usage is clean and type-safe
log.Status = SyncStatus.Synced;
```

**DON'T: Modify Files Outside Task Scope**

When working on a specific issue or task, only modify files directly related to that task. Avoid "drive-by refactoring" that makes code review difficult.

**DON'T: Mix Business Logic in Controllers**

```csharp
// ❌ BAD: Business logic in controller
[HttpPost]
public async Task<IActionResult> CreateSample([FromBody] CreateSampleDto dto)
{
    var existing = await _repository.GetByCodeAsync(dto.Code);
    if (existing is not null)
        return Conflict("Sample code already exists");
    
    var sample = new Sample { /* ... */ };
    await _repository.AddAsync(sample);
    return Ok(sample);
}

// ✅ GOOD: Delegate to service
[HttpPost]
public async Task<IActionResult> CreateSample([FromBody] CreateSampleDto dto)
{
    var sample = await _sampleService.CreateAsync(dto, HttpContext.RequestAborted);
    return CreatedAtAction(nameof(GetById), new { id = sample.Id }, sample);
}
```

---

## 🔐 Authentication & Security

### JWT Claims Structure

Our JWT tokens contain 5 claims that are issued by OpenIddict during authentication:

**Standard OIDC Claims (OpenIddictConstants.Claims):**
- **`sub` (Subject)** - User ID (Guid) - **PRIMARY USER IDENTIFIER** ⚠️
- `name` - Username (for display purposes)
- `email` - User email (for display purposes)

**Custom Quater Claims (QuaterClaimTypes):**
- **`identity.quater.app/role`** - User role (Admin/Technician/Viewer) - Used for authorization
- `identity.quater.app/lab_id` - Associated lab ID (for future multi-tenancy)

**Token Destinations:**
- **Access Token**: ALL 5 claims (used for API authorization)
- **Identity Token**: Only Subject, Name, Email (OIDC standard claims)

### Reading Claims Correctly

**✅ ALWAYS use OpenIddict constants for standard claims:**

```csharp
// ✅ CORRECT: Use OpenIddict constants
var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(); // Fail fast - token is invalid
}

// Use the userId for business logic
var user = await _userManager.FindByIdAsync(userId);
```

**❌ NEVER use ClaimTypes or string literals:**

```csharp
// ❌ WRONG: Using legacy ASP.NET Identity claim types
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // WRONG!

// ❌ WRONG: Using string literals
var userId = User.FindFirstValue("sub"); // WRONG!

// ❌ DANGEROUS: Fallback pattern
var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject)
             ?? User.FindFirstValue(ClaimTypes.NameIdentifier); // DANGEROUS!
```

### Fail-Fast for Required Claims

**CRITICAL SECURITY RULE**: Never add fallbacks for required claims. This hides bugs and creates security vulnerabilities.

```csharp
// ❌ DANGEROUS: Defensive fallback (NEVER DO THIS)
var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
if (string.IsNullOrEmptyId))
{
    // Fallback to another claim type
    userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // SECURITY RISK!
}

// ✅ CORRECT: Fail fast (ALWAYS DO THIS)
var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(); // Fail immediately - token is invalid
}
```

**Why fallbacks are dangerous:**

1. **Security Vulnerability**: An attacker could craft a token with the wrong claim type to bypass authentication
2. **Hides Configuration Bugs**: If OpenIddict stops issuing `Subject` claims, the system silently falls bactead of alerting you
3. **Inconsistent Behavior**: Different code paths execute for different users, making debugging impossible
4. **False Sense of Security**: Appears "defensive" but actually creates attack vectors

**Real-world example from our codebase:**

```csharp
// AuthController.cs - UserInfo endpoint
[HttpGet("userinfo")]
[Authorize]
public async Task<IActionResult> UserInfo()
{
    // ✅ CORRECT: Fail fast if Subject claim is missing
    var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
    if (string.IsNullOrEmpty(userId))
    {
        return Unauthorized();
    }

    var user = serManager.FindByIdAsync(userId);
    if (user == null)
    {
        return NotFound(new { error = "User not found" });
    }

    return Ok(new
    {
        id = user.Id,
        email = user.Email,
        userName = user.UserName,
        role = user.Role.ToString(),
        labId = user.LabId,
        isActive = user.IsActive
    });
}
```

### When to Check for Null: The Security Guide

**✅ DO check for null: External Inputs**

```csharp
// User input from request body - ALWAYS validate
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    if (string.IsNullOrEmpty(request.Email))
    {
        return BadRequest("Email is required");
    }
    
    if (string.IsNullOrEmpty(request.Password))
    {
        return BadRequest("Password is required");
    }
    
    // Process registration...
}
```

**✅ DO check for null: Optional Claims**

```csharp
// Optional claim that might not exist in all tokens
var phoneNumber = User.FindFirstValue("phone_number");
if (!string.IsNullOrEmpty(phoneNumber))
{
    // Use phone number if available
    await SendSmsNotificationAsync(phoneNumber);
}
```

**❌ DON'T check for null: Required Claims**

```csharp
// ❌ BAD: Subject is ALWAYS required - don't add fallback
var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
if (string.IsNullOrEmpty(userId))
{
    // Fallback logic here is WRONG - fail fast instead
    return Unauthorized();
}

// ✅ GOOD: Fail fast if required claim is missing
var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(); // Token is invalid
}
```

**❌ DON'T check for null: Internal Invariants**

```csharp
// ❌ BAD: If user doesn't exist after authentication, that's a BUG
var user = await _userManager.FindByIdAsync(userId);
if (user == null)
{
    // Don't hide the bug with fallback logic or default values
    return NotFound(); // Fail fast - this should never happen
}
```

### Authorization with Role Claims

**Use role-based authorization policies (defined in ServiceCollectionExtensions.cs):**

```csharp
// ✅ CORRECT: Use declarative authorization policies
[Authorize(Policy = Policies.AdminOnly)]
public async Task<IActionResult> DeleteLab(Guid id)
{
    await _labService.DeleteAsync(id, HttpContext.RequestAborted);
    return NoContent();
}

[Authorize(Policy = Policies.TechnicianOrAbove)]
public async Task<IActionResult> CreateSample([FromBody] CreateSampleDto dto)
{
    var sample = await _sampleService.CreateAsync(dto, HttpContext.RequestAborted);
    return CreatedAtAction(nameof(GetById), new { id = sample.Id }, sample);
}

[Authorize(Policy = Policies.ViewerOrAbove)]
public async Task<IActionResult> GetSamples([FromQuery] int page = 1)
{
    var samples = await _sampleService.GetPagedAsync(page, HttpContext.RequestAborted);
    return Ok(samples);
}
```

**❌ NEVER check roles manually in controllers:**

```csharp
// ❌ BAD: Manual role check in controller
public async Task<IActionResult> DeleteLab(Guid id)
{
    var role ser.FindFirstValue(QuaterClaimTypes.Role);
    if (role != Roles.Admin)
    {
        return Forbid();
    }
    
    await _labService.DeleteAsync(id, HttpContext.RequestAborted);
    return NoContent();
}

// ✅ GOOD: Use policy attribute
[Authorize(Policy = Policies.AdminOnly)]
public async Task<IActionResult> DeleteLab(Guid id)
{
    await _labService.DeleteAsync(id, HttpContext.RequestAborted);
    return NoContent();
}
```

### Logging Security Events

**Always log security-related events for monitoring and auditing:**

```csharp
// Authentication failures
_logger.LogWarning(
    "Authentication failed for us}: {Reason}",
    request.Email,
    "Invalid credentials");

// Authorization failures (handled by ASP.NET Core automatically, but you can add custom logging)
_logger.LogWarning(
    "Authorization failed: User {UserId} with role {Role} attempted to access {Resource}",
    userId,
    userRole,
    resourceName);

// Token validation errors
_logger.LogWarning(
    "Token validation failed: {Error}",
    validationError);

// Suspicious activity
_logger.LogWarning(
    "Rate limit exceeded for user {UserId} on endpoint {Endpoint}",
    userId,
    endpoint);
```

**DON'T log sensitive data``csharp
// ❌ BAD: Logging passwords or tokens
_logger.LogInformation("User login attempt: {Email} with password {Password}", email, password);
_logger.LogInformation("Token issued: {Token}", accessToken);

// ✅ GOOD: Log events without sensitive data
_logger.LogInformation("User {Email} authenticated successfully", email);
_logger.LogInformation("Token issued for user {UserId}", userId);
```

---

## 🛡️ Security Best Practices Summary

### Authentication DO's and DON'Ts

**DO:**
✅ Use `OpenIddictConstants.Claims.Subject` for user ID  
✅ Fail fast when required claims are missing  
✅ Use declarative authorization policies (`[Authorize(Policy = ...)]`)  
✅ Log authentication and authorization failures  
✅ Validate all external inputs  
✅ Return generic error messages to prevent information disclosure  

**DON'T:**
❌ Use `ClaimTypes.NameIdentifier` (legacy ASP.NET Identity)  
❌ Add fallbacks for required claims (hides bugs, creates vulnerabilities)  
❌ Check roles manually in controllers (use policies)  
❌ Log sensitive data (passwords, tokens, PII)  
❌ Expose detailed error messages to clients  
❌ Trust client-provided data without validation  

### Common Security Pitfalls

1. **Defensive Fallbacks**: Adding "just in case" fallbacks for required claims creates security vulnerabilities
2. **Mixed Claim Types**: Using both `Subject` and `NameIdentifier` causes inconsistent behavior
3. **Manual Authorization**: Checking roles in controller logic instead of using policies
4. **Information Disclosure**: Returning detailed error messages that reveal system internals
5. **Missing Validation**: Trusting external inputs without proper validation

---

### 📋 Code Review Checklist

Before submitting code, verify:

**General:**
- [ ] No generic exceptions (`InvalidOperationException`, `ArgumentException`) for business logic
- [ ] No magic strings for status/type values (use enums)
- [ ] No hardcoded error messages (use `ErrorMessages.cs`)
- [ ] Entity type references use `nameof()` not string literals
- [ ] Constants used from `AppConstants.cs` where applicable
- [ ] Clean Architecture boundaries respected
- [ ] All async methods accept `CancellationToken`
- [ ] Nullable reference types properly annotated
- [ ] Build succeeds with 0 errors, 0 warnings

**Authentication & Security:**
- [ ] Claims read using `OpenIddictConstants.Claims.Subject` (not `ClaimTypes.NameIdentifier`)
- [ ] No defensive fallbacks for required claims (Subject, Role)
- [ ] Authentication failures return `Unauthorized()` immediately (fail fast)
- [ ] Authorization uses policies (`[Authorize(Policy = ...)]`), not manual role checks
- [ ] Security events logged appropriately (auth failures, suspicious activity)
- [ ] No sensitive data in logs (passwords, tokens, PII)
- [ ] External inputs validated before use
- [ ] Error messages don't expose system internals

---

## 🚦 Rate Limiting Best Practices

### Current Configuration

**Production Defaults** (as of latest update):
- **Authenticated users**: 60 requests per minute (1 req/second)
- **Anonymous users**: 10 requests per minute (0.17 req/second)
- **Window**: 60 seconds (sliding window)

**Development Defaults**:
- **Authenticated users**: 100 requests per minute (for testing)
- **Anonymous users**: 20 requests per minute (for testing)

### When to Adjust Rate Limits

**Increase limits if:**
- ✅ Users frequently hit rate limits during normal usage
- ✅ Mobile apps need to sync large datasets
- ✅ Bulk operations are common (e.g., importing samples)
- ✅ Real-time features require frequent polling
- ✅ Load testing shows limits are too restrictive

**Decrease limits if:**
- ⚠️ Experiencing abuse or DDoS attacks
- ⚠️ Server resources are constrained
- ⚠️ Cost optimization is needed (reduce Redis/API load)

### Monitoring Rate Limiting

**Key Metrics to Track:**
1. **Rate limit hit rate**: % of requests that exceed limits
2. **429 response count**: Number of rate limit rejections
3. **Redis performance**: Latency and throughput
4. **User complaints**: Feedback about rate limiting

**Recommended Monitoring**:
```csharp
// Log rate limit exceeded events
_logger.LogWarning(
    "Rate limit exceeded for user {UserId} on endpoint {Endpoint}. Count: {Count}/{Max}",
    userId,
    endpoint,
    currentCount,
    limit);
```

### Per-Endpoint Rate Limiting

Use `[EndpointRateLimit]` attribute for sensitive endpoints:

```csharp
// Example: Limit password reset requests
[HttpPost("forgot-password")]
[EndpointRateLimit(requests: 3, windowMinutes: 60, trackBy: RateLimitTrackBy.Email)]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    // Implementation
}

// Example: Limit bulk operations
[HttpPost("bulk-import")]
[Authorize(Policy = Policies.TechnicianOrAbove)]
[EndpointRateLimit(requests: 5, windowMinutes: 60, trackBy: RateLimitTrackBy.UserId)]
public async Task<IActionResult> BulkImport([FromBody] BulkImportRequest request)
{
    // Implementation
}
```

### Rate Limiting Strategy by Deployment Size

| Deployment Size | Authenticated | Anonymous | Notes |
|----------------|---------------|-----------|--
| **Small** (< 50 users) | 60/min | 10/min | Current defaults |
| **Medium** (50-500 users) | 100/min | 20/min | Increase for active usage |
| **Large** (500+ users) | 200/min | 30/min | Scale with user base |
| **Enterprise** (1000+ users) | 300/min | 50/min | Consider tiered limits |

### Redis Configuration for Rate Limiting

**Single Instance** (current setup):
```yaml
redis:
  image: redis:7-alpine
  command: redis-server --appendonly yes --requirepass ${REDIS_PASSWORD}
  volumes:
    - redis_data:/data
```

**Persistence Options**:
- **RDB (Snapshot)**: Point-in-time backups, looverhead
- **AOF (Append-Only File)**: More durable, logs every write (current setup)

**When Redis is Down**:
- Middleware **fails open** (allows requests)
- Logged as error for monitoring
- Prevents Redis outages from taking down API

### Configuration via Environment Variables

Override rate limits without code changes:

```bash
# In .env or docker-compose.yml
RATELIMITING_AUTHENTICATED_LIMIT=100
RATELIMITING_ANONYMOUS_LIMIT=20
RATELIMITING_WINDOW_SECONDS=60
```

### Security Considerations

1. **IP-based rate limiting** (anonymous users):
   - Can be bypassed with VPNs/proxies
   - Use for basic protection only
   - Consider additional security measures (CAPTCHA, etc.)

2. **User-based rate limiting** (authenticated users):
   - More accurate tracking
   - Prevents abuse from compromised accounts
   - Combine with account lockout policies

3. **Endpoint-specific limits**:
   - Protect sensitive operations (password reset, email verification)
   - Prevent brute force attacks
   - Track by email for pre-authentication endpoints

### Troubleshooting Rate Limiting

**Issue**: Users hitting rate limits during normal usage
- **Solution**: Increase limits or optimize client-side caching

**Issue**: Redis connection errors
- **Solution**: Check Redis health, increase connection timeout, verify network

**Issue**: Rate limiting not working
- **Solution**: Verify Redis is running, check middleware order in Program.cs

**Issue**: Different limits for different user tiers
- **Solution**: Implement custom rate limiting logic based on user role/subscription

---

## 🔐 OpenIddict Certificate Management

### Overview

OpenIddict requires two X.509 certificates for production:
1. **Encryption Certificate**: Encrypts access tokens (confidentiality)
2. **Signing Certificate**: Signs tokens (integrity and authenticity)

**Security Model**:
- Tokens are **signed** (tamper-proof) AND **encrypted** (confidential)
- Even if intercepted, tokens cannot be read or modified
- Separate certificates provide security isolation

### Certificate Generation

#### Development (Self-Signed Certificates)

Use the provided script to generate self-signed certificates:

```bash
cd backend
./scripts/generate-openiddict-certs.sh development
```

**Output**:
- `certs/encryption.pfx` (no password)
- `certs/signing.pfx` (no password)

**Configuration** (appsettings.Development.json):
```json
{
  "OpenIddict": {
    "EncryptionCertificatePath": "/path/to/certs/encryption.pfx",
    "SigningCertificatePath": "/path/to/certs/signing.pfx",
    "EncryptionCertificatePassword": "",
    "SigningCertificatePassword": ""
  }
}
```

#### Production (CA-Signed Certificates)

**Step 1: Generate Certificate Signing Requests (CSRs)**

```bash
cd backend
./scripts/generate-openiddict-certs.sh production
```

**Output**:
- `certs/encryption.csr` - Submit to your CA
- `certs/signing.csr` - Submit to your CA
- `certs/encryption-key.pem` - Keep secure!
- `certs/signing-key.pem` - Keep secure!

**Step 2: Submit CSRs to Certificate Authority**

Submit the CSR files to your CA (e.g., Let's Encrypt, DigiCert, internal CA).

**Step 3: Create PFX Files**

Once you receive signed certificates from your CA:

```bash
# Create encryption PFX with password
openssl pkcs12 -export -out encryption.pfx \
  -inkey certs/encryption-key.pem \
  -in encryption-cert.pem \
  -passout pass:YOUR_STRONG_PASSWORD

# Create signing PFX with password
openssl pkcs12 -export -out signing.pfx \inkey certs/signing-key.pem \
  -in signing-cert.pem \
  -passout pass:YOUR_STRONG_PASSWORD
```

**Step 4: Store in Infisical**

```bash
# Store certificate paths
infisical secrets set OPENIDDICT_ENCRYPTION_CERT_PATH /app/certs/encryption.pfx
infisical secrets set OPENIDDICT_SIGNING_CERT_PATH /app/certs/signing.pfx

# Store passwords securely
infisical secrets set OPENIDDICT_ENCRYPTION_CERT_PASSWORD YOUR_STRONG_PASSWORD
infisical secrets set OPENIDDICT_SIGNING_CERT_PASSWORD YOUR_STRONG_PASSWORD
```

### Docker Deployment

**Mount certificates as read-only volume

```yaml
services:
  backend:
    image: quater-backend:latest
    volumes:
      - ./certs:/app/certs:ro  # Read-only mount
    environment:
      - OPENIDDICT_ENCRYPTION_CERT_PATH=/app/certs/encryption.pfx
      - OPENIDDICT_SIGNING_CERT_PATH=/app/certs/signing.pfx
      - OPENIDDICT_ENCRYPTION_CERT_PASSWORD=${OPENIDDICT_ENCRYPTION_CERT_PASSWORD}
      - OPENIDDICT_SIGNING_CERT_PASSWORD=${OPENIDDICT_SIGNING_CERT_PASSWORD}
```

**With Infisical injection**:

```bash
# Run with Infisical
infisical run --env=production -- docker-compose up -d
```

### Certificate Rotation

**When to rotate certificates**:
- ✅ Before expiration (recommended: 30 days before)
- ✅ If private key is compromised
- ✅ As part of regular security maintenance (annually)
- ✅ When changing certificate authority

**Rotation procedure**:

1. **Generate new certificates** (keep old ones active)
2. **Add new certificates** to OpenIddict configuration
3. **Deploy updated configuration** (zero-downtime)
4. **Wait for old tokens to expire** (default: 1 hour for access tokens)
5. **Remove old certificates** from configuration

**Zero-downtime rotation**:
```csharp
// OpenIddict supports multiple certificates simultaneously
options.AddEncryptionCertificate(oldEncryptionCert)
       .AddEncryptionCertificate(newEncryptionCert)  // Add new cert
       .AddSigningCertificate(oldSigningCert)
       .AddSigningCertificate(newSigningCert);       // Add new cert
```

### Security Best Practices

**DO**:
- ✅ Use separate certificates for encryption and signing
- ✅ Store certificates in secure secrets manager (Infisical, Azure Key Vault, AWS Secrets Manager)
- ✅ Use strong passwords for PFX files (min 16 characters)
- ✅ Mount certificates as read-only in Docker
- ✅ Rotate certificates before expiration
- ✅ Keep private keys secure and never commit to version control
- ✅ Use CA-signed certificates in production
- ✅ Monitor certificate expiration dates

**DON'T**:
- ❌ Use self-signed certificates in production
- ❌ Commit certificates or private keys to version control
- ❌ Share certificates between environments
- ❌ Use the same certificate for encryption and signing
- ❌ Store certificates in application code
- ❌ Use weak or no passwords for PFX files in production
- ❌ Ignore certificate expiration warnings

### Troubleshooting

**Issue**: "Certificate not found" error on startup
- **Solution**: Verify certificate paths in environment variables
- **Solution**: Check file permissions (container user must have read access)
- **Solution**: Ensure certificates are mounted correctly in Docker

**Issue**: "Invalid password" error
- **Solution**: Verify password in environment variables matches PFX password
- **Solution**: Check for special characters that need escaping in shell

**Issue**: "Certificate has expired"
- **Solution**: Generate new certificates and rotate immediately
- **Solution**: Set up monitoring for certificate expiration

**Issue**: Tokens cannot be decrypted after deployment
- **Solution**: Ensure encryption certificate hasn't changed
- **Solution**: Keep old certificate active during rotation period

### Certificate Validation

**Verify certificate details**:

```bash
# View certificate information
openssl pkcs12 -in encryption.pfx -nokeys -info

# Check certificate expiration
openssl pkcs12 -in encryption.pfx -nokeys | openssl x509 -noout -dates

# Verify certificate chain
openssl pkcs12 -in encryption.pfx -nokeys | openssl x509 -noout -text
```

### Monitoring

**Add certificate expiration monitoring**:

```csharp
// Log certificate expiration on startup
var encryptionCert = X509CertificateLoader.LoadPkcs12FromFile(encryptionCertPath, password);
var daysUntilExpiration = (encryptionCert.NotAfter - DateTime.UtcNow).TotalDays;

if (daysUntilExpiration < 30)
{
    _logger.LogWarning(
        "Encryption certificate expires in {Days} days on {ExpirationDate}. Rotation recommended.",
        (int)daysUntilExpiration,
        encryptionCert.NotAfter);
}
```

---

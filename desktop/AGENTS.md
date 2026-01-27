# Desktop Agent Instructions - Quater Water Quality Lab Management System

## Build & Test Commands

### Desktop (Avalonia)

```bash
# Build
dotnet build desktop/Quater.Desktop.sln

# Run tests
dotnet test desktop/tests/Quater.Desktop.Tests/
dotnet test desktop/tests/Quater.Desktop.Data.Tests/

# Run single test
dotnet test --filter "FullyQualifiedName=Quater.Desktop.Tests.ViewModels.SampleViewModelTests.CreateSample_ValidData_Success"

# Run desktop app
cd desktop/src/Quater.Desktop
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

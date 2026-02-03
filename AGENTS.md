# Agent Instructions - Quater Water Quality Lab Management System

## Project Overview

**Quater** is an open-source, cross-platform water quality lab management system:
- **Backend API**: ASP.NET Core 10.0 + PostgreSQL (C# 13 / .NET 10)
- **Desktop App**: Avalonia UI 11.x (Windows/Linux/macOS)
- **Mobile App**: React Native 0.73+ (Android, field sample collection only)

**Architecture**: Offline-first with bidirectional sync, Last-Write-Wins conflict resolution.

---

## Build, Test & Lint Commands

### Backend (.NET 10.0)

```bash
# Build
dotnet build                                    # Build entire solution
dotnet build backend/src/Quater.Backend.Api     # Build specific project

# Test
dotnet test                                     # Run all tests
dotnet test --filter "FullyQualifiedName~LabService"  # Run tests matching pattern
dotnet test --filter "FullyQualifiedName=Quater.Backend.Core.Tests.Services.LabServiceIntegrationTests.CreateAsync_ValidLab_CreatesLab"  # Run single test

# Run API
dotnet run --project backend/src/Quater.Backend.Api

# Database Migrations
dotnet ef migrations add MigrationName --project backend/src/Quater.Backend.Data
dotnet ef database update --project backend/src/Quater.Backend.Data
```

**Note**: Tests use Testcontainers with PostgreSQL. Docker must be running.

---

## Monorepo Structure

- `backend/src/Quater.Backend.Api` - Web API controllers & endpoints
- `backend/src/Quater.Backend.Core` - Interfaces, DTOs, exceptions, validators
- `backend/src/Quater.Backend.Data` - EF Core DbContext, configurations, interceptors
- `backend/src/Quater.Backend.Services` - Business logic services
- `backend/tests/Quater.Backend.Core.Tests` - xUnit integration tests
- `shared/` - Shared models, enums, interfaces, value objects (used by all apps)
- `desktop/` - Avalonia UI Desktop Application
- `mobile/` - React Native Mobile Application
- `specs/` - Feature specifications (Speckit)

---

## Code Style Guidelines (C# Backend)

### Imports & Naming
- **Import order**: System → Third-party → Project namespaces
- **Classes/Interfaces**: PascalCase (`LabService`, `ILabService`)
- **Methods**: PascalCase with `Async` suffix (`GetByIdAsync`, `CreateAsync`)
- **Parameters/Variables**: camelCase (`userId`, `pageNumber`)
- **Private fields**: `_camelCase` with underscore (`_context`, `_timeProvider`)

### Types & Nullability
- **Nullable reference types enabled**: Use `?` for nullable types (`string?`, `LabDto?`)
- Use `= null!` for DI fields initialized in constructor
- Use `string.Empty` instead of `""`
- Use collection expressions: `[]` instead of `new List<T>()`

### Method Signatures
- Always include `CancellationToken ct = default` for async methods
- Use **primary constructors** (C# 13):
  ```csharp
  public class LabService(QuaterDbContext context) : ILabService
  {
      // No explicit constructor needed
  }
  ```

### Error Handling
Use custom exceptions from `Quater.Backend.Core.Exceptions`:
- `NotFoundException` - Resource not found (404)
- `ConflictException` - Duplicate/conflict (409)
- `BadRequestException` - Invalid input (400)
- `ForbiddenException` - Access denied (403)

Example: `throw new ConflictException(ErrorMessages.LabAlreadyExists);`

### Entity Framework Patterns
- Use `AsNoTracking()` for read-only queries
- Use `FindAsync()` for single entity by primary key
- Use `FirstDefaultAsync()` for queries with conditions
- Always filter soft-deleted: `.Where(l => !l.IsDeleted)`
- Use `IgnoreQueryFilters()` to include soft-deleted entities
- **CRITICAL**: Audit fields (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy) are managed by `AuditInterceptor` - NEVER set manually
- **CRITICAL**: IsDeleted is managed by `SoftDeleteInterceptor` - NEVER set manually

### DTOs & Mapping
- Keep DTOs in `Quater.Backend.Core/DTOs/`
- Use private static methods for mapping: `private static LabDto MapToDto(Lab lab)`
- DTOs use Data Annotations: `[Requd]`, `[MaxLength(200)]`

### Testing Patterns
- Use **xUnit** with **FluentAssertions**
- Integration tests use **Testcontainers** with PostgreSQL
- Test naming: `MethodName_Scenario_ExpectedResult`
- Use `[Collection("PostgreSQL")]` for database tests
- Implement `IAsyncLifetime` for setup/teardown
- Example:
  ```csharp
  [Fact]
  public async Task CreateAsync_ValidLab_CreatesLab()
  {
      // Arrange
      var dto = new CreateLabDto { Name = "Test Lab" };
      
      // Act
      var result = await _service.CreateAsync(dto, "user-id");
      
      // Assert
      result.Should().NotBeNull();
      result.Name.Sh().Be("Test Lab");
  }
  ```

### Documentation
- Use XML comments for public APIs: `<summary>`, `<param>`, `<returns>`, `<exception>`
- Document complex business logic with inline comments

---

## Architecture Patterns

### Shared Models (Core Domain Pattern)
- Domain models in `shared/Quater.Shared` - shared across backend, desktop, mobile
- Models implement: `IEntity`, `IAuditable`, `ISoftDelete`, `IConcurrent`
- Audit properties are `private set` - managed by interceptors only

### EF Core Interceptors (Automatic Cross-Cutting Concerns)
1. **AuditInterceptor** - Sets CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
2. **SoftDeleteInterceptor** - Converts DELETE to UPDATE with IsDeleted=true
3. **AuditTrailInterceptor** - Creates AuditLog entries for all changes

**IMPORTANT**: Never manually set audit fields or IsDeleted - interceptors handle this automatically.

### Service Layer
- Services in `backend/src/Quater.Backend.Services/`
- Interfaces in `backend/src/Quater.Backend.Core/Interfaces/`
- Use primary constructors (C# 13)
- Receive `QuaterDbContext` via DI
- Return DTOs, never domain models directly

### Value Objects
- Immutable records in `shared/ValueObjects/`
- Example: ent` validates parameter/unit combinations
- Use `record` keyword for immutability

---

## Key Architecture Decisions

1. **Authentication**: ASP.NET Core Identity + OpenIddict (OAuth2/OIDC)
2. **Conflict Resolution**: Last-Write-Wins with automatic backup
3. **API Versioning**: `/api/v1/` prefix for all endpoints
4. **Audit Archival**: 90-day hot/cold split with nightly background job

See `specs/001-water-quality-platform/ARCHITECTURE_DECISIONS.md` for details.

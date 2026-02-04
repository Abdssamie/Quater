# Agent Instructions - Quater Water Quality Lab Management System

## Project Overview

**Quater** is an open-source, cross-platform water quality lab management system:
- **Backend API**: ASP.NET Core 10.0 + PostgreSQL (C# 13 / .NET 10)
- **Desktop App**: Avalonia UI 11.x (Windows/Linux/macOS) - Offline-first with SQLite
- **Mobile App**: React Native 0.73+ (Android, field sample collection only)

**Architecture**: Offline-first with optimistic concurrency control (RowVersion), client-side conflict resolution.

---

## Build, Test & Run Commands

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
# API runs on https://localhost:5001 with Swagger UI at /swagger

# Database Migrations
dotnet ef migrations add MigrationName --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api
dotnet ef database update --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api
```

**Note**: Tests use Testcontainers with PostgreSQL. Docker must be running.

---

## Monorepo Structure

- `backend/src/Quater.Backend.Api` - Web API controllers, middleware, Program.cs
- `backend/src/Quater.Backend.Core` - Interfaces, DTOs, exceptions, validators, extensions
- `backend/src/Quater.Backend.Data` - EF Core DbContext, configurations, interceptors, migrations
- `backend/src/Quater.Backend.Services` - Business logic services
- `backend/src/Quater.Backend.Infrastructure.Email` - Email queue and templates
- `backend/tests/Quater.Backend.Core.Tests` - xUnit integration tests with Testcontainers
- `shared/` - Shared models, enums, interfaces, value objects (used by all apps)
- `desktop/` - Avalonia UI Desktop Application
- `mobile/` - React Native Mobile Application

---

## Code Style Guidelines (C# Backend)

### Imports & Naming
- **Import order**: System → Third-party → Project namespaces
- **Classes/Interfaces**: PascalCase (`LabService`, `ILabService`)
- **Methods**: PascalCase with `Async` suffix (`GetByIdAsync`, `CreateAsync`)
- **Parameters/Variables**: camelCase (`userId`, `pageNumber`)
- **Private fields**: `_camelCase` with underscore (`_context`, `_logger`)

### Types & Nullability
- **Nullable reference types enabled**: Use `?` for nullable types (`string?`, `LabDto?`)
- Use `= null!` for DI fields initialized in constructor
- Use `string.Empty` instead of `""`
- Use collection expressions: `[]` instead of `new List<T>()`
- All entity IDs are `Guid` (including User IDs)

### Method Signatures
- Always include `CancellationToken ct = default` for async methods
- Use **primary constructors** (C# 12):
  ```csharp
  public class LabQuaterDbContext context, ILogger<LabService> logger) : ILabService
  {
      // No explicit constructor needed - parameters become fields
  }
  ```

### Controllers
- Use `[ApiController]` and `[Route("api/[controller]")]`
- Use primary constructors for DI
- Add XML documentation comments (`<summary>`)
- Use `[ProducesResponseType]` for all responses
- Authorization: `[Authorize(Policy = Policies.ViewerOrAbove)]` at class level, override with `[Authorize(Policy = Policies.AdminOnly)]` for write operations
- Return `ActionResult<T>` for typed responses
- Use `User.GetUserIdOrThrow()` to get current user ID (returns `Guid`)
- **Endpoint Naming (Option B Pattern)**: Use descriptive prefixes for filtering endpoints (e.g., `/by-lab/{labId}`, `/by-sample/{sampleId}`, `/by-entity/{entityId}`, `/by-user/{userId}`) - self-documenting and no confusion with ID parameters

### Error Handling
Use custom exceptions from `Quater.Backend.Core.Exceptions`:
- `NotFoundException` - Resource not found (404)
- `ConflictException` - Duplicate/conflict (409)
- `BadRequestException` - Invalid input (400)
- `ForbiddenException` - Access denied (403)

Example: `throw new NotFoundException(ErrorMessages.LabNotFound);`

**Global Exception Handler**: `GlobalExceptionHandlerMiddleware` catches all unhandled exceptions and returns consistent JSON error responses with TraceId and Timestamp.

### Entity Framework Patterns
- Use `AsNoTracking()` for read-only queries
- Use `FindAsync()` foingle entity by primary key
- Use `FirstOrDefaultAsync()` for queries with conditions
- Always filter soft-deleted: `.Where(l => !l.IsDeleted)` (or rely on global query filters)
- Use `IgnoreQueryFilters()` to include soft-deleted entities
- **CRITICAL**: Audit fields (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy) are managed by `AuditInterceptor` - NEVER set manually
- **CRITICAL**: IsDeleted/DeletedAt are managed by `SoftDeleteInterceptor` - Use `context.Remove()` for soft delete, interceptor converts to UPDATE
- **Concurrency**: All entities have `RowVersion` (byte[]) for optimistic concurrency - EF Core handles thisomatically

### DTOs & Mapping
- Keep DTOs in `Quater.Backend.Core/DTOs/`
- Use private static methods for mapping: `private static LabDto MapToDto(Lab lab)`
- DTOs use Data Annotations: `[Required]`, `[MaxLength(200)]`, `[EmailAddress]`
- Return DTOs from services, never domain models directly

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
      var dto = new CreateLabDto { Name = "Test Lab", Location = "Test Location" };
      
      // Act
      var result = await _service.CreateAsync(dto, Guid.NewGuid());
      
      // Assert
      result.Should().NotBeNull();
      result.Name.Should().Be("Test Lab");
  }
  ```

### Documentation
- Use XML comments for public APIs: `<summary>`, `<param>`, `<returns>`, `<exception>`
- Document complex business logic with inline comments
- Controllers should have XML comments for Swagger documentation

---

## Architecture Patterns

### Shared Models (Core Domain Pattern)
- Domain models in `shared/Quater.Shared` - shared across backend, desktop, mobile
- Models implement: `IEntity`, `IAuditable`, `ISoftDelete`, `IConcurrent`
- Audit properties are `private set` - managed by interceptors only
- User model extends `IdentityUser<Guid>` for ASP.NET Core Identity

### EF Core Interceptors (Automatic Cross-Cutting Concerns)
1. **AuditInterceptor** - Sets CreatedAt, CreatedBy, UpdatedAt, UpdatedBy automatically
2. **SoftDeleteInterceptor** - Converts DELETE to UPDATE with IsDeleted=true, DeletedAt=DateTime.UtcNow
3. **AuditTrailInterceptoCreates AuditLog entries for all changes

**IMPORTANT**: Never manually set audit fields or IsDeleted - interceptors handle this automatically via reflection.

### Service Layer
- Services in `backend/src/Quater.Backend.Services/`
- Interfaces in `backend/src/Quater.Backend.Core/Interfaces/`
- Use primary constructors (C# 12)
- Receive `QuaterDbContext` via DI
- Return DTOs, never domain models directly
- Services receive `Guid userId` parameter for audit tracking

### Value Objects
- Immutable records in `shared/ValueObjects/`
- Example: `Measurement` validates parameter/unit combinations and value ranges
- Example: `Location` encapsulates latitude/longitude with validation
- Use `record` keyword for immutability

### Authorization
- **Policies**: `AdminOnly`, `TechnicianOrAbove`, `ViewerOrAbove`
- **Roles**: `Admin`, `Technician`, `Viewer`
- Apply at controller level with overrides on specific actions
- Use `User.GetUserIdOrThrow()` extension method to get current user ID

---

## Key Implementation Details

### API Endpoints
- Base route: `/api/[controller]` (e.g., `/api/labs`, `/api/users`)
- Pagination: `pageNumber` (default 1), `pageSize` (default 50, max 100)
- **Filtering endpoints use Option B pattern**: `/api/samples/by-lab/{labId}`, `/api/testresults/by-sample/{sampleId}`, `/api/auditlogs/by-entity/{entityId}`, `/api/auditlogs/by-user/{userId}` (NOT `/api/samples/lab/{labId}`)
- Legacy endpoints (without `/by-` prefix) are marked `[Obsolete]` and redirect to new endpoints
- All IDs are `Guid` type

### Authentication & Authorization
- ASP.NET Core Identity + OpenIddict (OAuth2/OIDC)
- Token endpoint: `POST /api/auth/token` (OAuth2 password grant)
- Change password: `POST /api/auth/change-password` (authenticated users only)
- User management: `/api/users` (admin only for CRUD, viewers can read)

### Soft Delete
- Use `context.Remove(entity)` - interceptor converts to soft delete
- Soft-deleted entities have `IsDeleted=true` and `DeletedAt` timestamp
- Global query filters automatically exclude soft-deleted entities
- Use `IgnoreQueryFilters()` to include them

### Concurrency Control
- All entities have `RowVersion` (byte[]) property
- EF Core automatically handles optimistic concurrency
- On conflict, `DbUpdateConcurrencyException` is thrown
- Global exception handler returns 409 Conflict with user-friendly message

---

## Common Patterns & Best Practices

1. **Controllers**: Primary constructor DI, XML docs, ProducesResponseType, proper authorization
2. **Services**: Primary constructor DI, return DTOs, use custom exceptions
3. **Queries**: AsNoTracking for reads, filter soft-deleted, use pagination
4. **Mutations**: Let interceptors handle audit fields, use context.Remove() for delete
5. **Testing**: Use Testcontainers, FluentAssertions, descriptive test names
6. **Error Handling**: Use custom exceptions, let global handler format responses
7. **Validation**: FluentValidation for complex rules, DataAnnotations for simple rules

---

## Important Notes

- **Docker required** for running tests (Testcontainers)
- **PostgreSQL** is the production database
- **SQLite** is used for desktop offline storage
- **Swagger UI** available at `/swagger` when running API
- **All tests must pass** before committing (currently 184/184 passing)
- **No manual audit field manipulation** - interceptors handle this
- **User IDs are Guid** - migrated from string to Guid for consistency

# Quater Backend Implementation Plan - COMPREHENSIVE

**Date**: 2026-01-29  
**Project**: Water Quality Lab Management System  
**Branch**: `001-water-quality-platform`  
**Tech Stack**: ASP.NET Core 10.0, Entity Framework Core 10.0, PostgreSQL 15+, OpenIddict, ASP.NET Core Identity

---

## Executive Summary

This document provides an **extensive, detailed plan** for implementing the complete Quater backend API. It covers ALL features required for the MVP, including:

- Authentication & Authorization (ASP.NET Core Identity + OpenIddict)
- Rate Limiting & Security Hardening
- Complete CRUD APIs for all entities
- Bulk Import/Export (CSV/Excel)
- Bidirectional Sync Engine with Conflict Resolution
- PDF Report Generation (QuestPDF)
- Background Jobs (Audit Log Archival, etc.)
- Comprehensive Error Handling, Logging, Validation
- API Versioning & Pagination
- Soft Delete Pattern
- Testing Infrastructure

**User Decisions Incorporated**:
- ✅ Rate limiting with account lockout (5 failed attempts = 15 min lockout)
- ✅ Bulk import/export via CSV/Excel
- ✅ Separate ConflictBackup table for conflict resolution backups
- ✅ Manual sync/refresh for now (pattern designed for easy SignalR addition later)
- ✅ Synchronous PDF generation (blocking)
- ✅ Soft delete for all entities (IsDeleted flag)

---

## Table of Contents

1. [Project Architecture](#1-project-architecture)
2. [Database Layer](#2-database-layer)
3. [Core Domain Models](#3-core-domain-models)
4. [Authentication & Authorization](#4-authentication--authorization)
5. [API Controllers & Endpoints](#5-api-controllers--endpoints)
6. [Sync Engine](#6-sync-engine)
7. [Bulk Operations](#7-bulk-operations)
8. [Report Generation](#8-report-generation)
9. [Background Jobs](#9-background-jobs)
10. [Middleware & Cross-Cutting Concerns](#10-middleware--cross-cutting-concerns)
11. [Testing Strategy](#11-testing-strategy)
12. [Configuration & Deployment](#12-configuration--deployment)
13. [Performance Optimization](#13-performance-optimization)
14. [Security Hardening](#14-security-hardening)

---

## 1. Project Architecture

### 1.1 Solution Structure

```
backend/
├── src/
│   ├── Quater.Backend.Api/              # ASP.NET Core Web API project
│   │   ├── Controllers/                 # API endpoints
│   │   ├── Middleware/                  # Custom middleware
│   │   ├── Jobs/                        # Background jobs (Quartz.NET)
│   │   ├── Models/                      # DTOs and request/response models
│   │   ├── Services/                    # Application services
│   │   ├── Filters/                     # Action filters, validation filters
│   │   ├── Extensions/                  # Extension methods
│   │   ├── Program.cs                   # Application entry point
│   │   └── appsettings.json             # Configuration
│   │
│   ├── Quater.Backend.Core/             # Domain layer (business logic)
│   │   ├── Models/                      # Domain entities
│   │   ├── Enums/                       # Enumerations
│   │   ├── Interfaces/                  # Service contracts
│   │   ├── Services/                    # Business logic services
│   │   ├── Validators/                  # FluentValidation validators
│   │   └── Exceptions/                  # Custom exceptions
│   │
│   ├── Quater.Backend.Data/             # Data access layer
│   │   ├── Context/                     # DbContext
│   │   ├── Repositories/                # Repository implementations
│   │   ├── Configurations/              # EF Core entity configurations
│   │   ├── Migrations/                  # Database migrations
│   │   ├── Seeders/                     # Data seeders
│   │   └── Interceptors/                # EF Core interceptors (soft delete, audit)
│   │
│   └── Quater.Backend.Sync/             # Sync engine
│       ├── Services/                    # Sync service implementations
│       ├── Models/                      # Sync DTOs
│       ├── Resolvers/                   # Conflict resolution logic
│       └── Strategies/                  # Last-Write-Wins strategy
│
└── tests/
    ├── Quater.Backend.Api.Tests/        # API integration tests
    ├── Quater.Backend.Core.Tests/       # Business logic unit tests
    ├── Quater.Backend.Data.Tests/       # Data layer tests
    └── Quater.Backend.Sync.Tests/       # Sync engine tests
```

### 1.2 Design Patterns

- **Repository Pattern**: Abstract data access
- **Unit of Work**: Transaction management
- **Dependency Injection**: Constructor injection for all dependencies
- **Strategy Pattern**: Conflict resolution strategies
- **Factory Pattern**: Entity creation with validation
- **Decorator Pattern**: Logging, caching, validation
- **CQRS-lite**: Separate read/write operations where beneficial
- **Result Pattern**: Explicit success/failure handling (avoid exceptions for business logic)

### 1.3 Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| **API Versioning** | Use `/api/v1/` prefix for all endpoints to allow future breaking changes |
| **Soft Delete** | All entities use `IsDeleted` flag with global query filters |
| **Optimistic Locking** | Use `Version` field + `RowVersion` for concurrency control |
| **Audit Trail** | Track all changes in `AuditLog` with 90-day hot/cold archival |
| **Conflict Resolution** | Last-Write-Wins with automatic backup in `ConflictBackup` table |
| **Rate Limiting** | Token bucket algorithm: 100 req/min for authenticated, 20 req/min for anonymous |
| **Pagination** | Default 100 records per page, max 1000 |
| **Logging** | Serilog with structured logging to console, file, and PostgreSQL |

---

## 2. Database Layer

### 2.1 DbContext Configuration

**File**: `backend/src/Quater.Backend.Data/Context/QuaterDbContext.cs`

**Features**:
- Soft delete global query filter on all entities
- Audit trail interception (track CreatedBy, ModifiedBy, CreatedDate, ModifiedDate)
- Optimistic concurrency with `RowVersion`
- PostgreSQL-specific configurations (JSONB for audit fields, full-text search)
- Seeding default data (Parameters, Admin user)

**Implementation**:
```csharp
public class QuaterDbContext : DbContext
{
    public DbSet<Sample> Samples { get; set; }
    public DbSet<TestResult> TestResults { get; set; }
    public DbSet<Parameter> Parameters { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Lab> Labs { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AuditLogArchive> AuditLogArchive { get; set; }
    public DbSet<ConflictBackup> ConflictBackups { get; set; }  // NEW

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from separate files
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuaterDbContext).Assembly);

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)),
                    Expression.Constant(false));
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
            }
        }
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        // Soft delete interception
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>().Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.LastModified = DateTime.UtcNow;
        }

        // Audit trail interception
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
                entry.Entity.CreatedBy = _currentUserService.UserId;
            }
            
            if (entry.State == EntityState.Modified || entry.State == EntityState.Added)
            {
                entry.Entity.LastModified = DateTime.UtcNow;
                entry.Entity.LastModifiedBy = _currentUserService.UserId;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }
}
```

### 2.2 Entity Configurations

**Directory**: `backend/src/Quater.Backend.Data/Configurations/`

Each entity has a dedicated configuration file:

- `SampleConfiguration.cs` - Indexes, relationships, constraints
- `TestResultConfiguration.cs` - Composite indexes for performance
- `ParameterConfiguration.cs` - Unique constraints, default values
- `UserConfiguration.cs` - Integration with ASP.NET Core Identity
- `LabConfiguration.cs`
- `SyncLogConfiguration.cs`
- `AuditLogConfiguration.cs`
- `AuditLogArchiveConfiguration.cs`
- `ConflictBackupConfiguration.cs` - **NEW**

**Example**: `SampleConfiguration.cs`

```csharp
public class SampleConfiguration : IEntityTypeConfiguration<Sample>
{
    public void Configure(EntityTypeBuilder<Sample> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever(); // Client generates Guids

        builder.Property(s => s.Type).HasMaxLength(50).IsRequired();
        builder.Property(s => s.LocationLatitude).IsRequired();
        builder.Property(s => s.LocationLongitude).IsRequired();
        builder.Property(s => s.LocationDescription).HasMaxLength(200);
        builder.Property(s => s.LocationHierarchy).HasMaxLength(500);
        builder.Property(s => s.CollectionDate).IsRequired();
        builder.Property(s => s.CollectorName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.Status).HasMaxLength(20).IsRequired();
        
        // Optimistic concurrency
        builder.Property(s => s.Version).IsConcurrencyToken();
        builder.Property(s => s.LastModified).IsConcurrencyToken();
        
        // Indexes
        builder.HasIndex(s => s.LastModified).HasDatabaseName("IX_Samples_LastModified");
        builder.HasIndex(s => s.IsSynced).HasDatabaseName("IX_Samples_IsSynced");
        builder.HasIndex(s => s.Status).HasDatabaseName("IX_Samples_Status");
        builder.HasIndex(s => s.LabId).HasDatabaseName("IX_Samples_LabId");
        builder.HasIndex(s => s.CollectionDate).HasDatabaseName("IX_Samples_CollectionDate");
        builder.HasIndex(s => new { s.IsSynced, s.LastModified }).HasDatabaseName("IX_Samples_Sync");

        // Relationships
        builder.HasOne<Lab>().WithMany().HasForeignKey(s => s.LabId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany<TestResult>().WithOne().HasForeignKey(tr => tr.SampleId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 2.3 Migrations

**Directory**: `backend/src/Quater.Backend.Data/Migrations/`

**Migration Strategy**:
1. Initial migration creates all tables
2. Separate migration for OpenIddict tables (already exists)
3. Migration for ConflictBackup table (NEW)
4. Migrations are applied programmatically at startup

**Commands**:
```bash
# Create new migration
dotnet ef migrations add MigrationName --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api

# Apply migrations
dotnet ef database update --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api
```

### 2.4 New Entity: ConflictBackup

**File**: `backend/src/Quater.Backend.Core/Models/ConflictBackup.cs`

```csharp
public sealed class ConflictBackup
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;  // Sample, TestResult, Parameter
    public Guid EntityId { get; init; }
    public int OverwrittenVersion { get; init; }
    public string BackupData { get; init; } = string.Empty;  // JSON serialized entity
    public DateTime BackupTimestamp { get; init; }
    public string OverwrittenBy { get; init; } = string.Empty;  // User who won the conflict
    public string ConflictResolutionStrategy { get; init; } = "LastWriteWins";
    public string? Notes { get; init; }
}
```

**Purpose**: Store the losing version of data when a sync conflict occurs (Last-Write-Wins).

---

## 3. Core Domain Models

### 3.1 Base Interfaces

**File**: `backend/src/Quater.Backend.Core/Interfaces/IEntity.cs`

```csharp
public interface IEntity
{
    Guid Id { get; }
}

public interface IAuditable
{
    DateTime CreatedDate { get; set; }
    string CreatedBy { get; set; }
    DateTime LastModified { get; set; }
    string LastModifiedBy { get; set; }
}

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public interface ISyncable
{
    int Version { get; set; }
    bool IsSynced { get; set; }
}

public interface IConcurrent
{
    byte[] RowVersion { get; set; }
}
```

### 3.2 Domain Entities (Updates)

All entities in `backend/src/Quater.Backend.Core/Models/` should implement relevant interfaces:

- `Sample` : IEntity, IAuditable, ISoftDelete, ISyncable, IConcurrent
- `TestResult` : IEntity, IAuditable, ISoftDelete, ISyncable, IConcurrent
- `Parameter` : IEntity, IAuditable, ISoftDelete
- `User` : IEntity, IAuditable
- `Lab` : IEntity, IAuditable, ISoftDelete
- `SyncLog` : IEntity
- `AuditLog` : IEntity
- `AuditLogArchive` : IEntity
- `ConflictBackup` : IEntity (NEW)

### 3.3 Enumerations

**Already Exist**:
- `ComplianceStatus` (Pass, Fail, Warning)
- `SampleStatus` (Pending, Completed, Archived)
- `SampleType` (DrinkingWater, Wastewater, SurfaceWater, Groundwater, IndustrialWater)
- `TestMethod` (Titration, Spectrophotometry, Chromatography, Microscopy, Electrode, Culture, Other)
- `UserRole` (Admin, Technician, Viewer)

**NEW Enums**:

`backend/src/Quater.Backend.Core/Enums/ConflictResolutionStrategy.cs`
```csharp
public enum ConflictResolutionStrategy
{
    LastWriteWins,
    FirstWriteWins,   // Reserved for future
    Manual            // Reserved for future
}
```

`backend/src/Quater.Backend.Core/Enums/SyncStatus.cs`
```csharp
public enum SyncStatus
{
    Success,
    Failed,
    InProgress,
    PartialSuccess
}
```

---

## 4. Authentication & Authorization

### 4.1 ASP.NET Core Identity Setup

**File**: `backend/src/Quater.Backend.Api/Program.cs`

**Configuration**:
```csharp
// Identity configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings (5 failed attempts = 15 min lockout)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;  // MVP: no email confirmation
})
.AddEntityFrameworkStores<QuaterDbContext>()
.AddDefaultTokenProviders();
```

### 4.2 OpenIddict Configuration

**Already Configured** in `Program.cs` but needs enhancement:

```csharp
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<QuaterDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token")
               .SetUserinfoEndpointUris("/connect/userinfo")
               .SetAuthorizationEndpointUris("/connect/authorize");

        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();  // PRODUCTION: Use cert

        options.RegisterScopes("api", "offline_access");

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough();

        // Token lifetime
        options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });
```

### 4.3 AuthController Enhancements

**File**: `backend/src/Quater.Backend.Api/Controllers/AuthController.cs` (already exists, needs rate limiting)

**NEW Endpoints**:

1. `POST /api/v1/auth/register` - Register new user
2. `POST /api/v1/auth/login` - Login with email/password
3. `POST /api/v1/auth/refresh` - Refresh access token
4. `POST /api/v1/auth/logout` - Revoke refresh token
5. `POST /api/v1/auth/change-password` - Change password (NEW)
6. `POST /api/v1/auth/forgot-password` - Request password reset (NEW - Phase 2)
7. `GET /api/v1/auth/userinfo` - Get current user info (NEW)

**Rate Limiting**: Apply rate limiting policy to login endpoint (see section 14.2)

### 4.4 Role-Based Authorization

**File**: `backend/src/Quater.Backend.Api/Extensions/AuthorizationExtensions.cs`

```csharp
public static class AuthorizationExtensions
{
    public static IServiceCollection AddQuaterAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()));
            options.AddPolicy("TechnicianOrAdmin", policy => policy.RequireRole(
                UserRole.Technician.ToString(), UserRole.Admin.ToString()));
            options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
```

**Usage in Controllers**:
```csharp
[Authorize(Policy = "AdminOnly")]
[HttpPost("api/v1/users")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct) { ... }

[Authorize(Policy = "TechnicianOrAdmin")]
[HttpPost("api/v1/samples")]
public async Task<IActionResult> CreateSample([FromBody] CreateSampleRequest request, CancellationToken ct) { ... }
```

---

## 5. API Controllers & Endpoints

### 5.1 Controller Structure

Each controller follows this pattern:
- API versioning: `/api/v1/[controller]`
- Authorization via `[Authorize]` attribute
- Rate limiting via `[EnableRateLimiting]` attribute
- Input validation via FluentValidation
- Pagination for list endpoints
- Structured logging
- Result pattern for error handling

### 5.2 SamplesController

**File**: `backend/src/Quater.Backend.Api/Controllers/SamplesController.cs` (already exists, needs completion)

**Endpoints**:

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/v1/samples` | List samples (paginated, filtered) | TechnicianOrAdmin |
| GET | `/api/v1/samples/{id}` | Get sample by ID | TechnicianOrAdmin |
| POST | `/api/v1/samples` | Create new sample | TechnicianOrAdmin |
| PUT | `/api/v1/samples/{id}` | Update sample | TechnicianOrAdmin |
| DELETE | `/api/v1/samples/{id}` | Soft delete sample | AdminOnly |
| GET | `/api/v1/samples/{id}/test-results` | Get all test results for sample | TechnicianOrAdmin |
| POST | `/api/v1/samples/bulk-import` | Bulk import samples from CSV | AdminOnly |
| GET | `/api/v1/samples/export` | Export samples to CSV | TechnicianOrAdmin |

**Pagination & Filtering**:
```csharp
[HttpGet]
[Authorize(Policy = "TechnicianOrAdmin")]
[EnableRateLimiting("api")]
public async Task<IActionResult> GetSamples(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 100,
    [FromQuery] string? status = null,
    [FromQuery] string? type = null,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null,
    [FromQuery] string? search = null,
    CancellationToken ct = default)
{
    if (pageSize > 1000) pageSize = 1000;  // Max page size

    var query = _sampleService.GetSamplesQuery();

    if (!string.IsNullOrEmpty(status))
        query = query.Where(s => s.Status == status);

    if (!string.IsNullOrEmpty(type))
        query = query.Where(s => s.Type == type);

    if (fromDate.HasValue)
        query = query.Where(s => s.CollectionDate >= fromDate.Value);

    if (toDate.HasValue)
        query = query.Where(s => s.CollectionDate <= toDate.Value);

    if (!string.IsNullOrEmpty(search))
        query = query.Where(s => s.LocationDescription.Contains(search) || s.Notes.Contains(search));

    var totalCount = await query.CountAsync(ct);
    var samples = await query
        .OrderByDescending(s => s.CollectionDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    var response = new PagedResponse<SampleDto>
    {
        Items = samples.Select(s => s.ToDto()).ToList(),
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };

    return Ok(response);
}
```

### 5.3 TestResultsController

**File**: `backend/src/Quater.Backend.Api/Controllers/TestResultsController.cs` (needs creation)

**Endpoints**:

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/v1/test-results` | List test results (paginated, filtered) | TechnicianOrAdmin |
| GET | `/api/v1/test-results/{id}` | Get test result by ID | TechnicianOrAdmin |
| POST | `/api/v1/test-results` | Create new test result | TechnicianOrAdmin |
| PUT | `/api/v1/test-results/{id}` | Update test result | TechnicianOrAdmin |
| DELETE | `/api/v1/test-results/{id}` | Soft delete test result | AdminOnly |
| POST | `/api/v1/test-results/bulk-import` | Bulk import test results from CSV | AdminOnly |
| GET | `/api/v1/test-results/export` | Export test results to CSV | TechnicianOrAdmin |
| POST | `/api/v1/test-results/validate` | Validate test result against WHO standards | TechnicianOrAdmin |

**Auto-Compliance Calculation**:
```csharp
[HttpPost]
[Authorize(Policy = "TechnicianOrAdmin")]
public async Task<IActionResult> CreateTestResult([FromBody] CreateTestResultRequest request, CancellationToken ct)
{
    var parameter = await _parameterService.GetByNameAsync(request.ParameterName, ct);
    if (parameter is null)
        return BadRequest($"Parameter '{request.ParameterName}' not found");

    var complianceStatus = _complianceService.CalculateCompliance(request.Value, parameter);

    var testResult = new TestResult
    {
        Id = Guid.NewGuid(),
        SampleId = request.SampleId,
        ParameterName = request.ParameterName,
        Value = request.Value,
        Unit = parameter.Unit,
        TestDate = request.TestDate ?? DateTime.UtcNow,
        TechnicianName = request.TechnicianName,
        TestMethod = request.TestMethod,
        ComplianceStatus = complianceStatus.ToString(),
        Version = 1,
        IsSynced = false
    };

    await _testResultService.CreateAsync(testResult, ct);
    return CreatedAtAction(nameof(GetTestResult), new { id = testResult.Id }, testResult.ToDto());
}
```

### 5.4 ParametersController

**File**: `backend/src/Quater.Backend.Api/Controllers/ParametersController.cs` (needs creation)

**Endpoints**:

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/v1/parameters` | List all parameters | Authenticated |
| GET | `/api/v1/parameters/{id}` | Get parameter by ID | Authenticated |
| POST | `/api/v1/parameters` | Create new parameter | AdminOnly |
| PUT | `/api/v1/parameters/{id}` | Update parameter | AdminOnly |
| DELETE | `/api/v1/parameters/{id}` | Soft delete parameter | AdminOnly |
| GET | `/api/v1/parameters/active` | Get all active parameters | Authenticated |

### 5.5 UsersController

**File**: `backend/src/Quater.Backend.Api/Controllers/UsersController.cs` (needs creation)

**Endpoints**:

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/v1/users` | List all users | AdminOnly |
| GET | `/api/v1/users/{id}` | Get user by ID | AdminOnly |
| POST | `/api/v1/users` | Create new user | AdminOnly |
| PUT | `/api/v1/users/{id}` | Update user | AdminOnly |
| DELETE | `/api/v1/users/{id}` | Deactivate user | AdminOnly |
| PUT | `/api/v1/users/{id}/role` | Change user role | AdminOnly |

### 5.6 LabsController

**File**: `backend/src/Quater.Backend.Api/Controllers/LabsController.cs` (needs creation)

**Endpoints**:

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/v1/labs` | List all labs | AdminOnly |
| GET | `/api/v1/labs/{id}` | Get lab by ID | AdminOnly |
| POST | `/api/v1/labs` | Create new lab | AdminOnly |
| PUT | `/api/v1/labs/{id}` | Update lab | AdminOnly |
| DELETE | `/api/v1/labs/{id}` | Soft delete lab | AdminOnly |

### 5.7 ReportsController

**File**: `backend/src/Quater.Backend.Api/Controllers/ReportsController.cs` (needs creation)

**Endpoints**:

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/api/v1/reports/compliance` | Generate compliance report (PDF) | TechnicianOrAdmin |
| POST | `/api/v1/reports/summary` | Generate summary report (JSON) | TechnicianOrAdmin |
| GET | `/api/v1/reports/stats` | Get overall statistics | TechnicianOrAdmin |

**Synchronous PDF Generation** (per user decision):
```csharp
[HttpPost("compliance")]
[Authorize(Policy = "TechnicianOrAdmin")]
public async Task<IActionResult> GenerateComplianceReport([FromBody] ComplianceReportRequest request, CancellationToken ct)
{
    _logger.LogInformation("Generating compliance report: {@Request}", request);

    var samples = await _sampleService.GetSamplesForReportAsync(request.FromDate, request.ToDate, request.SampleType, ct);
    var pdfBytes = await _reportService.GenerateComplianceReportPdfAsync(samples, ct);

    return File(pdfBytes, "application/pdf", $"compliance-report-{DateTime.UtcNow:yyyyMMdd}.pdf");
}
```

---

## 6. Sync Engine

### 6.1 Sync Architecture

**Directory**: `backend/src/Quater.Backend.Sync/`

**Components**:
1. **SyncService** - Orchestrates sync process
2. **ConflictResolver** - Implements Last-Write-Wins strategy
3. **ChangeTracker** - Tracks entity changes
4. **BackupService** - Creates conflict backups

### 6.2 SyncController

**File**: `backend/src/Quater.Backend.Api/Controllers/SyncController.cs` (needs creation)

**Endpoints**:

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/api/v1/sync/pull` | Pull changes from server | Authenticated |
| POST | `/api/v1/sync/push` | Push changes to server | Authenticated |
| GET | `/api/v1/sync/status` | Get last sync status | Authenticated |
| GET | `/api/v1/sync/conflicts` | Get list of conflicts | Authenticated |
| POST | `/api/v1/sync/conflicts/{id}/resolve` | Manually resolve conflict | TechnicianOrAdmin |

### 6.3 Sync Flow

**Pull (Client → Server)**:
```csharp
[HttpPost("pull")]
[Authorize]
public async Task<IActionResult> PullChanges([FromBody] SyncPullRequest request, CancellationToken ct)
{
    var deviceId = request.DeviceId;
    var lastSyncTimestamp = request.LastSyncTimestamp;

    // Get all changes since last sync
    var changes = await _syncService.GetChangesAsync(lastSyncTimestamp, ct);

    // Log sync attempt
    await _syncLogService.LogPullAsync(deviceId, changes.Count, ct);

    var response = new SyncPullResponse
    {
        Changes = changes.Select(c => c.ToDto()).ToList(),
        Timestamp = DateTime.UtcNow
    };

    return Ok(response);
}
```

**Push (Server → Client)**:
```csharp
[HttpPost("push")]
[Authorize]
public async Task<IActionResult> PushChanges([FromBody] SyncPushRequest request, CancellationToken ct)
{
    var conflicts = new List<ConflictDto>();
    var accepted = new List<Guid>();
    var rejected = new List<RejectionDto>();

    foreach (var change in request.Changes)
    {
        var result = await _syncService.ApplyChangeAsync(change, ct);

        if (result.IsConflict)
        {
            // Last-Write-Wins: Compare timestamps
            var winningVersion = await _conflictResolver.ResolveAsync(change, result.ServerVersion, ct);

            if (winningVersion == ConflictWinner.Client)
            {
                // Backup server version
                await _backupService.BackupEntityAsync(result.ServerVersion, ct);
                // Apply client version
                await _syncService.ApplyChangeAsync(change, ct, force: true);
                accepted.Add(change.EntityId);
            }
            else
            {
                // Client version is older, reject and backup client version
                conflicts.Add(new ConflictDto
                {
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    ClientVersion = change.Version,
                    ServerVersion = result.ServerVersion.Version,
                    WinningVersion = "Server"
                });
            }
        }
        else if (result.IsSuccess)
        {
            accepted.Add(change.EntityId);
        }
        else
        {
            rejected.Add(new RejectionDto
            {
                EntityId = change.EntityId,
                Reason = result.Error,
                Message = result.ErrorMessage
            });
        }
    }

    await _syncLogService.LogPushAsync(request.DeviceId, accepted.Count, conflicts.Count, ct);

    var response = new SyncPushResponse
    {
        Conflicts = conflicts,
        Accepted = accepted,
        Rejected = rejected,
        Timestamp = DateTime.UtcNow
    };

    return Ok(response);
}
```

### 6.4 ConflictResolver

**File**: `backend/src/Quater.Backend.Sync/Resolvers/ConflictResolver.cs`

```csharp
public sealed class ConflictResolver(IBackupService backupService, ILogger<ConflictResolver> logger)
{
    public async Task<ConflictWinner> ResolveAsync<T>(
        ChangeRecord clientChange,
        T serverEntity,
        CancellationToken ct) where T : ISyncable, IAuditable
    {
        // Last-Write-Wins: Compare LastModified timestamps
        var clientTimestamp = clientChange.LastModified;
        var serverTimestamp = serverEntity.LastModified;

        if (clientTimestamp > serverTimestamp)
        {
            logger.LogInformation("Conflict resolved: Client wins ({ClientTime} > {ServerTime})",
                clientTimestamp, serverTimestamp);

            // Backup losing server version
            await backupService.BackupAsync(serverEntity, "LastWriteWins", ct);

            return ConflictWinner.Client;
        }
        else
        {
            logger.LogInformation("Conflict resolved: Server wins ({ServerTime} >= {ClientTime})",
                serverTimestamp, clientTimestamp);

            // Backup losing client version (via conflict notification)
            await backupService.BackupAsync(clientChange, "LastWriteWins", ct);

            return ConflictWinner.Server;
        }
    }
}

public enum ConflictWinner
{
    Client,
    Server
}
```

### 6.5 BackupService

**File**: `backend/src/Quater.Backend.Sync/Services/BackupService.cs`

```csharp
public sealed class BackupService(QuaterDbContext dbContext, ILogger<BackupService> logger)
{
    public async Task BackupAsync<T>(T entity, string strategy, CancellationToken ct) where T : IEntity, ISyncable, IAuditable
    {
        var backup = new ConflictBackup
        {
            Id = Guid.NewGuid(),
            EntityType = typeof(T).Name,
            EntityId = entity.Id,
            OverwrittenVersion = entity.Version,
            BackupData = JsonSerializer.Serialize(entity),
            BackupTimestamp = DateTime.UtcNow,
            OverwrittenBy = entity.LastModifiedBy,
            ConflictResolutionStrategy = strategy,
            Notes = $"Automatic backup: {strategy}"
        };

        dbContext.ConflictBackups.Add(backup);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Backed up {EntityType} {EntityId} version {Version}",
            backup.EntityType, backup.EntityId, backup.OverwrittenVersion);
    }
}
```

---

## 7. Bulk Operations

### 7.1 Bulk Import Architecture

**File**: `backend/src/Quater.Backend.Api/Services/BulkImportService.cs`

**Features**:
- CSV/Excel parsing with CsvHelper
- Validation of each row
- Transaction support (all or nothing)
- Error reporting (which rows failed and why)
- Progress tracking (for large files)

**Dependencies**:
- `CsvHelper` package
- `EPPlus` package for Excel (optional, Phase 2)

### 7.2 Sample Bulk Import

**Endpoint**: `POST /api/v1/samples/bulk-import`

**Request**:
```json
{
  "file": "base64-encoded-csv",
  "validateOnly": false
}
```

**Implementation**:
```csharp
[HttpPost("bulk-import")]
[Authorize(Policy = "AdminOnly")]
[RequestSizeLimit(10_000_000)]  // 10 MB max
public async Task<IActionResult> BulkImportSamples([FromBody] BulkImportRequest request, CancellationToken ct)
{
    var csvBytes = Convert.FromBase64String(request.FileBase64);
    using var stream = new MemoryStream(csvBytes);
    using var reader = new StreamReader(stream);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

    csv.Context.RegisterClassMap<SampleCsvMap>();

    var records = csv.GetRecords<SampleCsvDto>().ToList();
    var errors = new List<BulkImportError>();
    var samples = new List<Sample>();

    for (int i = 0; i < records.Count; i++)
    {
        var record = records[i];
        var validation = await _validator.ValidateAsync(record, ct);

        if (!validation.IsValid)
        {
            errors.Add(new BulkImportError
            {
                Row = i + 2,  // CSV rows start at 2 (header is row 1)
                Errors = validation.Errors.Select(e => e.ErrorMessage).ToList()
            });
            continue;
        }

        samples.Add(record.ToEntity());
    }

    if (request.ValidateOnly)
    {
        return Ok(new BulkImportValidationResponse
        {
            TotalRows = records.Count,
            ValidRows = samples.Count,
            InvalidRows = errors.Count,
            Errors = errors
        });
    }

    if (errors.Any())
    {
        return BadRequest(new BulkImportValidationResponse
        {
            TotalRows = records.Count,
            ValidRows = samples.Count,
            InvalidRows = errors.Count,
            Errors = errors
        });
    }

    // Import all samples in a transaction
    await _sampleService.BulkCreateAsync(samples, ct);

    return Ok(new BulkImportResponse
    {
        TotalImported = samples.Count,
        Errors = errors
    });
}
```

### 7.3 CSV Mapping

**File**: `backend/src/Quater.Backend.Api/Models/Csv/SampleCsvMap.cs`

```csharp
public sealed class SampleCsvMap : ClassMap<SampleCsvDto>
{
    public SampleCsvMap()
    {
        Map(m => m.Type).Name("Sample Type");
        Map(m => m.LocationLatitude).Name("Latitude");
        Map(m => m.LocationLongitude).Name("Longitude");
        Map(m => m.LocationDescription).Name("Location").Optional();
        Map(m => m.CollectionDate).Name("Collection Date");
        Map(m => m.CollectorName).Name("Collector");
        Map(m => m.Notes).Name("Notes").Optional();
    }
}
```

### 7.4 Bulk Export

**Endpoint**: `GET /api/v1/samples/export?fromDate=...&toDate=...&format=csv`

**Implementation**:
```csharp
[HttpGet("export")]
[Authorize(Policy = "TechnicianOrAdmin")]
public async Task<IActionResult> ExportSamples(
    [FromQuery] DateTime? fromDate,
    [FromQuery] DateTime? toDate,
    [FromQuery] string format = "csv",
    CancellationToken ct = default)
{
    var query = _sampleService.GetSamplesQuery();

    if (fromDate.HasValue)
        query = query.Where(s => s.CollectionDate >= fromDate.Value);

    if (toDate.HasValue)
        query = query.Where(s => s.CollectionDate <= toDate.Value);

    var samples = await query.ToListAsync(ct);

    if (format.ToLower() == "csv")
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.Context.RegisterClassMap<SampleCsvMap>();
        csv.WriteRecords(samples.Select(s => s.ToCsvDto()));

        var csvBytes = Encoding.UTF8.GetBytes(writer.ToString());
        return File(csvBytes, "text/csv", $"samples-export-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    return BadRequest("Unsupported format");
}
```

---

## 8. Report Generation

### 8.1 ReportService

**File**: `backend/src/Quater.Backend.Api/Services/ReportService.cs`

**Dependencies**:
- `QuestPDF` package

**Features**:
- Compliance reports (pass/fail summary, threshold comparisons)
- WHO standards reference
- Charts/graphs (simple bar charts via QuestPDF)
- Professional layout with lab branding

### 8.2 ComplianceReportDocument

**File**: `backend/src/Quater.Backend.Api/Reports/ComplianceReportDocument.cs`

```csharp
public sealed class ComplianceReportDocument : IDocument
{
    private readonly List<Sample> _samples;
    private readonly List<TestResult> _testResults;
    private readonly List<Parameter> _parameters;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;

    public ComplianceReportDocument(
        List<Sample> samples,
        List<TestResult> testResults,
        List<Parameter> parameters,
        DateTime fromDate,
        DateTime toDate)
    {
        _samples = samples;
        _testResults = testResults;
        _parameters = parameters;
        _fromDate = fromDate;
        _toDate = toDate;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(50);
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(x =>
            {
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Quater Water Quality Lab Management System").FontSize(20).Bold();
                column.Item().Text("Compliance Report").FontSize(16);
                column.Item().Text($"Period: {_fromDate:yyyy-MM-dd} to {_toDate:yyyy-MM-dd}").FontSize(12);
                column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(10);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            // Summary section
            column.Item().Element(ComposeSummary);

            column.Item().PaddingVertical(10);

            // Compliance breakdown table
            column.Item().Element(ComposeComplianceTable);

            column.Item().PaddingVertical(10);

            // Non-compliant samples (if any)
            var nonCompliantSamples = _samples.Where(s =>
                _testResults.Any(tr => tr.SampleId == s.Id && tr.ComplianceStatus == "Fail")).ToList();

            if (nonCompliantSamples.Any())
            {
                column.Item().Element(container => ComposeNonCompliantSamples(container, nonCompliantSamples));
            }
        });
    }

    private void ComposeSummary(IContainer container)
    {
        var totalSamples = _samples.Count;
        var totalTests = _testResults.Count;
        var passedTests = _testResults.Count(tr => tr.ComplianceStatus == "Pass");
        var failedTests = _testResults.Count(tr => tr.ComplianceStatus == "Fail");
        var warningTests = _testResults.Count(tr => tr.ComplianceStatus == "Warning");

        container.Column(column =>
        {
            column.Item().Text("Summary").FontSize(16).Bold();
            column.Item().Text($"Total Samples: {totalSamples}");
            column.Item().Text($"Total Tests: {totalTests}");
            column.Item().Text($"Passed: {passedTests} ({(passedTests / (double)totalTests * 100):F1}%)");
            column.Item().Text($"Failed: {failedTests} ({(failedTests / (double)totalTests * 100):F1}%)");
            column.Item().Text($"Warning: {warningTests} ({(warningTests / (double)totalTests * 100):F1}%)");
        });
    }

    private void ComposeComplianceTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Parameter").Bold();
                header.Cell().Element(CellStyle).Text("Unit").Bold();
                header.Cell().Element(CellStyle).Text("WHO Threshold").Bold();
                header.Cell().Element(CellStyle).Text("Tests").Bold();
                header.Cell().Element(CellStyle).Text("Pass Rate").Bold();
            });

            foreach (var parameter in _parameters)
            {
                var tests = _testResults.Where(tr => tr.ParameterName == parameter.Name).ToList();
                if (!tests.Any()) continue;

                var passRate = tests.Count(t => t.ComplianceStatus == "Pass") / (double)tests.Count * 100;

                table.Cell().Element(CellStyle).Text(parameter.Name);
                table.Cell().Element(CellStyle).Text(parameter.Unit);
                table.Cell().Element(CellStyle).Text(parameter.WhoThreshold?.ToString() ?? "N/A");
                table.Cell().Element(CellStyle).Text(tests.Count.ToString());
                table.Cell().Element(CellStyle).Text($"{passRate:F1}%");
            }
        });

        static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
    }

    private void ComposeNonCompliantSamples(IContainer container, List<Sample> nonCompliantSamples)
    {
        container.Column(column =>
        {
            column.Item().Text("Non-Compliant Samples").FontSize(14).Bold();

            foreach (var sample in nonCompliantSamples)
            {
                var failedTests = _testResults.Where(tr => tr.SampleId == sample.Id && tr.ComplianceStatus == "Fail").ToList();

                column.Item().PaddingTop(10).Column(sampleColumn =>
                {
                    sampleColumn.Item().Text($"Sample: {sample.LocationDescription ?? $"({sample.LocationLatitude}, {sample.LocationLongitude})"}").Bold();
                    sampleColumn.Item().Text($"Collection Date: {sample.CollectionDate:yyyy-MM-dd}");
                    sampleColumn.Item().Text("Failed Tests:");

                    foreach (var test in failedTests)
                    {
                        var parameter = _parameters.First(p => p.Name == test.ParameterName);
                        sampleColumn.Item().Text($"  - {test.ParameterName}: {test.Value} {test.Unit} (Threshold: {parameter.WhoThreshold} {parameter.Unit})");
                    }
                });
            }
        });
    }
}
```

### 8.3 Report Generation Endpoint

**File**: `backend/src/Quater.Backend.Api/Controllers/ReportsController.cs`

```csharp
[HttpPost("compliance")]
[Authorize(Policy = "TechnicianOrAdmin")]
[EnableRateLimiting("api")]
public async Task<IActionResult> GenerateComplianceReport([FromBody] ComplianceReportRequest request, CancellationToken ct)
{
    _logger.LogInformation("Generating compliance report: {@Request}", request);

    // Fetch data
    var samples = await _sampleService.GetSamplesAsync(request.FromDate, request.ToDate, request.SampleType, ct);
    var sampleIds = samples.Select(s => s.Id).ToList();
    var testResults = await _testResultService.GetBySampleIdsAsync(sampleIds, ct);
    var parameters = await _parameterService.GetAllActiveAsync(ct);

    // Generate PDF
    var document = new ComplianceReportDocument(samples, testResults, parameters, request.FromDate, request.ToDate);
    var pdfBytes = document.GeneratePdf();

    _logger.LogInformation("Compliance report generated: {Size} bytes", pdfBytes.Length);

    return File(pdfBytes, "application/pdf", $"compliance-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
}
```

**Performance**: Should generate PDF for 100+ samples in <10 seconds (per success criteria SC-003)

---

## 9. Background Jobs

### 9.1 Quartz.NET Setup

**File**: `backend/src/Quater.Backend.Api/Program.cs`

```csharp
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    // Audit Log Archival Job (daily at 2 AM UTC)
    var jobKey = new JobKey("AuditLogArchivalJob");
    q.AddJob<AuditLogArchivalJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("AuditLogArchivalJob-trigger")
        .WithCronSchedule("0 0 2 * * ?"));  // Daily at 2 AM UTC
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
```

### 9.2 AuditLogArchivalJob

**File**: `backend/src/Quater.Backend.Api/Jobs/AuditLogArchivalJob.cs` (already exists, may need review)

**Purpose**: Move audit logs older than 90 days to `AuditLogArchive` table

**Implementation**:
```csharp
public sealed class AuditLogArchivalJob(QuaterDbContext dbContext, ILogger<AuditLogArchivalJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        logger.LogInformation("Starting audit log archival for logs older than {CutoffDate}", cutoffDate);

        var logsToArchive = await dbContext.AuditLogs
            .Where(a => a.Timestamp < cutoffDate && !a.IsArchived)
            .ToListAsync();

        logger.LogInformation("Found {Count} audit logs to archive", logsToArchive.Count);

        if (!logsToArchive.Any())
            return;

        // Copy to archive table
        var archivedLogs = logsToArchive.Select(log => new AuditLogArchive
        {
            Id = log.Id,
            UserId = log.UserId,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Action = log.Action,
            OldValue = log.OldValue,
            NewValue = log.NewValue,
            ConflictResolutionNotes = log.ConflictResolutionNotes,
            Timestamp = log.Timestamp,
            IpAddress = log.IpAddress,
            ArchivedDate = DateTime.UtcNow
        }).ToList();

        dbContext.AuditLogArchive.AddRange(archivedLogs);

        // Mark original logs as archived (keep for referential integrity)
        foreach (var log in logsToArchive)
        {
            log.IsArchived = true;
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Successfully archived {Count} audit logs", logsToArchive.Count);
    }
}
```

**Alternative Strategy**: Delete from `AuditLogs` after archiving (saves space but loses referential integrity). Recommendation: Keep as `IsArchived = true` for now.

### 9.3 Future Background Jobs (Phase 2)

- **Automatic Sync Retry Job**: Retry failed sync operations
- **Report Scheduling Job**: Generate reports on schedule
- **Email Notification Job**: Send alerts for non-compliant samples
- **Database Cleanup Job**: Remove old soft-deleted records after retention period

---

## 10. Middleware & Cross-Cutting Concerns

### 10.1 Global Exception Handler

**File**: `backend/src/Quater.Backend.Api/Middleware/ExceptionHandlingMiddleware.cs`

```csharp
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = exception.Message,
            Details = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                ? exception.StackTrace
                : null
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(response);
    }
}
```

**Registration**: `app.UseMiddleware<ExceptionHandlingMiddleware>();`

### 10.2 Request Logging Middleware

**File**: `backend/src/Quater.Backend.Api/Middleware/RequestLoggingMiddleware.cs`

```csharp
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid();

        context.Items["RequestId"] = requestId;

        logger.LogInformation("Request started: {Method} {Path} [RequestId: {RequestId}]",
            context.Request.Method, context.Request.Path, requestId);

        await next(context);

        stopwatch.Stop();

        logger.LogInformation("Request completed: {Method} {Path} {StatusCode} in {ElapsedMs}ms [RequestId: {RequestId}]",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, requestId);
    }
}
```

### 10.3 Rate Limiting Configuration

**File**: `backend/src/Quater.Backend.Api/Program.cs`

```csharp
builder.Services.AddRateLimiter(options =>
{
    // API endpoints: 100 requests per minute for authenticated users
    options.AddPolicy("api", context =>
    {
        var user = context.User;
        var partitionKey = user?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = user?.Identity?.IsAuthenticated == true ? 100 : 20,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokensPerPeriod = user?.Identity?.IsAuthenticated == true ? 100 : 20,
            AutoReplenishment = true
        });
    });

    // Auth endpoints: 5 requests per 15 minutes (prevent brute force)
    options.AddPolicy("auth", context =>
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(15),
            PermitLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

app.UseRateLimiter();
```

**Usage**:
```csharp
[EnableRateLimiting("auth")]
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) { ... }

[EnableRateLimiting("api")]
[HttpGet("samples")]
public async Task<IActionResult> GetSamples(...) { ... }
```

### 10.4 Structured Logging (Serilog)

**File**: `backend/src/Quater.Backend.Api/Program.cs`

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/quater-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        tableName: "Logs",
        needAutoCreateTable: true,
        restrictedToMinimumLevel: LogEventLevel.Warning)
    .CreateLogger();

builder.Host.UseSerilog();
```

**appsettings.json**:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    }
  }
}
```

### 10.5 CORS Configuration

**File**: `backend/src/Quater.Backend.Api/Program.cs`

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDesktopAndMobile", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",  // Desktop dev
                "http://localhost:8081")  // Mobile dev
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

app.UseCors("AllowDesktopAndMobile");
```

---

## 11. Testing Strategy

### 11.1 Unit Tests

**Directory**: `backend/tests/Quater.Backend.Core.Tests/`

**Coverage**:
- Business logic services
- Validators (FluentValidation)
- Conflict resolution strategies
- Compliance calculation logic

**Example**: `ComplianceCalculatorTests.cs`

```csharp
public sealed class ComplianceCalculatorTests
{
    [Fact]
    public void CalculateCompliance_WhenValueBelowThreshold_ReturnsPass()
    {
        // Arrange
        var parameter = new Parameter
        {
            Name = "pH",
            WhoThreshold = 8.5,
            Unit = "pH"
        };
        var value = 7.5;

        // Act
        var result = ComplianceCalculator.CalculateCompliance(value, parameter);

        // Assert
        result.Should().Be(ComplianceStatus.Pass);
    }

    [Fact]
    public void CalculateCompliance_WhenValueAboveThreshold_ReturnsFail()
    {
        // Arrange
        var parameter = new Parameter
        {
            Name = "pH",
            WhoThreshold = 8.5,
            Unit = "pH"
        };
        var value = 9.5;

        // Act
        var result = ComplianceCalculator.CalculateCompliance(value, parameter);

        // Assert
        result.Should().Be(ComplianceStatus.Fail);
    }
}
```

### 11.2 Integration Tests

**Directory**: `backend/tests/Quater.Backend.Api.Tests/`

**Features**:
- WebApplicationFactory for in-memory testing
- Testcontainers for PostgreSQL
- API endpoint testing with authentication
- Database seeding for tests

**Example**: `SamplesControllerTests.cs`

```csharp
public sealed class SamplesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SamplesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with in-memory database
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<QuaterDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<QuaterDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateSample_ValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreateSampleRequest
        {
            Type = "drinking_water",
            LocationLatitude = 33.5731,
            LocationLongitude = -7.5898,
            LocationDescription = "Municipal Well #3",
            CollectionDate = DateTime.UtcNow,
            CollectorName = "John Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/samples", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var sample = await response.Content.ReadFromJsonAsync<SampleDto>();
        sample.Should().NotBeNull();
        sample!.Id.Should().NotBeEmpty();
    }
}
```

### 11.3 Sync Engine Tests

**Directory**: `backend/tests/Quater.Backend.Sync.Tests/`

**Critical Test Cases**:
- Conflict detection
- Last-Write-Wins resolution
- Conflict backup creation
- Version tracking
- Concurrent modifications

**Example**: `ConflictResolverTests.cs`

```csharp
public sealed class ConflictResolverTests
{
    [Fact]
    public async Task Resolve_ClientNewerThanServer_ClientWins()
    {
        // Arrange
        var backupService = Substitute.For<IBackupService>();
        var logger = Substitute.For<ILogger<ConflictResolver>>();
        var resolver = new ConflictResolver(backupService, logger);

        var clientChange = new ChangeRecord
        {
            EntityId = Guid.NewGuid(),
            LastModified = DateTime.UtcNow.AddMinutes(5),
            Version = 2
        };

        var serverEntity = new Sample
        {
            Id = clientChange.EntityId,
            LastModified = DateTime.UtcNow,
            Version = 1
        };

        // Act
        var winner = await resolver.ResolveAsync(clientChange, serverEntity, CancellationToken.None);

        // Assert
        winner.Should().Be(ConflictWinner.Client);
        await backupService.Received(1).BackupAsync(serverEntity, "LastWriteWins", Arg.Any<CancellationToken>());
    }
}
```

---

## 12. Configuration & Deployment

### 12.1 appsettings.json

**File**: `backend/src/Quater.Backend.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=quater;Username=postgres;Password=postgres"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "RateLimiting": {
    "Api": {
      "TokenLimit": 100,
      "ReplenishmentPeriod": 60
    },
    "Auth": {
      "PermitLimit": 5,
      "Window": 900
    }
  },
  "Jwt": {
    "Issuer": "https://quater.local",
    "Audience": "https://quater.local",
    "AccessTokenLifetime": 3600,
    "RefreshTokenLifetime": 1209600
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:8081"]
  },
  "BackgroundJobs": {
    "AuditLogArchivalCronSchedule": "0 0 2 * * ?",
    "AuditLogRetentionDays": 90
  },
  "Report": {
    "MaxSamplesPerReport": 10000
  }
}
```

### 12.2 Docker Compose

**File**: `docker/docker-compose.yml`

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: quater-postgres
    environment:
      POSTGRES_DB: quater
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  backend:
    build:
      context: ../backend
      dockerfile: ../docker/Dockerfile.backend
    container_name: quater-backend
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=quater;Username=postgres;Password=postgres"
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres_data:
```

### 12.3 Dockerfile

**File**: `docker/Dockerfile.backend`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Quater.Backend.Api/Quater.Backend.Api.csproj", "Quater.Backend.Api/"]
COPY ["src/Quater.Backend.Core/Quater.Backend.Core.csproj", "Quater.Backend.Core/"]
COPY ["src/Quater.Backend.Data/Quater.Backend.Data.csproj", "Quater.Backend.Data/"]
COPY ["src/Quater.Backend.Sync/Quater.Backend.Sync.csproj", "Quater.Backend.Sync/"]
RUN dotnet restore "Quater.Backend.Api/Quater.Backend.Api.csproj"
COPY src/ .
WORKDIR "/src/Quater.Backend.Api"
RUN dotnet build "Quater.Backend.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Quater.Backend.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Quater.Backend.Api.dll"]
```

### 12.4 Deployment Script

**File**: `backend/deploy.sh`

```bash
#!/bin/bash
set -e

echo "Building backend..."
dotnet build backend/Quater.Backend.sln --configuration Release

echo "Running tests..."
dotnet test backend/tests/Quater.Backend.Api.Tests/
dotnet test backend/tests/Quater.Backend.Core.Tests/
dotnet test backend/tests/Quater.Backend.Sync.Tests/

echo "Applying migrations..."
dotnet ef database update --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api

echo "Deploying to Docker..."
docker-compose -f docker/docker-compose.yml up -d --build

echo "Deployment complete!"
```

---

## 13. Performance Optimization

### 13.1 Database Query Optimization

**Strategies**:
- Use `AsNoTracking()` for read-only queries
- Implement compiled queries for frequently executed queries
- Add composite indexes for common filter combinations
- Use pagination to limit result sets
- Implement query result caching with `IMemoryCache`

**Example**: Compiled Query

```csharp
private static readonly Func<QuaterDbContext, DateTime, DateTime, IAsyncEnumerable<Sample>> GetSamplesByDateRange =
    EF.CompileAsyncQuery((QuaterDbContext db, DateTime from, DateTime to) =>
        db.Samples.AsNoTracking()
            .Where(s => s.CollectionDate >= from && s.CollectionDate <= to)
            .OrderByDescending(s => s.CollectionDate));

public async Task<List<Sample>> GetSamplesForReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct)
{
    var samples = new List<Sample>();
    await foreach (var sample in GetSamplesByDateRange(_dbContext, fromDate, toDate).WithCancellation(ct))
    {
        samples.Add(sample);
    }
    return samples;
}
```

### 13.2 Caching Strategy

**File**: `backend/src/Quater.Backend.Api/Services/CachedParameterService.cs`

```csharp
public sealed class CachedParameterService(IParameterService inner, IMemoryCache cache, ILogger<CachedParameterService> logger) : IParameterService
{
    private const string CacheKey = "Parameters_All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<List<Parameter>> GetAllActiveAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out List<Parameter>? cached))
        {
            logger.LogDebug("Cache hit: {CacheKey}", CacheKey);
            return cached!;
        }

        logger.LogDebug("Cache miss: {CacheKey}", CacheKey);
        var parameters = await inner.GetAllActiveAsync(ct);

        cache.Set(CacheKey, parameters, CacheDuration);

        return parameters;
    }

    public async Task InvalidateCacheAsync()
    {
        cache.Remove(CacheKey);
        logger.LogInformation("Cache invalidated: {CacheKey}", CacheKey);
    }
}
```

**Cache Invalidation**: Invalidate on parameter updates (via service method)

### 13.3 Bulk Operations Optimization

**Strategies**:
- Use `AddRange()` instead of `Add()` in loops
- Batch SaveChanges() (e.g., every 1000 records)
- Disable automatic change tracking for bulk operations
- Use EF Core BulkExtensions for large imports (e.g., EFCore.BulkExtensions)

**Example**:
```csharp
public async Task BulkCreateAsync(List<Sample> samples, CancellationToken ct)
{
    const int batchSize = 1000;

    for (int i = 0; i < samples.Count; i += batchSize)
    {
        var batch = samples.Skip(i).Take(batchSize).ToList();
        _dbContext.Samples.AddRange(batch);
        await _dbContext.SaveChangesAsync(ct);
    }
}
```

---

## 14. Security Hardening

### 14.1 Input Validation

**FluentValidation for all DTOs**:

**File**: `backend/src/Quater.Backend.Core/Validators/CreateSampleRequestValidator.cs`

```csharp
public sealed class CreateSampleRequestValidator : AbstractValidator<CreateSampleRequest>
{
    public CreateSampleRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(type => Enum.TryParse<SampleType>(type, out _))
            .WithMessage("Invalid sample type");

        RuleFor(x => x.LocationLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.LocationLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180");

        RuleFor(x => x.CollectionDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Collection date cannot be in the future");

        RuleFor(x => x.CollectorName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LocationDescription)
            .MaximumLength(200);

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
```

**Registration**: `builder.Services.AddValidatorsFromAssemblyContaining<CreateSampleRequestValidator>();`

### 14.2 SQL Injection Prevention

- **Always use parameterized queries** (EF Core does this by default)
- **Never concatenate user input into SQL strings**
- **Use `FromSqlInterpolated()` instead of `FromSqlRaw()`**

### 14.3 XSS Prevention

- **Return JSON, not HTML** (ASP.NET Core does this by default)
- **Sanitize all user input** before storing (use `AntiXssEncoder` if rendering HTML)

### 14.4 HTTPS Enforcement

**File**: `backend/src/Quater.Backend.Api/Program.cs`

```csharp
app.UseHttpsRedirection();
```

**Production**: Use reverse proxy (Nginx) for SSL termination

### 14.5 Security Headers

**File**: `backend/src/Quater.Backend.Api/Program.cs`

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

---

## Summary Checklist

### Core Features
- [ ] Database layer with EF Core, soft delete, audit trail
- [ ] ASP.NET Core Identity + OpenIddict authentication
- [ ] Rate limiting (5 failed login attempts = 15 min lockout)
- [ ] Complete CRUD APIs for all entities
- [ ] Bulk import/export (CSV)
- [ ] Sync engine with Last-Write-Wins and conflict backups
- [ ] PDF report generation (QuestPDF, synchronous)
- [ ] Background jobs (Quartz.NET for audit archival)
- [ ] API versioning (`/api/v1/`)
- [ ] Pagination (default 100, max 1000)
- [ ] Soft delete for all entities
- [ ] Structured logging (Serilog)
- [ ] Global exception handling
- [ ] CORS configuration
- [ ] Security headers

### Testing
- [ ] Unit tests for business logic
- [ ] Integration tests for API endpoints
- [ ] Sync engine tests (conflict resolution)
- [ ] FluentValidation tests

### Deployment
- [ ] Docker Compose configuration
- [ ] Dockerfile for backend
- [ ] Deployment script
- [ ] Production configuration (appsettings.Production.json)

---

## Next Steps for Beads Agent

The beads agent will create granular tasks from this plan. Each section should be broken down into implementable tasks with:

- Clear acceptance criteria
- Dependencies on other tasks
- Estimated complexity
- Testing requirements

**Example Task Structure**:
- Task: "Implement ConflictBackup entity and repository"
  - Subtasks:
    1. Create ConflictBackup.cs model
    2. Create ConflictBackupConfiguration.cs
    3. Create migration for ConflictBackup table
    4. Create IConflictBackupRepository interface
    5. Implement ConflictBackupRepository
    6. Write unit tests
    7. Update DbContext

---

**END OF BACKEND IMPLEMENTATION PLAN**

# Single-Tenant Multi-Lab Migration Plan with RLS

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Transform the data model and authorization layer from "User belongs to one Lab" to "User has per-lab roles via UserLab join table," enforced by PostgreSQL Row Level Security (RLS) and context-aware middleware.

**Architecture:** 
1.  **Data Model:** Remove `LabId` and `Role` from `Users` table. Introduce `UserLab` many-to-many table with `Role` column.
2.  **RLS Enforcement:** Enable RLS on all lab-scoped tables. Use PostgreSQL session variable `app.current_lab_id` to enforce isolation.
3.  **Context-Aware Auth:** Middleware extracts `X-Lab-Id` header, verifies user membership in `UserLab`, and sets the Postgres session variable via `DbConnectionInterceptor` or `IAuthService`.
4.  **System Admin:** The "Organization Admin" is simply the `SystemUser` (already exists) or specific users with a global flag, bypassing RLS via special policy.

**Tech Stack:** ASP.NET Core 10, EF Core 9, PostgreSQL RLS, OpenIddict.

---

## Prerequisites
- Working backend with PostgreSQL
- Docker running for Testcontainers

---

## Task 1: Create UserLab Entity and Migration

**Files:**
- Create: `shared/Models/UserLab.cs`
- Modify: `shared/Models/User.cs` (Remove LabId/Role)
- Modify: `shared/Models/Lab.cs` (Update navigation)
- Modify: `backend/src/Quater.Backend.Data/QuaterDbContext.cs` (DbSet and Config)
- Migration: `backend/src/Quater.Backend.Data/Migrations/XXXX_AddUserLabAndRLS.cs`

**Step 1: Define UserLab Join Entity**

Create: `shared/Models/UserLab.cs`
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Quater.Shared.Enums;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a user's membership and role within a specific lab.
/// </summary>
public class UserLab
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid LabId { get; set; }
    public Lab Lab { get; set; } = null!;

    [Required]
    public UserRole Role { get; set; }
    
    // Audit fields (simplified for join table, or full IAuditable if needed)
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
```

**Step 2: Update User and Lab Models**

Modify: `shared/Models/User.cs`
- Remove `public UserRole Role { get; set; }`
- Remove `public Guid LabId { get; set; }`
- Remove `public Lab Lab { get; init; }`
- Add `public ICollection<UserLab> UserLabs { get; init; } = [];`

Modify: `shared/Models/Lab.cs`
- Change `public ICollection<User> Users` to `public ICollection<UserLab> UserLabs { get; init; } = [];`

**Step 3: Configure Many-to-Many in DbContext**

Modify: `backend/src/Quater.Backend.Data/QuaterDbContext.cs`
- Add `public DbSet<UserLab> UserLabs { get; set; }`
- In `OnModelCreating`:
```csharp
modelBuilder.Entity<UserLab>(entity =>
{
    entity.HasKey(e => new { e.UserId, e.LabId });
    entity.HasOne(e => e.User)
        .WithMany(u => u.UserLabs)
        .HasForeignKey(e => e.UserId);
    entity.HasOne(e => e.Lab)
        .WithMany(l => l.UserLabs)
        .HasForeignKey(e => e.LabId);
    entity.Property(e => e.Role).HasConversion<string>();
});
```

**Step 4: Create Migration for Schema Change**

Run:
```bash
dotnet ef migrations add IntroduceUserLabTable --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api
```

**Step 5: Apply Migration**

Run:
```bash
dotnet ef database update --project backend/src/Quater.Backend.Data --startup-project backend/src/Quater.Backend.Api
```

---

## Task 2: Implement Postgres RLS Policies

**Files:**
- Modify: `backend/src/Quater.Backend.Data/Migrations/XXXX_AddUserLabAndRLS.cs` (Add raw SQL for RLS)

**Context:** We need to enable RLS on sensitive tables (`Samples`, `TestResults`, `Parameters` if lab-specific). The migration in Task 1 creates the tables; we must append SQL to enable RLS.

**Step 1: Add RLS SQL to Migration**

Modify the generated migration `Up` method to include:

```csharp
// Enable RLS on Samples
migrationBuilder.Sql("ALTER TABLE \"Samples\" ENABLE ROW LEVEL SECURITY;");
migrationBuilder.Sql("ALTER TABLE \"TestResults\" ENABLE ROW LEVEL SECURITY;");

// Create Policy: Users can only see rows where LabId matches app.current_lab_id
// We cast current_setting to uuid safely. 
// If app.current_lab_id is null/empty, no rows are returned (secure default).
// Exception: System Admin (handled via separate policy or BYPASSRLS user role in DB - but here we use app logic often. 
// Better: The policy allows access if app.current_user_is_admin = 'true' OR LabId = current_lab_id)

var createPolicySql = @"
CREATE POLICY lab_isolation_policy ON ""Samples""
    USING (""LabId"" = NULLIF(current_setting('app.current_lab_id', true), '')::uuid);

CREATE POLICY lab_isolation_policy ON ""TestResults""
    USING (""SampleId"" IN (SELECT ""Id"" FROM ""Samples"")); -- Inherit from Sample
";

migrationBuilder.Sql(createPolicySql);
```

*Refinement:* For `TestResults`, if they don't have `LabId` directly, they rely on `Sample`. This requires a join policy which can be slow. *Recommendation:* Denormalize `LabId` to `TestResults` or rely on `Sample` being filtered first? RLS checks every row. Better to keep RLS simple. For now, assume `Samples` has `LabId`.

**Step 2: Verify Migration SQL**

Review generated SQL to ensure it executes without error.

---

## Task 3: Context-Aware Lab Middleware

**Files:**
- Create: `backend/src/Quater.Backend.Api/Middleware/LabContextMiddleware.cs`
- Modify: `backend/src/Quater.Backend.Api/Program.cs`
- Create: `backend/src/Quater.Backend.Core/Interfaces/ILabContextAccessor.cs`

**Goal:** Intercept requests, read `X-Lab-Id`, validate user access via `UserLab`, and set the context.

**Step 1: Define Interface**

Create: `backend/src/Quater.Backend.Core/Interfaces/ILabContextAccessor.cs`
```csharp
public interface ILabContextAccessor
{
    Guid? CurrentLabId { get; }
    UserRole? CurrentRole { get; }
    void SetContext(Guid labId, UserRole role);
}
```

**Step 2: Implement Middleware**

Create: `backend/src/Quater.Backend.Api/Middleware/LabContextMiddleware.cs`
```csharp
public class LabContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILabContextAccessor labContext, QuaterDbContext db)
    {
        var labIdHeader = context.Request.Headers["X-Lab-Id"].ToString();
        var userId = context.User.GetUserId(); // Ext method

        if (!string.IsNullOrEmpty(labIdHeader) && Guid.TryParse(labIdHeader, out var labId) && userId != null)
        {
            // Check UserLab table
            // Note: Caching recommended here for performance (Redis/Memory)
            var userLab = await db.UserLabs.FindAsync(userId, labId);
            
            if (userLab != null)
            {
                labContext.SetContext(labId, userLab.Role);
            }
            else 
            {
                 // User claims they are in Lab X, but aren't member -> 403
                 context.Response.StatusCode = 403;
                 return;
            }
        }
        
        await next(context);
    }
}
```

**Step 3: Register Middleware**

Modify `Program.cs` to add `app.UseMiddleware<LabContextMiddleware>()` after Authentication but before Authorization.

---

## Task 4: Database Session Interceptor for RLS

**Files:**
- Create: `backend/src/Quater.Backend.Data/Interceptors/RlsSessionInterceptor.cs`
- Modify: `backend/src/Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs`

**Goal:** Before EF Core sends any command to Postgres, inject `SET app.current_lab_id = '...'`.

**Step 1: Create Interceptor**

Create: `backend/src/Quater.Backend.Data/Interceptors/RlsSessionInterceptor.cs`
```csharp
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

public class RlsSessionInterceptor(ILabContextAccessor labContext) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken result)
    {
        if (labContext.CurrentLabId.HasValue)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SET app.current_lab_id = '{labContext.CurrentLabId}';";
            await cmd.ExecuteNonQueryAsync(result);
        }
    }
}
```

**Step 2: Register Interceptor**

Modify `ServiceCollectionExtensions.cs` (AddDatabaseServices) to register `RlsSessionInterceptor` and add it to `options.AddInterceptors(...)`.

---

## Task 5: System Admin Handling

**Files:**
- Modify: `backend/src/Quater.Backend.Services/CurrentUserService.cs`

**Goal:** System User (Admin) bypasses checks or has global access.
- If `UserId == SystemUser.Id`, skip UserLab check in Middleware.
- In RLS Interceptor, if System User, set `app.current_lab_id` to a value that policy ignores OR set a separate `app.is_admin = true` variable and update RLS policy to `USING (app.is_admin = 'true' OR "LabId" = ...)`

**Step 1: Update RLS Policy for Admin**

Modify migration SQL to:
```sql
USING (current_setting('app.is_system_admin', true) = 'true' OR "LabId" = NULLIF(current_setting('app.current_lab_id', true), '')::uuid)
```

**Step 2: Update Interceptor for Admin**

Update `RlsSessionInterceptor` to set `app.is_system_admin` based on user identity.

---

## Execution Handoff
Plan complete and saved to `docs/plans/2026-02-07-single-tenant-migration.md`. Two execution options:

1. **Subagent-Driven (this session)** - I dispatch fresh subagent per task.
2. **Parallel Session** - Open new session to execute.

Which approach?

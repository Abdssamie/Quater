# Registration Security Fix Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix privilege escalation vulnerability where users can self-assign Admin role during registration.

**Architecture:** Remove Role field from RegisterRequest DTO and hard-code all self-registered users to Viewer role. Only admins can create users with elevated roles via UsersController.

**Tech Stack:** ASP.NET Core 10, ASP.NET Core Identity, C# 13

---

## Prerequisites

- .NET 10 SDK installed
- PostgreSQL running (for tests)
- Docker running (for Testcontainers in tests)

---

## Task 1: Fix Registration Role Escalation

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Controllers/RegistrationController.cs:60-67,107-122`
- Modify: `backend/tests/Quater.Backend.Api.Tests/Controllers/RegistrationControllerTests.cs`

**Context:** Currently users can self-assign any role (including Admin) during registration by setting the `Role` field in the request. This is a critical privilege escalation vulnerability.

**Step 1: Write failing test for role enforcement**

Modify: `backend/tests/Quater.Backend.Api.Tests/Controllers/RegistrationControllerTests.cs`

Add this test to the existing test class:

```csharp
[Fact]
public async Task Register_AlwaysCreatesViewerRole()
{
    // Arrange - Try to register without specifying role
    var request = new
    {
        Email = "newuser@test.com",
        Password = "Password123!",
        LabId = _testLab.Id
        // Note: We're NOT sending Role in the request
        // The server should default to Viewer
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/registration/register", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    // Verify user was created with Viewer role
    await _fixture.ExecuteServiceAsync<UserManager<User>>(async userManager =>
    {
        var user = await userManager.FindByEmailAsync("newuser@test.com");
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Viewer, "all self-registered users should be Viewer");
    });
}
```

**Step 2: Run test to verify it fails**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~RegistrationControllerTests.Register_AlwaysCreatesViewerRole" -v normal
```

Expected: FAIL - Test expects Viewer role but current code requires Role in request

**Step 3: Remove Role from RegisterRequest DTO**

Modify: `backend/src/Quater.Backend.Api/Controllers/RegistrationController.cs:107-122`

```csharp
/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lab ID is required")]
    public Guid LabId { get; set; }
    
    // ✅ Role removed - server will default to Viewer for security
    // Only admins can create users with elevated roles via UsersController
}
```

**Step 4: Hard-code role to Viewer in registration logic**

Modify: `backend/src/Quater.Backend.Api/Controllers/RegistrationController.cs:60-67`

```csharp
var user = new User
{
    UserName = request.Email,
    Email = request.Email,
    Role = UserRole.Viewer,  // ✅ Always Viewer for self-registration
    LabId = request.LabId,
    IsActive = true,
};

var result = await _userManager.CreateAsync(user, request.Password);

if (!result.Succeeded)
{
    return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
}

_logger.LogInformation("User {Email} registered successfully with role {Role}", request.Email, UserRole.Viewer);
```

**Step 5: Run test to verify it passes**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~RegistrationControllerTests.Register_AlwaysCreatesViewerRole" -v normal
```

Expected: PASS - User is created with Viewer role

**Step 6: Update existing registration tests**

Modify: `backend/tests/Quater.Backend.Api.Tests/Controllers/RegistrationControllerTests.cs`

Find all tests that send `Role` in the request and remove it. Search for `Role = UserRole` and update each occurrence.

Example changes:

```csharp
// Before:
var request = new
{
    Email = "test@example.com",
    Password = "Password123!",
    Role = UserRole.Technician,  // ❌ Remove this
    LabId = testLab.Id
};

// After:
var request = new
{
    Email = "test@example.com",
    Password = "Password123!",
    LabId = testLab.Id
    // Role is not sent - server defaults to Viewer
};
```

Also update assertions that check the role in the response:

```csharp
// Before:
result.role.Should().Be("Technician");

// After:
result.role.Should().Be("Viewer");
```

**Step 7: Run all registration tests**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~RegistrationControllerTests" -v normal
```

Expected: PASS - All tests pass with updated request format

**Step 8: Commit**

```bash
git add backend/src/Quater.Backend.Api/ControllersstrationController.cs
git add backend/tests/Quater.Backend.Api.Tests/Controllers/RegistrationControllerTests.cs
git commit -m "fix: prevent role escalation in self-registration

- Remove Role field from RegisterRequest DTO
- Hard-code all self-registered users to Viewer role
- Only admins can create users with elevated roles via UsersController
- Add test to verify role enforcement
- Update existing tests to match new request format"
```

---

## Summary

**Completed:**
- ✅ Fixed privilege escalation vulnerability in registration
- ✅ All self-registered users now default to Viewer role
- ✅ Updated all tests to match new behavior

**Files Modified:**
- `backend/src/Quater.Backend.Api/Controllers/RegistrationController.cs`
- `backend/tests/Quater.Backend.Api.Tests/Controllers/RegistrationControllerTests.cs`

**Tests Added:** 1 test
**Commits:** 1 commit

**Next Steps:**
- Proceed to Plan 3: Claim Type Fixes

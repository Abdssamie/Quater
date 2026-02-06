# Login Security Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix timing attack vulnerability and add rate limiting to the login endpoint to prevent user enumeration and brute-force attacks.

**Architecture:** Move IsActive check after password validation to ensure constant-time authentication. Implement Redis-based rate limiting since `[EndpointRateLimit]` attribute doesn't work on Razor Pages.

**Tech Stack:** ASP.NET Core 10, ASP.NET Core Identity, Redis, C# 13

---

## Prerequisites

- .NET 10 SDK installed
- Redis running (for rate limiting)
- PostgreSQL running (for tests)
- Docker running (for Testcontainers in tests)

---

## Task 1: Fix Timing Attack in Login Page

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Pages/Account/Login.cshtml.cs:39-86`
- Create: `backend/tests/Quater.Backend.Api.Tests/Pages/LoginPageTests.cs`

**Context:** Currently the login page checks `IsActive` before password validation, creating a timing side-channel that allows user enumeration. Attackers can distinguish between non-existent users (fast), inactive users (fast), and active users with wrong passwords (slow due to bcrypt).

**Step 1: Write failing test for timing attack mitigation**

Create: `backend/tests/Quater.Backend.Api.Tests/Pages/LoginPageTests.cs`

```csharp
using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Api.Tests.Pages;

[Collection("Api")]
public sealed class LoginPageTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private Lab _testLab = null!;

    public LoginPageTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Create test lab
        _testLab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Test Lab",
            Location = "Test Location",
            ContactInfo = "test@lab.com",
            IsActive = true
        };

        await _fixture.ExecuteDbContextAsync(async context =>
        {
            context.Labs.Add(_testLab);
            await context.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_InactiveUser_HasSimilarTimingToWrongPassword()
    {
        // Arrange - Create active user
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "active@test.com",
            Email = "active@test.com",
            EmailConfirmed = true,
            Role = UserRole.Viewer,
            LabId = _testLab.Id,
            IsActive = true
        };

        await _fixture.ExecuteServiceAsync<UserManager<User>>(async userManager =>
        {
            await userManager.CreateAsync(activeUser, "Password123!");
        });

        // Deactivate user
        await _fixture.ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.FindAsync(activeUser.Id);
            user!.IsActive = false;
            await context.SaveChangesAsync();
        });

        // Act - Measure timing for inactive user
        var sw1 = Stopwatch.StartNew();
        var response1 = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "active@test.com",
            ["Password"] = "WrongPassword123!"
        }));
        sw1.Stop();

        // Act - Measure timing for wrong password on active user (re-activate first)
        await _fixture.ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.FindAsync(activeUser.Id);
            user!.IsActive = true;
            await context.SaveChangesAsync();
        });

        var sw2 = Stopwatch.StartNew();
        var response2 = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "active@test.com",
            ["Password"] = "WrongPassword123!"
        }));
        sw2.Stop();

        // Assert - Timing should be similar (within 100ms)
        var timingDifference = Math.Abs(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds);
        timingDifference.Should().BeLessThan(100, "timing attack mitigation should make inactive user and wrong password take similar time");
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsGenericError()
    {
        // Arrange
        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "inactive@test.com",
            Email = "inactive@test.com",
            EmailConfirmed = true,
            Role = UserRole.Viewer,
            LabId = _testLab.Id,
            IsActive = false
        };

        await _fixture.ExecuteServiceAsync<UserManager<User>>(async userManager =>
        {
            await userManager.CreateAsync(inactiveUser, "Password123!");
        });

        // Act
        var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "inactive@test.com",
            ["Password"] = "Password123!"
        }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Returns page with error
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password"); // Generic error message
    }
}
```

**Step 2: Run test to verify it fails**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~LoginPageTests" -v normal
```

Expected: FAIL - Timing test fails because IsActive check happens before password validation.

**Step 3: Implement timing attack mitigation**

Modify: `backend/src/Quater.Backend.Api/Pages/Account/Login.cshtml.cs`

Replace the `OnPostAsync` method (lines 39-86):

```csharp
public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
{
    ReturnUrl = returnUrl;

    if (!ModelState.IsValid)
    {
        return Page();
    }

    // ✅ FIXED: Validate password FIRST (constant-time for all users)
    // This ensures that non-existent, inactive, and active users all go through
    // the expensive bcrypt password hashing, preventing timing attacks
    var result = await signInManager.PasswordSignInAsync(
        Email,  // Use email directly (SignInManager will look up user)
        Password,
        isPersistent: false,
        lockoutOnFailure: true);

    if (!result.Succeeded)
    {
        // Handle all authentication failures with generic message
        if (result.IsLockedOut)
        {
            logger.LogWarning("Login failed: User {Email} is locked out", Email);
            ErrorMessage = "Account is locked. Please try again later.";
        }
        else if (result.IsNotAllowed)
        {
            logger.LogWarning("Login failed: User {Email} sign-in is not allowed", Email);
            ErrorMessage = "Login is not allowed. Please verify your email.";
        }
        else
        {
            logger.LogWarning("Login failed for user {Email}", Email);
            ErrorMessage = "Invalid email or password.";
        }
        
        return Page();
    }

    // ✅ Only after successful password verification, check IsActive
    // At this point, we know the password is correct, so checking IsActive
    // doesn't leak timing information
    var user = await userManager.FindByEmailAsync(Email);
    if (user is null || !user.IsActive)
    {
        // User authenticated but is inactive - revoke the session immediately
        await signInManager.SignOutAsync();
        logger.LogWarning("Login denied: User {Email} is inactive", Email);
        ErrorMessage = "Invalid email or password."; // Generic message
        return Page();
    }

    logger.LogInformation("User {Email} signed in via login page for OAuth2 flow", Email);
    return LocalRedirect(returnUrl ?? "/");
}
```

**Step 4: Run test to verify it passes**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~LoginPageTests" -v normal
```

Expected: PASS - Both tests pass, timing difference is < 100ms

**Step 5: Commit**

```bash
git add backend/src/Quater.Backend.Api/Pages/Account/Login.cshtml.cs
git add backend/tests/Quater.Backend.Api.Tests/Pages/LoginPageTests.cs
git commit -m "fix: mitigate timing attack in login page

- Move IsActive check after password validation
- Ensures constant-time authentication for all users
- Prevents user enumeration via timing side-channel
- Add tests to verify timing attack mitigation"
```

---

## Task 2: Add Rate Limiting to Login Endpoint

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Pages/Account/Login.cshtml.cs:16-19,39-46`
- Modify: `backend/tests/Quater.Backend.Api.Tests/Pages/LoginPageTests.cs`

**Context:** The login page has no rate limiting, making it vulnerable to brute-force attacks. The `[EndpointRateLimit]` attribute only works on controller actions, not Razor Pages, so we need to implement manual rate limiting using Redis.

**Step 1: Write failing test for rate limiting**

Modify: `backend/tests/Quater.Backend.Api.Tests/Pages/LoginPageTests.cs`

Add this test to the existing `LoginPageTests` class:

```csharp
[Fact]
public async Task Login_ExceedsRateLimit_ReturnsError()
{
    // Arrange - Create test user
    var testUser = new User
    {
        Id = Guid.NewGuid(),
        UserName = "ratelimit@test.com",
        Email = "ratelimit@test.com",
        EmailConfirmed = true,
        Role = UserRole.Viewer,
        LabId = _testLab.Id,
        IsActive = true
    };

    await _fixture.ExecuteServiceAsync<UserManager<User>>(async userManager =>
    {
        await userManager.CreateAsync(testUser, "Password123!");
    });

    // Act - Attempt login 6 times (limit is 5)
    HttpResponseMessage? lastResponse = null;
    for (int i = 0; i < 6; i++)
    {
        lastResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "ratelimit@test.com",
            ["Password"] = "WrongPassword!"
        }));
    }

    // Assert - 6th attempt should be rate limited
    lastResponse.Should().NotBeNull();
    var content = await lastResponse!.Content.ReadAsStringAsync();
    content.Should().Contain("Too many login attempts");
}
```

**Step 2: Run test to verify it fails**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~LoginPageTests.Login_ExceedsRateLimit_ReturnsError" -v normal
```

Expected: FAIL - No rate limiting implemented yet

**Step 3: Add Redis dependency to Login page**

Modify: `backend/src/Quater.Backend.Api/Pages/Account/Login.cshtml.cs`

Add using statement at the top:

```csharp
using StackExchange.Redis;
```

Update the constructor (lines 16-19):

```csharp
[AllowAnonymous]
public sealed class LoginModel(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    ILogger<LoginModel> logger,
    IConnectionMultiplexer redis) : PageModel  // ✅ Add Redis dependency
{
    private const int MaxLoginAttempts = 5;
    private const int WindowMinutes = 15;
```

**Step 4: Implement rate limiting check**

Modify: `backend/src/Quater.Backend.Api/Pages/Account/Login.cshtml.cs`

Add rate limiting at the start of `OnPostAsync` (after ModelState validation):

```csharp
public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
{
    ReturnUrl = returnUrl;

    if (!ModelState.IsValid)
    {
        return Page();
    }

    // ✅ Rate limiting check
    var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var rateLimitKey = $"login-ratelimit:{clientIp}";
    
    var db = redis.GetDatabase();
    var attempts = await db.StringIncrementAsync(rateLimitKey);
    
    if (attempts == 1)
    {
        // First attempt - set expiration
        await db.KeyExpireAsync(rateLimitKey, TimeSpan.FromMinutes(WindowMinutes));
    }
    
    if (attempts > MaxLoginAttempts)
    {
        l.LogWarning("Rate limit exceeded for IP {IpAddress} on login endpoint", clientIp);
        ErrorMessage = $"Too many login attempts. Please try again in {WindowMinutes} minutes.";
        return Page();
    }

    // ... rest of login logic (password validation, IsActive check, etc.)
```

**Step 5: Run test to verify it passes**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~LoginPageTests.Login_ExceedsRateLimit_ReturnsError" -v normal
```

Expected: PASS - 6th login attempt is rate limited

**Step 6: Run all login tests**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~LoginPageTests" -v normal
```

Expected: PASS - All 3 tests pass

**Step 7: Commit**

```bash
git add backend/src/Quater.Backend.Api/Pages/Account/Login.cshtml.cs
git add backend/tests/Quater.Backend.Api.Tests/Pages/LoginPageTests.cs
git commit -m "feat: add rate limiting to login endpoint

- Limit to 5 attempts per IP per 15 minutes
- Use Redis for distributed rate limiting
- Manual implementation since [EndpointRateLimit] doesn't work on Razor Pages
- Add test to verify rate limiting works"
```

---

## Summary

**Completed:**
- ✅ Fixed timing attack vulnerability in login page
- ✅ Added rate limiting to prevent brute-force attacks
- ✅ Added comprehensive tests for both fixes

**Files Modified:**
- `backend/src/Quater.Backend.Api/Pages/Account/Login.cshtml.cs`
- `backend/tests/Quater.Backend.Api.Tests/Pages/LoginPageTests.cs` (new)

**Tests Added:** 3 tests
**Commits:** 2 commits

**Next Steps:**
- Proceed to Plan 2: Registration Security Fix

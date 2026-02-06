# Authorization Infrastructure Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add fallback authorization policy and fix exception handling in AuthorizationController to improve security defaults and OAuth2 compliance.

**Architecture:** Configure fallback policy requiring authentication by default (defense-in-depth). Replace generic exceptions with proper OAuth2 error responses in authorization endpoint.

**Tech Stack:** ASP.NET Core 10, OpenIddict, C# 13

---

## Prerequisites

- .NET 10 SDK installed
- PostgreSQL running (for tests)
- Docker running (for Testcontainers in tests)

---

## Task 1: Add Fallback Authorization Policy

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs:363`
- Create: `backend/tests/Quater.Backend.Api.Tests/Authorization/FallbackPolicyTests.cs`

**Context:** Currently, endpoints without `[Authorize]` attribute are publicly accessible by default. A fallback policy requiring authentication makes the system secure by default - new endpoints must explicitly use `[AllowAnonymous]` to be public.

**Step 1: Write failing test for fallback policy**

Create: `backend/tests/Quater.Backend.Api.Tests/Authorization/FallbackPolicyTests.cs`

```csharp
using System.Net;
using FluentAssertions;
using Quater.Backend.Api.Tests.Fixtures;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

[Collection("Api")]
public sealed class FallbackPolicyTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public FallbackPolicyTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthentication_Returns401()
    {
        // Arrange - Use an endpoint that requires authentication
        
        // Act
        var response = await _client.GetAsync("/api/samples");

        // Assert - Should return 401 Unauthorized due to fallback policy
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousEndpoint_WithoutAuthentication_ReturnsSuccess()
    {
        // Arrange - Use an endpoint with [AllowAnonymous]
        
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert - Should return  OK because it has [AllowAnonymous]
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

**Step 2: Run test to verify current behavior**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~FallbackPolicyTests" -v normal
```

Expected: May pass or fail depending on current configuration

**Step 3: Add fallback authorization policy**

Modify: `backend/src/Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs`

Add after the existing policies (around line 363):

```csharp
    // Fallback policequire authentication by default
    // Endpoints must explicitly use [AllowAnonymous] to be public
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

**Step 4: Run test to verify it passes**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~FallbackPolicyTests" -v normal
```

Expected: PASS - Both tests pass

**Step 5: Run all tests**

```bash
cd backend
dotnet test backend/Quater.Backend.sln -v normal
```

Expected: PASS ll tests still pass

**Step 6: Commit**

```bash
git add backend/src/Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs
git add backend/tests/Quater.Backend.Api.Tests/Authorization/FallbackPolicyTests.cs
git commit -m "feat: add fallback authorization policy requiring authentication

- Configure FallbackPolicy to require authenticated users by default
- Endpoints must explicitly use [AllowAnonymous] to be public
- Defense-in-depth: prevents accidental exposure of protected endpoints
- Add tests to verify fallback policy works correctly"
```

---

## Task 2: Fix Exception Handling in AuthorizationController

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Controllers/AuthorizationController.cs:84-101`
- Modify: `backend/tests/Quater.Backend.Api.Tests/Controllers/AuthorizationCodeFlowTests.cs`

**Context:** The authorization controller throws a generic InvalidOperationException if the authenticated user cannot be found in the database. This should return a proper OAuth2 error response instead.

**Step 1: Write failing test**

Modify: `backend/tests/Quater.Backend.Api.Tests/Controllers/AuthorizationCodeFlowTests.cs`

Add this test:

```csharp
[Fact]
public async Task Authorize_UserNotFoundInDatabase_ReturnsOAuthError()
{
    // Arrange - Create user, log in, then delete from database
    var testUser = new User
    {
        Id = Guid.NewGuid(),
        UserName = "deleted@test.com",
        Email = "deleted@test.com",
        EmailConfirmed = true,
        Role = UserRole.Viewer,
        LabId = _testLab.Id,
        IsActive = true
    };

    await _fixture.ExecuteServiceAsync<UserManager<User>>(async userManager =>
    {
        await userManager.CreateAsync(testUser, "Password123!");
    });

    var cookieClient = await AuthenticationHelper.LoginViaCookieAsync(
        _fixture,
        "deleted@test.com",
        "Password123!");

    await _fixture.ExecuteDbContextAsync(async context =>
    {
        var user = await context.Users.FindAsync(testUser.Id);
        context.Users.Remove(user!);
        await context.SaveChangesAsync();
    });

    // Act
    var (codeVerifier, codeChallenge) = AuthenticationHelper.GeneratePkceParameters();
    var authorizeUrl = "/api/auth/authorize?" +
        "response_type=code&" +
        "client_id=quater-mobile-client&" +
        "redirect_uri=quater://oauth/callback&" +
        $"code_challenge={codeChallenge}&" +
        "code_challenge_method=S256&" +
        "scope=openid profile email";

    var response = await cookient.GetAsync(authorizeUrl);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
    var location = response.Headers.Location?.ToString();
    location.Should().NotBeNull();
    location.Should().Contain("error=server_error");
}
```

**Step 2: Run test to verify it fails**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~AuthorizationCodeFlowTests.Authorize_UserNotFoundInDatabase_ReturnsOAuthError" -v normal
```

Expected: FAIL - Throws InvalidOperationException

**Step 3: Replace exception wituth error response**

Modify: `backend/src/Quater.Backend.Api/Controllers/AuthorizationController.cs`

Replace lines 84-101:

```csharp
// User is authenticated - look up the user to get current claims.
var user = await _userManager.GetUserAsync(result.Principal);
if (user is null)
{
    _logger.LogError(
        "Authenticated user could not be found in database. Principal: {Principal}",
        result.Principal.Identity?.Name);
    
    return Forbid(
        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        properties: new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.ServerError,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "An internal error occurred during authentication."
        }));
}

// Verify the user account is still active.
if (!user.IsActive)
{
    _logger.LogWarning(
        "Authorization denied: User {UserId} is inactive",
        user.Id);

    return Forbid(
        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        properties: new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.AccessDenied,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user account is inactive."
        }));
}
```

**Step 4: Run test to verify it passes**

```bash
cd backend
dotnet test tests/Quater.Backend.Api.Tests/Quater.Backend.Api.Tests.csproj --filter "FullyQualifiedName~AuthorizationCodeFlowTests.Authorize_UserNotFoubase_ReturnsOAuthError" -v normal
```

Expected: PASS

**Step 5: Commit**

```bash
git add backend/src/Quater.Backend.Api/Controllers/AuthorizationController.cs
git add backend/tests/Quater.Backend.Api.Tests/Controllers/AuthorizationCodeFlowTests.cs
git commit -m "fix: return OAuth error instead of throwing exception in authorize endpoint

- Replace InvalidOperationException with proper OAuth2 error response
- Return server_error when authenticated user not found in database
- Add test for edge case (user deleted after authentication)
- Follows OAuth2 error response standards (RFC 6749)"
```

---

## Summary

**Completed:**
- Added fallback authorization policy requiring authentication by default
- Fixed exception handling in AuthorizationController to return OAuth2 errors
- Added tests for both improvements

**Files Modified:**
- `backend/src/Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs`
- `backend/src/Quater.Backend.Api/Controllers/AuthorizationController.cs`
- `backend/tests/Quater.Backend.Api.Tests/Authorization/FallbackPolicyTests.cs` (new)
- `backend/tests/Quater.Backend.Api.Tests/Controllers/AuthorizationCodeFlowTests.cs`

**Tests Added:** 3 tests
**Commits:** 2 commits

**Next Steps:**
- Proceed to Plan 5: Multi-Tenancy Foundation (UserLab model)

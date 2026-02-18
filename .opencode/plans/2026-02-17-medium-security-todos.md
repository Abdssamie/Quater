# Medium Priority Security TODOs Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix three MEDIUM priority security TODOs: (1) VersionController information disclosure, (2) RateLimitingMiddleware fail-open behavior when Redis is down, (3) PasswordController timing inconsistency.

**Architecture:** Implement fail-closed security model for rate limiting by returning 503 Service Unavailable when Redis is unavailable. Add authentication to VersionController. Add constant-time delay to ResetPassword to prevent timing attacks.

**Tech Stack:** ASP.NET Core 10, Redis, xUnit, FluentAssertions, Moq

---

## Summary of Changes

### 1. VersionController - Information Disclosure (MEDIUM)
**Location:** `backend/src/Quater.Backend.Api/Controllers/VersionController.cs:8`

**Issue:** The `/api/version` endpoint is unauthenticated and publicly exposes:
- Application version
- Build date
- Environment name

**Risk:** Information disclosure that could help attackers identify specific versions with known vulnerabilities.

**Solution:** Add `[Authorize(Policy = Policies.ViewerOrAbove)]` attribute to require authentication.

### 2. RateLimitingMiddleware - Fail-Open When Redis Down (MEDIUM)
**Location:** `backend/src/Quater.Backend.Api/Middleware/RateLimitingMiddleware.cs:166`

**Issue:** Currently "fails open" (allows requests) when Redis is unavailable. This removes all rate limiting protection during Redis outages.

**Risk:** No brute force protection during Redis outage; attackers could exploit this window.

**Solution:** Change to "fail-closed" by returning HTTP 503 Service Unavailable when Redis is down. This ensures security is maintained even if it causes temporary service disruption.

### 3. PasswordController - Timing Inconsistency (MEDIUM)
**Location:** `backend/src/Quater.Backend.Api/Controllers/PasswordController.cs:129`

**Issue:** `ForgotPassword` has 200ms constant-time delay to prevent timing attacks, but `ResetPassword` immediately returns for non-existent users (line 127-132).

**Risk:** Attackers could enumerate valid email addresses by measuring response times.

**Solution:** Add the same 200ms constant-time delay to `ResetPassword` endpoint.

---

## Task 1: VersionController - Add Authentication

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Controllers/VersionController.cs:1-42`
- Create: `backend/tests/Quater.Backend.Api.Tests/Controllers/VersionControllerTests.cs`

**Step 1: Add using statements and authorization attribute**

Add to the top of the file:
```csharp
using Quater.Backend.Core.Constants;
```

Add `[Authorize]` attribute to the controller class (replacing the TODO comment):
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewerOrAbove)]
public class VersionController : ControllerBase
```

**Step 2: Remove TODO comment**

Remove lines 8-9:
```csharp
// TODO: MEDIUM - Endpoint is unauthenticated and publicly exposes version/environment info.
// Risk: Information disclosure. Consider adding [Authorize] or removing sensitive details.
```

**Step 3: Write failing integration test**

Create `backend/tests/Quater.Backend.Api.Tests/Controllers/VersionControllerTests.cs`:

```csharp
using System.Net;
using FluentAssertions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Quater.Backend.Api.Tests.Controllers;

[Collection("Api")]
public sealed class VersionControllerTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture = fixture;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _client = _fixture.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Get_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_AuthenticatedViewer_ReturnsOkWithVersionInfo()
    {
        // Arrange
        var (user, password) = await CreateTestUserAsync("version-test@test.com", "Password123!");
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, password);
        _client.AddAuthToken(tokenResponse.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<VersionResponse>();
        content.Should().NotBeNull();
        content!.Version.Should().NotBeNullOrEmpty();
        content.ApiVersion.Should().Be("v1");
    }

    private async Task<(User user, string password)> CreateTestUserAsync(string email, string password)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Quater.Backend.Data.QuaterDbContext>();

        var lab = new Quater.Shared.Models.Lab
        {
            Id = Guid.NewGuid(),
            Name = $"Test Lab for {email}",
            Location = "Test Location",
            ContactInfo = "test@test.com",
            IsActive = true
        };
        dbContext.Labs.Add(lab);
        await dbContext.SaveChangesAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            UserLabs = [new Quater.Shared.Models.UserLab { LabId = lab.Id, Role = Quater.Shared.Enums.UserRole.Viewer }],
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return (user, password);
    }
}

public class VersionResponse
{
    public string Version { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string BuildDate { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}
```

**Step 4: Run test to verify it fails**

Run:
```bash
dotnet test backend/tests/Quater.Backend.Api.Tests/ --filter "FullyQualifiedName~VersionControllerTests.Get_Unauthenticated_ReturnsUnauthorized" -v n
```

Expected: FAIL - Test expects 401 but gets 200 because controller is not yet protected

**Step 5: Implement authorization**

Edit `backend/src/Quater.Backend.Api/Controllers/VersionController.cs`:

```csharp
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quater.Backend.Core.Constants;

namespace Quater.Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewerOrAbove)]
public class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Get version from assembly (set by Directory.Build.props)
        var assemblyVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString();

        // Prefer environment variable (for Docker builds), fallback to assembly version
        var envVersion = Environment.GetEnvironmentVariable("APP_VERSION");

        if (string.IsNullOrWhiteSpace(envVersion) && string.IsNullOrWhiteSpace(assemblyVersion))
        {
            throw new InvalidOperationException(
                "Version information not found. Ensure APP_VERSION environment variable is set or assembly version is configured in Directory.Build.props");
        }

        var version = envVersion ?? assemblyVersion!;
        var buildDate = Environment.GetEnvironmentVariable("BUILD_DATE") ?? "unknown";

        return Ok(new
        {
            version,
            apiVersion = "v1",
            buildDate,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
        });
    }
}
```

**Step 6: Run tests to verify they pass**

Run:
```bash
dotnet test backend/tests/Quater.Backend.Api.Tests/ --filter "FullyQualifiedName~VersionControllerTests" -v n
```

Expected: PASS

**Step 7: Commit**

```bash
git add backend/src/Quater.Backend.Api/Controllers/VersionController.cs
git add backend/tests/Quater.Backend.Api.Tests/Controllers/VersionControllerTests.cs
git commit -m "security: require authentication for version endpoint

- Add [Authorize(Policy = Policies.ViewerOrAbove)] to VersionController
- Remove MEDIUM priority TODO comment
- Add integration tests for authenticated/unauthenticated access
- Prevents information disclosure of version/build/environment info

Closes TODO: VersionController.cs:8"
```

---

## Task 2: RateLimitingMiddleware - Fail-Closed When Redis Down

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Middleware/RateLimitingMiddleware.cs:164-189, 283-299`
- Create: `backend/tests/Quater.Backend.Api.Tests/Middleware/RateLimitingMiddlewareTests.cs`

**Step 1: Analyze current fail-open behavior**

The middleware currently catches Redis exceptions and calls `await _next(context)` (lines 164-189), allowing the request to proceed.

**Step 2: Write failing test**

Create `backend/tests/Quater.Backend.Api.Tests/Middleware/RateLimitingMiddlewareTests.cs`:

```csharp
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Shared.Models;
using StackExchange.Redis;

namespace Quater.Backend.Api.Tests.Middleware;

[Collection("Api")]
public sealed class RateLimitingMiddlewareTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private HttpClient _client = null!;
    private Mock<IConnectionMultiplexer> _mockRedis = null!;

    public RateLimitingMiddlewareTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _client = _fixture.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_RedisConnectionException_ReturnsServiceUnavailable()
    {
        // Arrange - Create client with broken Redis
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is down"));

        var factory = _fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer>(_mockRedis.Object);
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Service temporarily unavailable");
    }

    [Fact]
    public async Task InvokeAsync_RedisTimeoutException_ReturnsServiceUnavailable()
    {
        // Arrange - Create client with timing-out Redis
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisTimeoutException("Redis timeout", CommandStatus.Unknown));

        var factory = _fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer>(_mockRedis.Object);
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task InvokeAsync_RedisWorking_NormalRequestSucceeds()
    {
        // Arrange - Use normal fixture with working Redis
        var (user, password) = await CreateTestUserAsync("ratelimit-test@test.com", "Password123!");
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, password);
        _client.AddAuthToken(tokenResponse.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-RateLimit-Limit");
        response.Headers.Should().ContainKey("X-RateLimit-Remaining");
    }

    private async Task<(User user, string password)> CreateTestUserAsync(string email, string password)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Quater.Backend.Data.QuaterDbContext>();

        var lab = new Quater.Shared.Models.Lab
        {
            Id = Guid.NewGuid(),
            Name = $"Test Lab for {email}",
            Location = "Test Location",
            ContactInfo = "test@test.com",
            IsActive = true
        };
        dbContext.Labs.Add(lab);
        await dbContext.SaveChangesAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            UserLabs = [new Quater.Shared.Models.UserLab { LabId = lab.Id, Role = Quater.Shared.Enums.UserRole.Viewer }],
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return (user, password);
    }
}
```

**Step 3: Run test to verify it fails**

Run:
```bash
dotnet test backend/tests/Quater.Backend.Api.Tests/ --filter "FullyQualifiedName~RateLimitingMiddlewareTests.InvokeAsync_RedisConnectionException_ReturnsServiceUnavailable" -v n
```

Expected: FAIL - Test expects 503 but gets 200 because middleware still fails open

**Step 4: Implement fail-closed behavior**

Edit `backend/src/Quater.Backend.Api/Middleware/RateLimitingMiddleware.cs`:

Change the catch blocks in `ApplyGlobalRateLimitAsync` method (lines 164-189):

```csharp
catch (RedisConnectionException ex)
{
    // FAIL CLOSED: Return 503 Service Unavailable when Redis is down
    // This ensures rate limiting is enforced even during Redis outages
    _logger.LogError(ex,
        "Redis connection error for client {ClientId}; failing closed (returning 503).",
        clientId);
    
    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Service temporarily unavailable",
        message = "Rate limiting service is currently unavailable. Please try again later."
    });
    return;
}
catch (RedisTimeoutException ex)
{
    // FAIL CLOSED: Return 503 on timeout
    _logger.LogError(ex,
        "Redis timeout for client {ClientId}; failing closed (returning 503).",
        clientId);
    
    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Service temporarily unavailable",
        message = "Rate limiting service timeout. Please try again later."
    });
    return;
}
catch (Exception ex)
{
    // FAIL CLOSED: Return 503 for any other errors
    _logger.LogError(ex,
        "Rate limiting error for client {ClientId}; failing closed (returning 503).",
        clientId);
    
    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Service temporarily unavailable",
        message = "An error occurred in rate limiting. Please try again later."
    });
    return;
}
```

Similarly, update the catch blocks in `ApplyEndpointRateLimitAsync` method (lines 283-299):

```csharp
catch (RedisConnectionException ex)
{
    // FAIL CLOSED
    _logger.LogError(ex,
        "Redis connection error for endpoint rate limit; failing closed (returning 503).");
    
    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Service temporarily unavailable",
        message = "Rate limiting service is currently unavailable. Please try again later."
    });
    return;
}
catch (RedisTimeoutException ex)
{
    // FAIL CLOSED
    _logger.LogError(ex,
        "Redis timeout for endpoint rate limit; failing closed (returning 503).");
    
    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Service temporarily unavailable",
        message = "Rate limiting service timeout. Please try again later."
    });
    return;
}
catch (Exception ex)
{
    // FAIL CLOSED
    _logger.LogError(ex,
        "Endpoint rate limiting error; failing closed (returning 503).");
    
    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Service temporarily unavailable",
        message = "An error occurred in rate limiting. Please try again later."
    });
    return;
}
```

**Step 5: Remove TODO comment**

Remove lines 166-167:
```csharp
// TODO: MEDIUM - Rate limiting fails open when Redis is down. Risk: No brute force protection during Redis outage.
// Consider failing closed for auth endpoints, or adding in-memory fallback rate limiting.
```

**Step 6: Run tests to verify they pass**

Run:
```bash
dotnet test backend/tests/Quater.Backend.Api.Tests/ --filter "FullyQualifiedName~RateLimitingMiddlewareTests" -v n
```

Expected: PASS

**Step 7: Commit**

```bash
git add backend/src/Quater.Backend.Api/Middleware/RateLimitingMiddleware.cs
git add backend/tests/Quater.Backend.Api.Tests/Middleware/RateLimitingMiddlewareTests.cs
git commit -m "security: fail closed when Redis is unavailable in rate limiting

- Change RateLimitingMiddleware from fail-open to fail-closed
- Return HTTP 503 Service Unavailable when Redis connection fails
- Ensures rate limiting protection is maintained during Redis outages
- Remove MEDIUM priority TODO comment
- Add comprehensive middleware tests for Redis failure scenarios

Closes TODO: RateLimitingMiddleware.cs:166"
```

---

## Task 3: PasswordController - Add Timing Protection to ResetPassword

**Files:**
- Modify: `backend/src/Quater.Backend.Api/Controllers/PasswordController.cs:121-162`
- Modify: `backend/tests/Quater.Backend.Api.Tests/Controllers/PasswordControllerTests.cs:438-456`

**Step 1: Analyze ForgotPassword timing protection**

The ForgotPassword endpoint has this pattern:
```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... logic ...
var elapsed = stopwatch.ElapsedMilliseconds;
var remainingDelay = 200 - (int)elapsed;
if (remainingDelay > 0)
{
    await Task.Delay(remainingDelay);
}
```

**Step 2: Write failing test for timing protection**

Add test to `backend/tests/Quater.Backend.Api.Tests/Controllers/PasswordControllerTests.cs` after line 489 (after SecurityAlertEmailFailure test):

```csharp
[Fact]
public async Task ResetPassword_NonExistentEmail_ReturnsBadRequestWithTimingProtection()
{
    // Arrange
    var request = new
    {
        Email = "nonexistent-reset@test.com",
        Code = "some-token",
        NewPassword = "NewPassword456!"
    };

    // Act
    var stopwatch = Stopwatch.StartNew();
    var response = await _client.PostJsonAsync("/api/password/reset", request);
    stopwatch.Stop();

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("Invalid request");

    // Verify timing attack protection (200ms delay)
    stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
}

[Fact]
public async Task ResetPassword_TimingAttackProtection_ConsistentResponseTime()
{
    // Arrange
    var (user, _) = await CreateTestUserAsync("timing-reset-exists@test.com", "OldPassword123!");
    var resetToken = await GeneratePasswordResetTokenAsync(user);

    var existingEmailRequest = new
    {
        Email = user.Email!,
        Code = resetToken,
        NewPassword = "NewPassword456!"
    };

    var nonExistentEmailRequest = new
    {
        Email = "nonexistent-reset2@test.com",
        Code = "some-token",
        NewPassword = "NewPassword456!"
    };

    // Act - Test existing email
    var stopwatch1 = Stopwatch.StartNew();
    var response1 = await _client.PostJsonAsync("/api/password/reset", existingEmailRequest);
    stopwatch1.Stop();

    // Act - Test non-existent email
    var stopwatch2 = Stopwatch.StartNew();
    var response2 = await _client.PostJsonAsync("/api/password/reset", nonExistentEmailRequest);
    stopwatch2.Stop();

    // Assert
    response1.StatusCode.Should().Be(HttpStatusCode.OK);
    response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    // Response times should be consistent (within 300ms) to prevent timing attacks
    var timeDifference = Math.Abs(stopwatch1.ElapsedMilliseconds - stopwatch2.ElapsedMilliseconds);
    timeDifference.Should().BeLessThan(300, "response times should be consistent to prevent timing attacks");

    // Both should have at least some processing time
    stopwatch1.ElapsedMilliseconds.Should().BeGreaterThan(0);
    stopwatch2.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
}
```

**Step 3: Run test to verify it fails**

Run:
```bash
dotnet test backend/tests/Quater.Backend.Api.Tests/ --filter "FullyQualifiedName~ResetPassword_NonExistentEmail_ReturnsBadRequestWithTimingProtection" -v n
```

Expected: FAIL - Test expects >= 200ms delay but gets faster response

**Step 4: Implement timing protection in ResetPassword**

Edit `backend/src/Quater.Backend.Api/Controllers/PasswordController.cs` - Replace the `ResetPassword` method (lines 118-162):

```csharp
/// <summary>
/// Reset password using a valid token
/// </summary>
[HttpPost("reset")]
[AllowAnonymous]
[EndpointRateLimit(10, 60, RateLimitTrackBy.Email)]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Start timing to ensure consistent response time regardless of email existence
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    var user = await _userManager.FindByEmailAsync(request.Email);
    if (user == null)
    {
        // Add constant-time delay to prevent timing attack vulnerability
        // This ensures response time is consistent regardless of whether email exists
        var elapsed = stopwatch.ElapsedMilliseconds;
        var remainingDelay = 200 - (int)elapsed;
        if (remainingDelay > 0)
        {
            await Task.Delay(remainingDelay);
        }

        return BadRequest(new { error = "Invalid request" });
    }

    var result = await _userManager.ResetPasswordAsync(user, request.Code, request.NewPassword);
    if (!result.Succeeded)
    {
        _logger.LogWarning("Password reset failed for user {Email}: {Errors}",
            request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
        return BadRequest(new { error = "Invalid or expired reset token" });
    }

    _logger.LogInformation("Password reset successfully for user {Email}", user.Email);

    // Send security alert email
    try
    {
        await AuthHelpers.SendSecurityAlertEmailAsync(
            user,
            "Password Reset",
            "Your password was successfully reset.",
            _emailQueue,
            _emailTemplateService);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send security alert email to {Email}", user.Email);
        // Don't fail password reset if alert email fails
    }

    return Ok(new { message = "Password reset successfully" });
}
```

**Step 5: Remove TODO comment**

Remove lines 129-131:
```csharp
// TODO: MEDIUM - Timing inconsistency. Forgot password has timing protection, reset doesn't.
// This immediately returns for non-existent users, potentially allowing timing-based email enumeration.
// Consider adding constant-time delay like ForgotPassword endpoint.
```

**Step 6: Run tests to verify they pass**

Run:
```bash
dotnet test backend/tests/Quater.Backend.Api.Tests/ --filter "FullyQualifiedName~PasswordControllerTests" -v n
```

Expected: PASS

**Step 7: Commit**

```bash
git add backend/src/Quater.Backend.Api/Controllers/PasswordController.cs
git add backend/tests/Quater.Backend.Api.Tests/Controllers/PasswordControllerTests.cs
git commit -m "security: add timing attack protection to ResetPassword endpoint

- Add 200ms constant-time delay to ResetPassword for non-existent users
- Makes response times consistent between existing and non-existent emails
- Prevents timing-based email enumeration attacks
- Aligns with existing protection in ForgotPassword endpoint
- Remove MEDIUM priority TODO comment
- Add timing protection tests for ResetPassword

Closes TODO: PasswordController.cs:129"
```

---

## Task 4: Final Verification

**Step 1: Run all security-related tests**

Run:
```bash
dotnet test backend/tests/Quater.Backend.Api.Tests/ --filter "FullyQualifiedName~VersionControllerTests|FullyQualifiedName~RateLimitingMiddlewareTests|FullyQualifiedName~PasswordControllerTests" -v n
```

Expected: All tests PASS

**Step 2: Build the project**

Run:
```bash
dotnet build backend/Quater.Backend.sln
```

Expected: Build succeeds with 0 errors, 0 warnings

**Step 3: Run the full test suite**

Run:
```bash
dotnet test backend/tests/Quater.Backend.Api.Tests/
```

Expected: All tests PASS (currently 75 tests)

**Step 4: Final commit**

```bash
git log --oneline -5
```

Verify all 3 commits are present with clear messages.

---

## Testing Summary

### VersionController Tests
- `Get_Unauthenticated_ReturnsUnauthorized` - Verifies 401 for unauthenticated requests
- `Get_AuthenticatedViewer_ReturnsOkWithVersionInfo` - Verifies 200 for authenticated requests

### RateLimitingMiddleware Tests
- `InvokeAsync_RedisConnectionException_ReturnsServiceUnavailable` - Verifies 503 when Redis connection fails
- `InvokeAsync_RedisTimeoutException_ReturnsServiceUnavailable` - Verifies 503 when Redis times out
- `InvokeAsync_RedisWorking_NormalRequestSucceeds` - Verifies normal operation with working Redis

### PasswordController Tests
- `ResetPassword_NonExistentEmail_ReturnsBadRequestWithTimingProtection` - Verifies >=200ms delay for non-existent emails
- `ResetPassword_TimingAttackProtection_ConsistentResponseTime` - Verifies consistent timing between existent/non-existent emails
- All existing PasswordController tests should continue to pass

---

## Security Impact

1. **VersionController**: Prevents information disclosure of application version, build date, and environment to unauthenticated attackers.

2. **RateLimitingMiddleware**: Ensures rate limiting protection is maintained even during Redis outages by failing closed (returning 503) rather than allowing unlimited requests.

3. **PasswordController**: Prevents timing-based email enumeration attacks by ensuring consistent response times for both existing and non-existent email addresses in the password reset flow.

All three fixes follow the principle of "fail-secure" - when security mechanisms fail, they default to the most secure state rather than the most permissive.

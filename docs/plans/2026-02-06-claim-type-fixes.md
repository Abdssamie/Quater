# Claim Type Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix claim type usage to follow OAuth2/OIDC standards by using OpenIddict constants instead of legacy ASP.NET claim types.

**Architecture:** Replace `ClaimTypes.NameIdentifier` with `OpenIddictConstants.Claims.Subject`, add LabId extraction methods, and ensure consistency across the codebase. This is a prerequisite for multi-tenancy implementation.

**Tech Stack:** ASP.NET Core 10, OpenIddict, C# 13

---

## Prerequisites

- .NET 10 SDK installed
- Understanding of OAuth2/OIDC claim standards

---

## Task 1: Fix ClaimsPrincipalExtensions

**Files:**
- Modify: `backend/src/Quater.Backend.Core/Extensions/ClaimsPrincipalExtensions.cs:1,18,39,49`
- Create: `backend/tests/Quater.Backend.Core.Tests/Extensions/ClaimsPrincipalExtensionsTests.cs`

**Context:** The extension methods use legacy ASP.NET claim types instead of OpenIddict constants. This works today due to OpenIddict's default claim mapping, but is fragile and violates OAuth2/OIDC standards.

**Step 1: Write failing tests for claim extraction**

Create: `backend/tests/Quater.Backend.Core.Tests/Extensions/ClaimsPrincipalExtensionsTests.cs`

```csharp
using System.Security.Claims;
using FluentAssertions;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Extensions;
using Xunit;

namespace Quater.Backend.Core.Tests.Extensions;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserIdOrThrow_WithSubjectClaim_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(OpenIddictConstants.Claims.Subject, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserIdOrThrow();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserIdOrThrow_WithoutSubjectClaim_ThrowsException()
    {
        // Arrange
        var principal = new ClaimsPrincipal();

        // Act & Assert
        var act = () => principal.GetUserIdOrThrow();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*User ID not found in claims*");
    }

    [Fact]
    public void GetUserEmail_WithEmailClaim_ReturnsEmail()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(OpenIddictConstants.Claims.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserEmail();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void GetUserRole_WithRoleClaim_ReturnsRole()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(QuaterClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserRole();

        // Assert
        result.Should().Be("Admin");
    }

    [Fact]
    public void GetLabIdOrThrow_WithLabIdClaim_ReturnsLabId()
    {
        // Arrange
        var labId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(QuaterClaimTypes.LabId, labId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetLabIdOrThrow();

        // Assert
        result.Should().Be(labId);
    }

    [Fact]
    public void GetLabIdOrThrow_WithoutLabIdClaim_ThrowsException()
    {
        // Arrange
        var principal = new ClaimsPrincipal();

        // Act & Assert
        var act = () => principal.GetLabIdOrThrow();
        act.Should().Throw<InvalException>()
            .WithMessage("*LabId not found in claims*");
    }

    [Fact]
    public void IsAdmin_WithAdminRole_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(QuaterClaimTypes.Role, Roles.Admin)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.IsAdmin();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_WithNonAdminRole_ReturnsFalse()
    {
        // Arrange
    r claims = new[]
        {
            new Claim(QuaterClaimTypes.Role, Roles.Viewer)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.IsAdmin();

        // Assert
        result.Should().BeFalse();
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
cd backend
dotnet test tests/Quater.Backend.Core.Tests/Quater.Backend.Core.Tests.csproj --filter "FullyQualifiedName~ClaimsPrincipalExtensionsTests" -v normal
```

Expected: FAIL - Tests fail because methods use wrong claim types

**Step 3: Fix ClaimsPrincipalExtensuse correct claim types**

Modify: `backend/src/Quater.Backend.Core/Extensions/ClaimsPrincipalExtensions.cs`

```csharp
using System.Security.Claims;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Constants;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the ClaimsPrincipal, or throws if not found.
    /// </summary>
    /// <param name="principal">Thelaims principal.</param>
    /// <returns>The user ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when user ID is not found in claims.</exception>
    public static Guid GetUserIdOrThrow(this ClaimsPrincipal principal)
    {
        var userIdString = principal.FindFirstValue(OpenIddictConstants.Claims.Subject);  // ✅ FIXED
        if (string.IsNullOrEmpty(userIdString))
        {
            throw new InvalidOperationException("User ID not found in claims. Ensure the user is authenticated.");
        }

        if (!Guid.TryParse(userIdString, out var userId))
        {
            throw new InvalidOperationException("User ID in claims is not a valid GUID.");
        }

        return userId;
    }

    /// <summary>
    /// Gets the user's email from the ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user's email, or null if not found.</returns>
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(OpenIddictConstants.Claims.Email);  // ✅ FIXED
    }

    /// <summary>
    /// Gets the user's role from the ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user's role, or null if not found.</returns>
    public static string? GetUserRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(QuaterClaimTypes.Role);  // ✅ FIXED
    }

    /// <summary>
    /// Gets the user's lab ID from the ClaimsPrincipal, or throws if not found.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The lab ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when lab ID is not found in claims.</exception>
    public static Guid GetLabIdOrT(this ClaimsPrincipal principal)  // ✅ NEW
    {
        var labIdString = principal.FindFirstValue(QuaterClaimTypes.LabId);
        if (string.IsNullOrEmpty(labIdString))
        {
            throw new InvalidOperationException("LabId not found in claims. Ensure the user is authenticated.");
        }

        if (!Guid.TryParse(labIdString, out var labId))
        {
            throw new InvalidOperationException("LabId in claims is not a valid GUID.");
        }

        return labId;
    }

    /// <summary>
    /// Checks if the current user is an admin.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>True if the user is an admin, false otherwise.</returns>
    public static bool IsAdmin(this ClaimsPrincipal principal)  // ✅ NEW
    {
        var role = principal.FindFirstValue(QuaterClaimTypes.Role);
        return role == Roles.Admin;
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
cd backend
dotnet test tests/Quater.Backend.Core.Tests/Quater.Backend.Core.Tests.csproj --filter "FullyQualifiedName~ClaimsPrincipalExtensionsTests" -v normal
```

Expected: PASS - All 8 tests pass

**St: Commit**

```bash
git add backend/src/Quater.Backend.Core/Extensions/ClaimsPrincipalExtensions.cs
git add backend/tests/Quater.Backend.Core.Tests/Extensions/ClaimsPrincipalExtensionsTests.cs
git commit -m "fix: use OpenIddict claim types instead of legacy ASP.NET types

- Replace ClaimTypes.NameIdentifier with OpenIddictConstants.Claims.Subject
- Replace ClaimTypes.Email with OpenIddictConstants.Claims.Email
- Replace ClaimTypes.Role with QuaterClaimTypes.Role
- Add GetLabIdOrThrow() method for multi-tenancy support
- Add IsAdmin() helper method
- Add comprehensive tests for all claim extraction methods"
```

---

## Task 2: Fix CurrentUserService

**Files:**
- Modify: `backend/src/Quater.Backend.Services/CurrentUserService.cs:1,26`
- Modify: `backend/src/Quater.Backend.Data/Interceptors/ICurrentUserService.cs:8`
- Create: `backend/tests/Quater.Backend.Core.Tests/Services/CurrentUserServiceTests.cs`

**Context:** CurrentUserService uses `ClaimTypes.NameIdentifier` instead of `OpenIddictConstants.Claims.Subject`. This service is used by audit interceptors, so incorrect claim extraction would break audit trails.

**Step 1: Write failing tests for CurrentUserService**

Create: `backend/tests/Quater.Backend.Core.Tests/Services/CurrentUserServiceTests.cs`

```csharp
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Constants;
using Quater.Backend.Data.Interceptors;
using Quater.Backend.Services;
using Xunit;

namespace Quater.Backend.Core.Tests.Services;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void GetCurrentUserId_WithSubjectClaim_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
           Claim(OpenIddictConstants.Claims.Subject, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = principal };
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithoutClaims_ReturnsSystemUserId()
    {
        // Arrange
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(httpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(SystemUser.GetId());
    }

    [Fact]
    public void GetCurrentUserLabId_WithLabIdClaim_ReturnsLabId()
    {
        // Arrange
        var labId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(QuaterClaimTypes.LabId, labId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = principal };
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserLabId();

        // Assert
        result.Should().Be(labId);
    }

    [Fact]
    public void GetCurrentUserLabId_WithoutLabIdClaim_ThrowsException()
    {
        // Arrange
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal() };
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessor.Object);

        // Act & Assert
        var act = () => service.GetCurrentUserLabId();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*LabId not found in claims*");
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
cd backend
dotnet test tests/Quater.Backend.Core.Tests/Quater.Backend.Core.Tests.csproj --filter "FullyQualifiedName~CurrentUserServiceTests" -v normal
```

Expected: FAIL - Tests fail because CurrentUserService uses wrong claim type

**Step 3: Update ICurrentUserService interface**

Modify: `backend/src/Quater.Backend.Data/Interceptors/ICurrentUserService.cs`

```csharp
namespace Quater.Backend.Data.Interceptors;

/// <summary>
/// Service for retrieving current user information from HTTP context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current authenticated us.
    /// </summary>
    /// <returns>The user ID from claims, or SystemUser ID if not authenticated.</returns>
    Guid GetCurrentUserId();

    /// <summary>
    /// Gets the current authenticated user's lab ID.
    /// </summary>
    /// <returns>The lab ID from claims.</returns>
    /// <exception cref="InvalidOperationException">Thrown when lab ID is not found in claims.</exception>
    Guid GetCurrentUserLabId();  // ✅ NEW
}
```

**Step 4: Fix CurrentUserService implementation**

Modify: `backend/src/Quater.Backend.Services/CurrentUserService.cs`

```csharp
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using Quater.Backend.Core.Constants;
using Quater.Backend.Data.Interceptors;
using System.Security.Claims;

namespace Quater.Backend.Services;

/// <summary>
/// Service for retrieving current user information from HTTP context.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current authenticated user's ID.
    /// </summary>
    /// <returns>The user ID from claims, or "System" if not authenticated.</returns>
    public Guid GetCurrentUserId()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(OpenIddictConstants.Claims.Subject);  // ✅ FIXED

        if (string.IsNullOrEmpty(userIdString))
        {
            return SystemUser.GetId();
        }

        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }

        // Fallback to system user if parsing fails
        return SystemUser.GetId();
    }

    /// <summary>
    /// Gets the current authenticated user's lab ID.
    /// </summary>
    /// <returns>The lab ID from claims.</returns>
    /// <exception cref="InvalidOperationException">Thrown when lab ID is not found in claims.</exception>
    public Guid GetCurrentUserLabId()  // ✅ NEW
    {
        var labIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(QuaterClaimTypes.LabId);

        if (string.IsNullOrEmpty(labIdString))
        {
            throw new InvalidOperationException("LabId not found in claims. Ensure the user is authenticated.");
        }

        if (!Guid.TryParse(labIdString, out var labId))
        {
            tw new InvalidOperationException("LabId in claims is not a valid GUID.");
        }

        return labId;
    }
}
```

**Step 5: Run tests to verify they pass**

```bash
cd backend
dotnet test tests/Quater.Backend.Core.Tests/Quater.Backend.Core.Tests.csproj --filter "FullyQualifiedName~CurrentUserServiceTests" -v normal
```

Expected: PASS - All 4 tests pass

**Step 6: Run all tests to ensure nothing broke**

```bash
cd backend
dotnet test backend/Quater.Backend.sln -v normal
```

Expected: PASS - All 246 tests still pass

**Step 7: Commit**

```bash
git add backend/src/Quatend.Services/CurrentUserService.cs
git add backend/src/Quater.Backend.Data/Interceptors/ICurrentUserService.cs
git add backend/tests/Quater.Backend.Core.Tests/Services/CurrentUserServiceTests.cs
git commit -m "fix: use OpenIddict claim types in CurrentUserService

- Replace ClaimTypes.NameIdentifier with OpenIddictConstants.Claims.Subject
- Add GetCurrentUserLabId() method for multi-tenancy support
- Update ICurrentUserService interface
- Add comprehensive tests for CurrentUserService
- Ensures audit interceptors use correct claim types"
```

---

## Summary

**Completed:**
- ✅ Fixed ClaimsPrincipalExtensions to use OpenIddict claim types
- ✅ Added GetLabIdOrThrow() and IsAdmin() helper m ✅ Fixed CurrentUserService to use OpenIddict claim types
- ✅ Added GetCurrentUserLabId() method
- ✅ Added comprehensive tests for both classes

**Files Modified:**
- `backend/src/Quater.Backend.Core/Extensions/ClaimsPrincipalExtensions.cs`
- `backend/src/Quater.Backend.Services/CurrentUserService.cs`
- `backend/src/Quater.Backend.Data/Interceptors/ICurrentUserService.cs`
- `backend/tests/Quater.Backend.Core.Tests/Extensions/ClaimsPrincipalExtensionsTests.cs` (new)
- `backend/tests/Quater.Backend.Core.Tests/Services/CurrentUserServiceTests.cs` (new)

**Tests Adde* 12 tests
**Commits:** 2 commits

**Next Steps:**
- Proceed to Plan 4: Authorization Infrastructure

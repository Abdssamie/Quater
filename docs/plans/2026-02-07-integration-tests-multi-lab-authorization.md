# Integration Tests for Multi-Lab Authorization Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add comprehensive integration tests to verify the multi-lab authorization system prevents privilege escalation and correctly enforces lab-scoped permissions.

**Architecture:** Create integration tests that verify end-to-end authorization flows including privilege escalation prevention, system admin bypass, lab context validation, role hierarchy enforcement, and multi-lab user scenarios. Tests will use the existing ApiTestFixture infrastructure with SetLabContext() and SetSystemAdmin() helpers.

**Tech Stack:** xUnit, ASP.NET Core Testing (WebApplicationFactory), Entity Framework Core, Testcontainers

---

## Task 1: Privilege Escalation Prevention Tests

**Files:**
- Create: `backend/tests/Quater.Backend.Api.Tests/Authorization/PrivilegeEscalationTests.cs`

**Step 1: Write test for admin in Lab A cannot delete sample in Lab B**

```csharp
using System.Net;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Core.DTOs;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

[Collection("ApiTest")]
public class PrivilegeEscalationTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _client = fixture.CreateClient();
    private Lab _labA = null!;
    private Lab _labB = null!;
    private User _user = null!;
    private Sample _sampleInLabB = null!;

    public async Task InitializeAsync()
    {
        // Create two labs
        _labA = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab A",
            Location = "Location A",
            IsActive = true
        };
        
        _labB = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab B",
            Location = "Location B",
            IsActive = true
        };
        
        await fixture.DbContext.Labs.AddRangeAsync(_labA, _labB);
        await fixture.DbContext.SaveChangesAsync();
        
        // Create user: Admin in Lab A, Viewer in Lab B
        _user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            IsActive = true,
            UserLabs =
            [
                new UserLab { LabId = _labA.Id, Role = UserRole.Admin },
                new UserLab { LabId = _labB.Id, Role = UserRole.Viewer }
            ]
        };
        
        await fixture.UserManager.CreateAsync(_user, "Password123!");
        
        // Create sample in Lab B
        _sampleInLabB = new Sample
        {
            Id = Guid.NewGuid(),
            SampleType = "Drinking",
            Location = "Test Location",
            LabId = _labB.Id,
            CollectionDate = DateTime.UtcNow
        };
        
        await fixture.DbContext.Samples.AddAsync(_sampleInLabB);
        await fixture.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AdminInLabA_CannotDeleteSample_InLabB_WhenViewerInLabB()
    {
        // Arrange: Authenticate as user and set Lab B context (where user is Viewer)
        fixture.AuthenticateAs(_user);
        fixture.SetLabContext(_labB.Id, UserRole.Viewer);
        
        // Act: Try to delete sample in Lab B (requires Admin role)
        var response = await _client.DeleteAsync($"/api/samples/{_sampleInLabB.Id}");
        
        // Assert: Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        
        // Verify sample still exists
        var sampleStillExists = await fixture.DbContext.Samples
            .AnyAsync(s => s.Id == _sampleInLabB.Id && !s.IsDeleted);
        Assert.True(sampleStillExists);
    }
}
```

**Step 2: Run test to verify it passes**

Run: `dotnet test backend/tests/Quater.Backend.Api.Tests/Authorization/PrivilegeEscalationTests.cs -v n`
Expected: PASS (1 test)

**Step 3: Add test for technician cannot create user in lab where they're viewer**

Add to `PrivilegeEscalationTests.cs`:

```csharp
[Fact]
public async Task TechnicianInLabA_CannotCreateUser_InLabB_WhenViewerInLabB()
{
    // Arrange: Authenticate as user and set Lab B context (where user is Viewer)
    fixture.AuthenticateAs(_user);
    fixture.SetLabContext(_labB.Id, UserRole.Viewer);
    
    // Act: Try to create user in Lab B (requires Admin role)
    var createUserDto = new CreateUserDto
    {
        UserName = "newuser",
        Email = "newuser@test.com",
        Password = "Password123!",
        Role = UserRole.Viewer,
        LabId = _labB.Id
    };
    
    var response = await _client.PostAsJsonAsync("/api/users", createUserDto);
    
    // Assert: Should be forbidden
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

**Step 4: Run tests to verify both pass**

Run: `dotnet test backend/tests/Quater.Backend.Api.Tests/Authorization/PrivilegeEscalationTests.cs -v n`
Expected: PASS (2 tests)

**Step 5: Commit**

```bash
git add backend/tests/Quater.Backend.Api.Tests/Authorization/PrivilegeEscalationTests.cs
git commit -m "test: add privilege escalation prevention tests"
```

---

## Task 2: System Admin Bypass Tests

**Files:**
- Create: `backend/tests/Quater.Backend.Api.Tests/Authorization/SystemAdminTests.cs`

**Step 1: Write test for system admin can access all labs**

```csharp
using System.Net;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

[Collection("ApiTest")]
public class SystemAdminTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _client = fixture.CreateClient();
    private Lab _labA = null!;
    private Lab _labB = null!;
    private Sample _sampleInLabA = null!;
    private Sample _sampleInLabB = null!;

    public async Task InitializeAsync()
    {
        // Create two labs
        _labA = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab A",
            Location = "Location A",
            IsActive = true
        };
        
        _labB = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab B",
            Location = "Location B",
            IsActive = true
        };
        
        await fixture.DbContext.Labs.AddRangeAsync(_labA, _labB);
        await fixture.DbContext.SaveChangesAsync();
        
        // Create samples in both labs
        _sampleInLabA = new Sample
        {
            Id = Guid.NewGuid(),
            SampleType = "Drinking",
            Location = "Location A",
            LabId = _labA.Id,
            CollectionDate = DateTime.UtcNow
        };
        
        _sampleInLabB = new Sample
        {
            Id = Guid.NewGuid(),
            SampleType = "Drinking",
            Location = "Location B",
            LabId = _labB.Id,
            CollectionDate = DateTime.UtcNow
        };
        
        await fixture.DbContext.Samples.AddRangeAsync(_sampleInLabA, _sampleInLabB);
        await fixture.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SystemAdmin_CanAccessSamples_InAllLabs_WithoutLabContext()
    {
        // Arrange: Set system admin mode (bypasses lab context requirement)
        fixture.SetSystemAdmin();
        
        // Act: Access samples in different labs without X-Lab-Id header
        var responseA = await _client.GetAsync($"/api/samples/{_sampleInLabA.Id}");
        var responseB = await _client.GetAsync($"/api/samples/{_sampleInLabB.Id}");
        
        // Assert: Both should succeed
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_CanPerformAdminActions_InAnyLab()
    {
        // Arrange: Set system admin mode
        fixture.SetSystemAdmin();
        
        // Create a test user to delete
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "deleteuser@test.com",
            Email = "deleteuser@test.com",
            EmailConfirmed = true,
            IsActive = true,
            UserLabs = [new UserLab { LabId = _labA.Id, Role = UserRole.Viewer }]
        };
        await fixture.UserManager.CreateAsync(testUser, "Password123!");
        
        // Act: Delete user (admin action)
        var response = await _client.DeleteAsync($"/api/users/{testUser.Id}");
        
        // Assert: Should succeed
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test backend/tests/Quater.Backend.Api.Tests/Authorization/SystemAdminTests.cs -v n`
Expected: PASS (2 tests)

**Step 3: Commit**

```bash
git add backend/tests/Quater.Backend.Api.Tests/Authorization/SystemAdminTests.cs
git commit -m "test: add system admin bypass tests"
```

---

## Task 3: Lab Context Validation Tests

**Files:**
- Create: `backend/tests/Quater.Backend.Api.Tests/Authorization/LabContextValidationTests.cs`

**Step 1: Write test for missing lab context returns 403**

```csharp
using System.Net;
using System.Net.Http.Json;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

[Collection("ApiTest")]
public class LabContextValidationTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _client = fixture.CreateClient();
    private Lab _labA = null!;
    private Lab _labB = null!;
    private User _user = null!;

    public async Task InitializeAsync()
    {
        // Create two labs
        _labA = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab A",
            Location = "Location A",
            IsActive = true
        };
        
        _labB = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab B",
            Location = "Location B",
            IsActive = true
        };
        
        await fixture.DbContext.Labs.AddRangeAsync(_labA, _labB);
        await fixture.DbContext.SaveChangesAsync();
        
        // Create user: Admin in Lab A only
        _user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            IsActive = true,
            UserLabs = [new UserLab { LabId = _labA.Id, Role = UserRole.Admin }]
        };
        
        await fixture.UserManager.CreateAsync(_user, "Password123!");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Request_WithoutLabContext_Returns403()
    {
        // Arrange: Authenticate as user but don't set lab context
        fixture.AuthenticateAs(_user);
        // Don't call fixture.SetLabContext()
        
        // Act: Try to access protected endpoint
        var response = await _client.GetAsync("/api/samples");
        
        // Assert: Should be forbidden with clear error message
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        
        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(error);
        Assert.Contains("X-Lab-Id", error["error"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Request_WithInvalidLabId_Returns403()
    {
        // Arrange: User is member of Lab A only
        fixture.AuthenticateAs(_user);
        
        // Act: Try to access Lab B (not a member)
        fixture.SetLabContext(_labB.Id, UserRole.Admin); // Invalid - user not in Lab B
        var response = await _client.GetAsync("/api/samples");
        
        // Assert: Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test backend/tests/Quater.Backend.Api.Tests/Authorization/LabContextValidationTests.cs -v n`
Expected: PASS (2 tests)

**Step 3: Commit**

```bash
git add backend/tests/Quater.Backend.Api.Tests/Authorization/LabContextValidationTests.cs
git commit -m "test: add lab context validation tests"
```

---

## Task 4: Role Hierarchy Tests

**Files:**
- Create: `backend/tests/Quater.Backend.Api.Tests/Authorization/RoleHierarchyTests.cs`

**Step 1: Write test for viewer can read but not create**

```csharp
using System.Net;
using System.Net.Http.Json;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Core.DTOs;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

[Collection("ApiTest")]
public class RoleHierarchyTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _client = fixture.CreateClient();
    private Lab _lab = null!;
    pri _viewerUser = null!;
    private User _technicianUser = null!;

    public async Task InitializeAsync()
    {
        // Create lab
        _lab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Test Lab",
            Location = "Test Location",
            IsActive = true
        };
        
        await fixture.DbContext.Labs.AddAsync(_lab);
        await fixture.DbContext.SaveChangesAsync();
        
        // Create viewer user
        _viewerUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "viewer@example.com",
            Email = "viewer@example.com",
            EmailConfirmed = true,
            IsActive = true,
            UserLabs = [new UserLab { LabId = _lab.Id, Role = UserRole.Viewer }]
        };
        
        // Create technician user
        _technicianUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "technician@example.com",
            Email = "technician@example.com",
            EmailConfirmed = true,
            IsActive = true,
            UserLabs = [new UserLab { LabId = _lab.Id, Role = UserRole.Technician }]
        };
        
        await fixture.UserManager.CreateAsync(_viewerUser, "Password123!");
        await fixture.UserManager.CreateAsync(_technicianUser, "Password123!");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Viewer_CanReadSamples_CannotCreateSamples()
    {
        // Arrange: Authenticate as viewer
        fixture.AuthenticateAs(_viewerUser);
        fixture.SetLabContext(_lab.Id, UserRole.Viewer);
        
        // Act: Try to read samples (allowed for Viewer)
        var readResponse = await _client.GetAsync("/api/samples");
        
        // Act: Try to create sample (forbidden for Viewer, requires Technician)
        var createDto = new CreateSampleDto
        {
            SampleType = "Drinking",
            Location = "Test Location",
            LabId = _lab.Id,
            CollectionDate = DateTime.UtcNow
        };
        var createResponse = await _client.PostAsJsonAsync("/api/samples", createDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task Technician_CanCreateSamples_CannotDeleteUsers()
    {
        // Arrange: Authenticate as technician
        fixture.AuthenticateAs(_technicianUser);
        fixture.SetLabContext(_lab.Id, UserRole.Technician);
        
        // Act: Try to create sample (allowed for Technician)
        var createDto = new CreateSampleDto
        {
            SampleType = "Drinking",
            Location = "Test Location",
            LabId = _lab.Id,
            CollectionDate = DateTime.UtcNow
        };
        var createResponse = await _client.PostAsJsonAsync("/api/samples", createDto);
        
        // Act: Try to delete user (forbidden for Technician, requires Admin)
        var deleteResponse = await _client.DeleteAsync($"/api/users/{_viewerUser.Id}");
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test backend/tests/Quater.Backend.Api.Tests/Authorization/RoleHierarchyTests.cs -v n`
Expected: PASS (2 tests)

**Step 3: Commit**

```bash
git add backend/tests/Quater.Backend.Api.Tests/Authorization/RoleHierarchyTests.cs
git commit -m "test: add role hierarchy enforcement tests"
```

---

## Task 5: Multi-Lab User Scenarios Tests

**Files:**
- Create: `backend/tests/Quater.Backend.Api.Tests/Authorization/MultiLabUserTests.cs`

**Step 1: Write test for user can switch between labs with different roles**

```csharp
using System.Net;
using System.Net.Http.Json;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Core.DTOs;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

[Collection("ApiTest")]
public class MultiLabUserTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _client = fixture.CreateClient();
    private Lab _labA = null!;
    private Lab _labB = null!;
    private User _user = null!;
    private Sample _sampleInLabA = null!;
    private Sample _sampleInLabB = null!;

    public async Task InitializeAsync()
    {
        // Create two labs
        _labA = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab A",
            Location = "Location A",
            IsActive = true
        };
        
        _labB = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab B",
            Location = "Location B",
            IsActive = true
        };
        
        await fixture.DbContext.Labs.AddRangeAsync(_labA, _labB);
        await fixture.DbContext.SaveChangesAsync();
        
        // Create user: Admin in Lab A, Viewer in Lab B
        _user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "multilab@example.com",
            Email = "multilab@example.com",
            EmailConfirmed = true,
            IsActive = true,
            UserLabs =
            [
                new UserLab { LabId = _labA.Id, Role = UserRole.Admin },
                new UserLab { LabId = _labB.Id, Role = UserRole.Viewer }
            ]
        };
        
        await fixture.UserManager.CreateAsync(_user, "Password123!");
        
        // Create samples in both labs
        _sampleInLabA = new Sample
        {
            Id = Guid.NewGuid(),
            SampleType = "Drinking",
            Location = "Location A",
            LabId = _labA.Id,
            CollectionDate = DateTime.UtcNow
        };
        
        _sampleInLabB = new Sample
        {
            Id = Guid.NewGuid(),
            SampleType = "Drinking",
            Location = "Location B",
            LabId = _labB.Id,
            CollectionDate = DateTime.UtcNow
        };
        
        await fixture.DbContext.Samples.AddRangeAsync(_sampleInLabA, _sampleInLabB);
        await fixture.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task User_CanSwitchBetweenLabs_WithDifferentRoles()
    {
        // Arrange: Authenticate as user
        fixture.AuthenticateAs(_user);
        
        // Act: Perform admin action in Lab A (should succeed)
        fixture.SetLabContext(_labA.Id, UserRole.Admin);
        var responseA = await _client.DeleteAsync($"/api/samples/{_sampleInLabA.Id}");
        
        // Act: Try admin action in Lab B (should fail - user is Viewer)
        fixture.SetLabContext(_labB.Id, UserRole.Viewer);
        var responseB = await _client.DeleteAsync($"/api/samples/{_sampleInLabB.Id}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NoContent, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, responseB.StatusCode);
        
        // Verify: Sample A deleted, Sample B still exists
        var sampleADeleted = await fixture.DbContext.Samples
            .AnyAsync(s => s.Id == _sampleInLabA.Id && s.IsDeleted);
        var sampleBExists = await fixture.DbContext.Samples
            .AnyAsync(s => s.Id == _sampleInLabB.Id && !s.IsDeleted);
        
        Assert.True(sampleADeleted);
        Assert.True(sampleBExists);
    }

    [Fact]
    public async Task User_CanReadInBothLabs_WithDifferentRoles()
    {
        // Arrange: Authenticate as user
        fixture.AuthenticateAs(_user);
        
        // Act: Read sample in Lab A (Admin role)
        fixture.SetLabContext(_labA.Id, UserRole.Admin);
        var responseA = await _client.GetAsync($"/api/samples/{_sampleInLabA.Id}");
        
        // Act: Read sample in Lab B (Viewer role)
        fixture.SetLabContext(_labB.Id, UserRole.Viewer);
        var responseB = await _client.GetAsync($"/api/samples/{_sampleInLabB.Id}");
        
        // Assert: Both should succeed (Viewer can read)
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test backend/tests/Quater.Backend.Api.Tests/Authorization/MultiLabUserTests.cs -v n`
Expected: PASS (2 tests)

**Step 3: Commit**

```bash
git add backend/tests/Quater.Backend.Api.Tests/Authorization/MultiLabUserTests.cs
git commit -m "test: add multi-lab user scenario tests"
```

---

## Task 6: Run Full Test Suite and Verify

**Step 1: Run altion tests**

Run: `dotnet test backend/tests/Quater.Backend.Api.Tests/Authorization/ -v n`
Expected: PASS (10 tests total)

**Step 2: Run full test suite**

Run: `dotnet test backend/Quater.Backend.sln`
Expected: PASS (all tests including new authorization tests)

**Step 3: Verify test coverage**

Check that authorization logic is covered:
- LabContextAuthorizationHandler.cs
- LabContextMiddleware.cs
- Authorization policies

**Step 4: Final commit**

```bash
git add -A
git commit -m "test: complete integration tests for multi-lab authorization

- Add privilege escalation prevention tests
- Add system admin bypass tests
- Add lab context validation tests
- Add role hierarchy enforcement tests
- Add multi-lab user scenario tests

All tests verify end-to-end authorization flows and security boundaries."
```

---

## Acceptance Criteria

- [ ] All 10 integration tests pass consistently
- [ ] Tests cover all critical authorization scenarios
- [ ] Privilege escalation attempts are blocked
- [ ] System admin bypass works correctly
- [ ] Lab context validation is comprehensive
- [ ] Role hierarchy is properly enforced
- [ ] Multi-lab users can switch contexts correctly
- [ ] Tests use realistic data and scenarios
- [ ] Tests are isolated and don't affect each other
- [ ] Full test suite passes (245+ tests)

---

## Notes

- Tests use existing ApiTestFixture infrastructure
- SetLabContext() and SetSystemAdmin() helpers already exist
- Tests verify both positive and negative scenarios
- Error messages are validated where applicable
- Database state is verified after operations
- Tests follow AAA pattern (Arrange, Act, Assert)

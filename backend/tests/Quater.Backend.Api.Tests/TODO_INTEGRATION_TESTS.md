# TODO: Integration Tests for Multi-Lab Authorization

## Priority: Medium
## Status: Not Started

### Overview
Add comprehensive integration tests to verify the multi-lab authorization system works correctly end-to-end. These tests should cover privilege escalation prevention, system admin bypass, and lab-context authorization scenarios.

---

## Test Scenarios to Implement

### 1. Privilege Escalation Prevention
**Goal:** Verify users cannot perform actions in labs where they lack sufficient privileges.

```csharp
[Fact]
public async Task AdminInLabA_CannotDeleteSampleInLabB_WhenViewerInLabB()
{
    // Arrange: User is Admin in Lab A, Viewer in Lab B
    var user = await CreateUserWithMultipleLabsAsync(
        (labA.Id, UserRole.Admin),
        (labB.Id, UserRole.Viewer));
    
    var sampleInLabB = await CreateSampleInLabAsync(labB.Id);
    
    // Act: Try to delete sample in Lab B with X-Lab-Id: Lab B
    fixture.SetLabContext(labB.Id, UserRole.Viewer);
    var response = await client.DeleteAsync($"/api/samples/{sampleInLabB.Id}");
    
    // Assert: Should be forbidden (403)
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.Contains("insufficient permissions", error.Message, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task TechnicianInLabA_CannotCreateUserInLabB_WhenViewerInLabB()
{
    // Arrange: User is Technician in Lab A, Viewer in Lab B
    var user = await CreateUserWithMultipleLabsAsync(
        (labA.Id, UserRole.Technician),
        (labB.Id, UserRole.Viewer));
    
    // Act: Try to create user in Lab B (requires Admin)
    fixture.SetLabContext(labB.Id, UserRole.Viewer);
    var response = await client.PostAsJsonAsync("/api/users", new CreateUserDto
    {
        UserName = "newuser",
        Email = "newuser@test.com",
        Password = "Password123!",
        Role = UserRole.Viewer,
        LabId = labB.Id
    });
    
    // Assert: Should be forbidden (403)
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

### 2. System Admin Bypass
**Goal:** Verify system admin can access all labs without X-Lab-Id header.

```csharp
[Fact]
public async Task SystemAdmin_CanAccessAllLabs_WithoutLabContext()
{
    // Arrange: System admin user
    fixture.SetSystemAdmin();
    
    var sampleInLabA = await CreateSampleInLabAsync(labA.Id);
    var sampleInLabB = await CreateSampleInLabAsync(labB.Id);
    
    // Act: Access samples in different labs without X-Lab-Id header
    var responseA = await client.GetAsync($"/api/samples/{sampleInLabA.Id}");
    var responseB = await client.GetAsync($"/api/samples/{sampleInLabB.Id}");
    
    // Assert: Both should succeed
    Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
    Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
}

[Fact]
public async Task SystemAdmin_CanPerformAdminActions_InAnyLab()
{
    // Arrange: System admin user
    fixture.SetSystemAdmin();
    
    // Act: Delete user in Lab A (admin action)
    var response = await client.DeleteAsync($"/api/users/{userId}");
    
    // Assert: Should succeed
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
}
```

---

### 3. Lab Context Validation
**Goal:** Verify X-Lab-Id header is required and validated correctly.

```csharp
[Fact]
public async Task Request_WithoutLabContext_Returns403()
{
    // Arrange: Regular user (not system admin)
    var user = await CreateUserWithLabAsync(labA.Id, UserRole.Admin);
    fixture.AuthenticateAs(user);
    // Don't set lab context
    
    // Act: Try to access protected endpoint
    var response = await client.GetAsync("/api/samples");
    
    // Assert: Should be forbidden with clear error message
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.Contains("X-Lab-Id header", error.Message, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task Request_WithInvalidLabId_Returns403()
{
    // Arrange: User is member of Lab A only
    var user = await CreateUserWithLabAsync(labA.Id, UserRole.Admin);
    fixture.AuthenticateAs(user);
    
    // Act: Try to access Lab B (not a member)
    fixture.SetLabContext(labB.Id, UserRole.Admin); // Invalid - user not in Lab B
    var response = await client.GetAsync("/api/samples");
    
    // Assert: Should be forbidden
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

### 4. Role Hierarchy Validation
**Goal:** Verify role hierarchy works correctly (Viewer < Technician < Admin).

```csharp
[Fact]
public async Task Viewer_CanReadSamples_CannotCreateSamples()
{
    // Arrange: User is Viewer in Lab A
    var user = await CreateUserWithLabAsync(labA.Id, UserRole.Viewer);
    fixture.SetLabContext(labA.Id, UserRole.Viewer);
    
    // Act: Try to read samples (allowed)
    var readResponse = await client.GetAsync("/api/samples");
    
    // Act: Try to create sample (forbidden)
    var createResponse = await client.PostAsJsonAsync("/api/samples", new CreateSampleDto
    {
        SampleType = "Drinking",
        Location = "Test Location",
        LabId = labA.Id
    });
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
}

[Fact]
public async Task Technician_CanCreateSamples_CannotDeleteUsers()
{
    // Arrange: User is Technician in Lab A
    var user = await CreateUserWithLabAsync(labA.Id, UserRole.Technician);
    fixture.SetLabContext(labA.Id, UserRole.Technician);
    
    // Act: Try to create sample (allowed)
    var createResponse = await client.PostAsJsonAsync("/api/samples", new CreateSampleDto
    {
        SleType = "Drinking",
        Location = "Test Location",
        LabId = labA.Id
    });
    
    // Act: Try to delete user (forbidden - requires Admin)
    var deleteResponse = await client.DeleteAsync($"/api/users/{userId}");
    
    // Assert
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
}
```

---

### 5. Multi-Lab User Scenarios
**Goal:** Verify users can switch between labs correctly.

```csharp
[Fact]
public async Task User_CanSwitchBetweenLabs_WithDifferentRoles()
{
    // Arrange: User is Admin in Lab A, Viewer in Lab B
    var user = await CreateUserWithMultipleLabsAsync(
        (labA.Id, UserRole.Admin),
        (labB.Id, UserRole.Viewer));
    
    // Act: Perform admin action in Lab A (should succeed)
    fixture.SetLabContext(labA.Id, UserRole.Admin);
    var responseA = await client.DeleteAsync($"/api/samples/{sampleInLabA.Id}");
    
    // Act: Try admin action in Lab B (should fail)
    fixture.SetLabContext(labB.Id, UserRole.Viewer);
    var responseB = await client.DeleteAsync($"/api/samples/{sampleInLabB.Id}");
    
    // Assert
    Assert.Equal(HttpStatusCode.NoContent, responseA.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, responseB.StatusCode);
}
```

---

## Test Infrastructure Needed

### Helper Methods
```csharp
// In ApiTestFixture.cs or test base class

protected async Task<User> CreateUserWithMultipleLabsAsync(params (Guid labId, UserRole role)[] labs)
{
    var user = new User
    {
        UserName = $"testuser_{Guid.NewGuid()}",
        Email = $"test_{Guid.NewGuid()}@example.com",
        IsActive = true,
        UserLabs = labs.Select(l => new UserLab
        {
            LabId = l.labId,
            Role = l.role
        }).ToList()
    };
    
    await _userManager.CreateAsync(user, "Password123!");
    return user;
}

protected async Task<Sample> CreateSampleInLabAsync(Guid labId)
{
    var sample = new Sample
    {
        SampleType = "Drinking",
        Location = "Test Location",
        LabId = labId,
        CollectionDate = DateTime.UtcNow
    };
    
    await _context.Samples.AddAsync(sample);
    await _context.SaveChangesAsync();
    return sample;
}
```

---

## Acceptance Criteria

- [ ] All privilege escalation scenarios are tested and pass
- [ ] System admin bypass is verified
- [ ] Lab context validation is comprehensive
- [ ] Role hierarchy is tested for all three roles
- [ ] Multi-lab user scenarios are covered
- [ ] Tests use realistic data and scenarios
- [ ] Tests are isolated and don't affect each other
- [ ] All tests pass consistently
- [ ] Code coverage for authorization logic is > 90%

---

## Estimated Effort
- **Time:** 4-6 hours
- **Complexity:** Medium
- **Dependencies:** None (all infrastructure exists)

---

## References
- Authorization handler: `LabContextAuthorizationHandler.cs`
- Middleware: `LabContextMiddleware.cs`
- Test fixture: `ApiTestFixture.cs`
- Existing test helpers: `SetLabContext()`, `SetSystemAdmin()`

# Integration Test Plan - Phase 4
## Controller Structure, Rate Limiting, and Error Handling Tests

---

## 📋 Executive Summary

This document outlines a comprehensive plan for writing integration tests to validate:
1. **New Controller Structure** - Split authentication controllers (Phase 3, Task 1)
2. **Rate Limiting Behavior** - Per-endpoint rate limiting with email tracking (Phase 3, Task 2)
3. **Error Handling** - Standardized exception handling across all endpoints (Phase 2, Task 2)

**Current Test Status**: 184 tests passing (all unit/service tests)  
**Target**: Add ~80-100 integration tests  
**Estimated Effort**: 12-16 hours  
**Test Framework**: xUnit + WebApplicationFactory + FluentAssertions

---

## 🎯 Test Objectives

### Primary Goals
1. ✅ Verify new controller structure works correctly with proper routing
2. ✅ Validate rate limiting enforces limits and tracks by IP/Email/UserId
3. ✅ Ensure error handling returns consistent HTTP status codes and error messages
4. ✅ Test authentication/authorization flows across split controllers
5. ✅ Verify breaking API changes are documented and intentional

### Secondary Goals
6. ✅ Test middleware integration (rate limiting, exception handling, security headers)
7. ✅ Validate request/response serialization
8. ✅ Test edge cases and boundary conditions
9. ✅ Ensure backward compatibility where applicable

---

## 🏗️ Test Project Structure

### New Test Project: `Quater.Backend.Api.Tests`

```
backend/tests/Quater.Backend.Api.Tests/
├── Controllers/
│   ├── AuthControllerTests.cs                    # Token endpoint tests
│   ├── RegistrationControllerTests.cs            # Registration endpoint tests
│   ├── PasswordControllerTests.cs                # Password management tests
│   ├── EmailVerificationControllerTests.cs       # Email verification tests
│   ├── SamplesControllerTests.cs                 # Sample CRUD tests
│   ├── LabsControllerTests.cs                    # Lab CRUD tests
│   ├── ParametersControllerTests.cs              # Parameter CRUD tests
│   ├── TestResultsControllerTests.cs             # TestResult CRUD tests
│   ├── UsersControllerTests.cs                   # User management tests
│   └── AuditLogsControllerTests.cs               # Audit log tests
├── Middleware/
│   ├── RateLimitingMiddlewareTests.cs            # Rate limiting behavior
│   ├── GlobalExceptionHandlerTests.cs            # Error handling tests
│   └── SecurityHeadersMiddlewareTests.cs         # Security headers tests
├── Integration/
│   ├── AuthenticationFlowTests.cs                # End-to-end auth flows
│   ├── ErrorHandlingIntegrationTests.cs          # Cross-controller error tests
│   └── RateLimitingIntegrationTests.cs           # Rate limit scenarios
├── Helpers/
│   ├── WebApplicationFactoryHelper.cs            # Test server factory
│   ├── Autelper.cs                   # JWT token generation
│   ├── RedisTestHelper.cs                        # Redis mock/test container
│   └── HttpClientExtensions.cs                   # HTTP client helpers
└── Fixtures/
    ├── ApiTestFixture.cs                         # Shared test fixture
    └── RedisFixture.cs                           # Redis test container fixture
```

---

## 📦 Required NuGet Packages

Add to `Quater.Backend.Api.Tests.csproj`:

```xml
<ItemGroup>
  <!-- Existing packages -->
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  <PackageReference Include="FluentAssertions" Version="7.0.0" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  <PackageReference Include="coverlet.collector" Version="6.0.4" />
  
  <!-- New packages for integration testing -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  <PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
  <PackageReference Include="Testcontainers.Redis" Version="3.10.0" />
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="Bogus" Version="35.6.1" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\..\src\Quater.Backend.Api\Quater.Backend.Api.csproj" />
  <ProjectReference Include="..\..\src\Quater.Backend.Core\Quater.Backend.Core.csproj" />
  <ProjectReference Include="..\..\src\Quater.Backend.Data\Quater.Backend.Data.csproj" />
  <ProjectReference Include="..\..\src\Quater.Backend.Services\Quater.Backend.Services.csproj" />
  <ProjectReference Include="../../../shared/Quater.Shared.csproj" />
</ItemGroup>
```

---

## 🧪 Test Categories & Breakdown

### **Category 1: Controller Structure Tests** (30 tests)

#### 1.1 AuthController Tests (8 tests)
**File**: `Controllers/AuthControllerTests.cs`

**Test Cases**:
- ✅ `Token_PasswordGrant_ValidCredentials_ReturnsAccessToken`
- ✅ `Token_PasswordGrant_InvalidCredentials_ReturnsForbidden`
- ✅ `Token_PasswordGrant_InactiveUser_ReturnsForbidden`
- ✅ `Token_PasswordGrant_LockedOutUser_ReturnsForbidden`
- ✅ `Token_RefreshTokenGrant_ValidToken_ReturnsNewAccessToken`
- ✅ `Token_RefreshTokenGrant_InvalidToken_ReturnsForbidden`
- ✅ `Logout_AuthenticatedUser_RevokesAllTokens`
- ✅ `UserInfo_AuthenticatedUser_ReturnsUserData`

**Key Validations**:
- Route: `POST /api/auth/token` (unchanged)
- OAuth2 token response format
- Claim structure (Subject, Email, Role, LabId)
- Token revocation on logout

---

#### 1.2 RegistrationController Tests (6 tests)
**File**: `Controllers/RegistrationControllerTests.cs`

**Test Cases**:
- ✅ `Register_ValidRequest_ReturnsOkAndSendsEmail`
- ✅ `Register_DuplicateEmail_ReturnsBadRequest`
- ✅ `Register_InvalidEmail_ReturnsBadRequest`
- ✅ `Register_WeakPassword_ReturnsBadRequest`
- ✅ `Register_MissingLabId_ReturnsBadRequest`
- ✅ `Register_InvalidRole_ReturnsBadRequest`

**Key Validations**:
- **BREAKING CHANGE**: Route changed from `POST /api/auth/register` → `POST /api/registration/register`
- Email verification sent
- User created with correct role and lab association
- Password validation enforced

---

#### 1.3 PasswordController Tests (8 tests)
**File**: `Controllers/PasswordControllerTests.cs`

**Test Cases**:
- ✅ `ChangePassword_ValidRequest_ReturnsOk`
- ✅ `ChangePassword_WrongCurrentPassword_ReturnsBadRequest`
- ✅ `ChangePassword_Unauthenticated_ReturnsUnauthorized`
- ✅ `ForgotPassword_ExistingEmail_ReturnsOkAndSendsEmail`
- ✅ `ForgotPassword_NonExistentEmail_ReturnsOkWithoutSendingEmail` (timing attack prevention)
- ✅ `ForgotPassword_InactiveUser_ReturnsOkWithoutSendingEmail`
- ✅ `ResetPassword_ValidToken_ReturnsOkAndSendsAlert`
- ✅ `ResetPassword_InvalidToken_ReturnsBadRequest`

**Key Validations**:
- **BREAKING CHANGES**: 
  - `POST /api/auth/change-password` → `POST /api/password/change`
  - `POST /api/auth/forgot-password` → `POST /api/password/forgot`
  - `POST /api/auth/reset-password` → `POST /api/password/reset`
- Timing attack protection (200ms delay)
- Security alert emails sent
- Token validation

---

#### 1.4 EmailVerificationController Tests (4 tests)
**File**: `Controllers/EmailVerificationControllerTests.cs`

**Test Cases**:
- ✅ `VerifyEmail_ValidToken_ReturnsOk`
- ✅ `VerifyEmail_InvalidToken_ReturnsBadRequest`
- ✅ `VerifyEmail_ExpiredToken_ReturnsBadRequest`
- ✅ `ResendVerification_ValidEmail_SendsNewEmail`

**Key Validations**:
- **BREAKING CHANGES**:
  - `POST /api/auth/verify-email` → `POST /api/email-verification/verify`
  - `POST /api/auth/resend-verification` → `POST /api/email-verification/resend`
- Email verification status updated
- Verification emails sent

---

#### 1.5 Other Controller Tests (4 tests)
**Files**: `Controllers/SamplesControllerTests.cs`, etc.

**Test Cases**:
- ✅ `SamplesController_GetAll_ReturnsPagedResults`
- ✅ `LabsController_Create_RequiresAdminRole`
- ✅ `ParametersController_Update_ReturnsNotFoundForInvalidId`
- ✅ `UsersController_Delete_RequiresAdminRole`

**Key Validations**:
- Authorization policies enforced
- Pagination works correctly
- Standard CRUD operations

---

### **Category 2: Rate Limiting Tests** (25 tests)

#### 2.1 Global Rate Limiting Tests (8 tests)
**File**: `Middleware/RateLimitingMiddlewareTests.cs`

**Test Cases**:
- ✅ `GlobalRateLimit_AuthenticatedUser_Allows100RequestsPerMinute`
- ✅ `GlobalRateLimit_AuthenticatedUser_Blocks101stRequest`
- ✅ `GlobalRateLimit_AnonymousUser_Allows20RequestsPerMinute`
- ✅ `GlobalRateLimit_AnonymousUser_Blocks21stRequest`
- ✅ `GlobalRateLimit_DifferentUsers_IndependentLimits`
- ✅ `GlobalRateLimit_ResetAfterWindow_AllowsNewRequests`
- ✅ `GlobalRateLimit_ReturnsCorrectHeaders` (X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset)
- ✅ `GlobalRateLimit_Returns429StatusCode`

**Key Validations**:
- Redis counter increments correctly
- TTL set on first request
- Rate limit headersn- 429 Too Many Requests status

---

#### 2.2 Endpoint-Specific Rate Limiting Tests (12 tests)
**File**: `Integration/RateLimitingIntegrationTests.cs`

**Test Cases**:

**Register Endpoint** (10 req/hour, IP-based):
- ✅ `Register_IpBased_Allows10RequestsPerHour`
- ✅ `Register_IpBased_Blocks11thRequest`
- ✅ `Register_DifferentIps_IndependentLimits`

**Token Endpoint** (10 req/min, IP-based):
- ✅ `Token_IpBased_Allows10RequestsPerMinute`
- ✅ `Token_IpBased_Blocks11thRequest`

**ForgotPassword Endpoint** (10 req/hour, Email-based):
- ✅ `ForgotPassword_EmailBased_Allows10RequestsPerHour`
- ✅ `ForgotPassword_EmailBased_Blocks11thRequest`
- ✅ `ForgotPassword_DifferentEmails_IndependentLimits`
- ✅ `ForgotPassword_EmailExtractedFromRequestBody`

**ResetPassword Endpoint** (10 req/hour, Email-based):
- ✅ `ResetPassword_EmailBased_Allows10RequestsPerHour`
- ✅ `ResetPassword_EmailBased_Blocks11thRequest`
- ✅ `ResetPassword_EmailExtractedFromRequestBody`

**Key Validations**:
- Endpoint-specific limits override global limits
- Email tracking reads from request body
- Request body buffering works correctly
- Different tracking strategies (IP vs Email vs UserId)

---

#### 2.3 Rate Limiting Edge Cases (5 tests)
**File**: `Integration/RateLimitingIntegrationTests.cs`

**Test Cases**:
- ✅ `RateLimit_RedisUnavailable_AllowsRequestsWithWarning`
- ✅ `RateLimit_ConcurrentRequests_AtomicIncrement`
- ✅ `RateLimit_MalformedRequestBody_FallsBackToIpTracking`
- ✅ `RateLimit_MissingEmailInBody_FallsBackToIpTracking`
- ✅ `RateLimit_LargeRequestBody_HandlesBuffering`

**Key Validations**:
- Graceful degradation when Redis fails
- Lua script prevents race conditions
- Fallback mechanisms work
- Request body buffering doesn't break large payloads

---

### **Category 3: Error Handling Tests** (20 tests)

#### 3.1 Custom Exception Mapping Tests (10 tests)
**File**: `Middleware/GlobalExceptionHandlerTests.cs`

**Test Cases**:
- ✅ `NotFoundException_Returns404WithErrorMessage`
- ✅ `BadRequestException_Returns400WithErrorMessage`
- ✅ `ConflictException_Returns409WithErrorMessage`
- ✅ `ForbiddenException_Returns403WithErrorMessage`
- ✅ `SyncException_Returns500WithErrorMessage`
- ✅ `UnhandledException_Returns500WithGenericMessage`
- ✅ `ValidationException_Returns400WithValidationErrors`
- ✅ `DbUpdateConcurrencyException_Returns409WithMessage`
- ✅ `UnauthorizedAccessException_Returns403WithMessage`
- ✅ `ArgumentException_Returns400WithMessage`

**Key Validations**:
- Correct HTTP status codes
- Error messages from `ErrorMessages.cs`
- Consistent error response format
- No ck traces in production

---

#### 3.2 Cross-Controller Error Handling Tests (10 tests)
**File**: `Integration/ErrorHandlingIntegrationTests.cs`

**Test Cases**:
- ✅ `SampleService_GetByIdAsync_NonExistentId_Returns404`
- ✅ `LabService_CreateAsync_DuplicateCode_Returns409`
- ✅ `ParameterService_UpdateAsync_ConcurrencyConflict_Returns409`
- ✅ `UserService_DeleteAsync_NonExistentUser_Returns404`
- ✅ `SampleController_Create_InvalidDto_Returns400WithValidationErrors`
- ✅ `LabController_Create_MissingRequiredField_Returns400`
- ✅ `ParameterController_Update_InvalidId_Returns404`
- ✅ `TestResultController_Create_InvalidSampleId_Returns404`
- ✅ `AuthController_Token_DatabaseError_Returns500`
- ✅ `RegistrationController_Register_EmailServiceDown_Returns500`

**Key Validations**:
- Services throw correct custom exceptions
- Controllers don't catch exceptions (let middleware handle)
- Error responses consistent across all controllers
- Validation errors properly formatted

---

### **Category 4: Authentication & Authorization Tests** (15 tests)

#### 4.1 Authentication Flow Tests (8 tests)
**File**: `Integration/AuthenticationFlowTests.cs`

**Test Cases**:
- ✅ `CompleteRegistrationFlow_RegisterVerifyLogin_Success`
- ✅ `PasswordResetFlow_ForgotResetLogin_Success`
- ✅ `TokenRefreshFlow_LoginRefreshUseToken_Success`
- ✅ `LogoutFlow_LoginLogoutUseToken_Fails`
- ✅ `UnverifiedEmail_Login_Succeeds` (email verification not enforced yet)
- ✅ `InactiveUser_Login_Fails`
- ✅ `LockedOutUser_Login_Fails`
- ✅ `ExpiredToken_Refresh_Fails`

**Key Validations**:
- End-to-end flows work across split controllers
- JWT tokens valid and contain correct claims
- Token revocation works
- User state changes reflected in auth

---

#### 4.2 Authorization Tests (7 tests)
**File**: `Integration/AtionFlowTests.cs`

**Test Cases**:
- ✅ `AdminEndpoint_TechnicianUser_Returns403`
- ✅ `AdminEndpoint_AdminUser_Returns200`
- ✅ `LabSpecificEndpoint_DifferentLab_Returns403`
- ✅ `LabSpecificEndpoint_SameLab_Returns200`
- ✅ `AuthenticatedEndpoint_NoToken_Returns401`
- ✅ `AuthenticatedEndpoint_InvalidToken_Returns401`
- ✅ `AuthenticatedEndpoint_ValidToken_Returns200`

**Key Validations**:
- Role-based authorization enforced
- Lab-based authorization enforced
- JWT validation works
- Proper 401 vs 403 distinction

---

## 🛠️ Test Infrastructure Components

### 1. WebApplicationFactory Setup

**File**: `Helpers/WebApplicationFactoryHn```csharp
public class QuaterApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real database with test container
            services.RemoveAll<DbContextOptions<QuaterDbContext>>();
            services.AddDbContext<QuaterDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));

            // Replace real Redis with test container
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));

            // Mock email service
            services.RemoveAll<IEmailQueue>();
            services.AddSingleton<IEmailQueue>(Mock.Of<IEmailQueue>());
        });
    }
}
```

---

### 2. Authentication Helper

**File**: `Helpers/AuthenticationHelper.cs`

```csharp
public static class AuthenticationHelper
{
    public static async Task<string> GetAuthTokenAsync(
        HttpClient client,        string email, 
        string password)
    {
        var request = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", email),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        });

        var response = await client.PostAsync("/api/auth/token", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<TokenResponse>(json);
        return token.AccessToken;
    }

    public static void AddAuthToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
}
```

---

### 3. Redis Test Helper

**File**: `Helpers/RedisTestHelper.cs`

```csharp
public static class RedisTestHelper
{
    public static async Task ClearRateLimieysAsync(IConnectionMultiplexer redis)
    {
        var db = redis.GetDatabase();
        var server = redis.GetServer(redis.GetEndPoints().First());
        
        await foreach (var key in server.KeysAsync(pattern: "ratelimit:*"))
        {
            await db.KeyDeleteAsync(key);
        }
        
        await foreach (var key in server.KeysAsync(pattern: "endpoint-ratelimit:*"))
        {
            await db.KeyDeleteAsync(key);
        }
    }

    public static async Task<long> GetRateLimitCountAsync(
        IConnectionMultiplexer redis, 
        string key)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(key);
        return value.HasValue ? (long)value : 0;
    }
}
```

---

### 4. HTTP Client Extensions

**File**: `Helpers/HttpClientExtensions.cs`

```csharp
public static class HttpClientExtensions
{
    public static async Task<T> GetJsonAsync<T>(
        this HttpClient client, 
        string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialin);
    }

    public static async Task<HttpResponseMessage> PostJsonAsync<T>(
        this HttpClient client, 
        string url, 
        T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    public static void SetForwardedFor(this HttpClient client, string ipAddress)
    {
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);
    }
}
```

---

## 📊 Test Execution Strategy

### Phase 1: Setup (2 hours)
1. Create `Quater.Backend.Api.Tests` project
2. Add NuGet packages
3. Create test infrastructure (WebApplicationFactory, helpers)
4. Set up test containers (PostgreSQL, Redis)
5. Create base test fixtures

### Phase 2: Controller Tests (4 hours)
1. Write AuthController tests (1 hour)
2. Write RegistrationController tests (1 hour)
3. Write PasswordController tests (1 hour)
4. Write EmailVerificationController tests (1 hour)

### Phase 3: Rate Limiting Tests (3 hours)
1. Write global rate limiting tests (1 hour)
2. Write endpoint-specific rate limiting.5 hours)
3. Write edge case tests (0.5 hours)

### Phase 4: Error Handling Tests (2 hours)
1. Write exception mapping tests (1 hour)
2. Write cross-controller error tests (1 hour)

### Phase 5: Authentication/Authorization Tests (2 hours)
1. Write authentication flow tests (1 hour)
2. Write authorization tests (1 hour)

### Phase 6: Cleanup & Documentation (1 hour)
1. Refactor duplicate code
2. Add XML documentation
3. Update TESTING_SUMMARY.md
4. Create test execution guide

---

## 🎯 Success Criteria

### Quantitative Metrics
- ✅ **80+ integration tests** written and passing
- ✅ **100% pass rate** on all tests
- ✅ **<60 seconds** total test execution time
- ✅ **Zero flaky tests** (consistent results)

### Qualitative Metrics
- ✅ All breaking API changes documented and tested
- ✅ Rate limiting behavior validated with real Redis
- ✅ Error handling consistent across all endpoints
- ✅ Authentication flows work end-to-end
- ✅ Test code is maintainable and well-documented

---

## 🚀 Running the Tests

```bash
# Run all integration tests
dotnet test backend/tests/Quater.Backend.Api.Tests/

# Run specific test category
dotnet test --filterualifiedName~RateLimitingTests"
dotnet test --filter "FullyQualifiedName~ErrorHandlingTests"
dotnet test --filter "FullyQualifiedName~AuthenticationFlowTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run all tests (unit + integration)
dotnet test backend/Quater.Backend.sln
```

---

## 📝 Test Naming Convention

Follow the pattern: `MethodName_Scenario_ExpectedResult`

**Examples**:
- ✅ `Register_ValidRequest_ReturnsOkAndSendsEmail`
- ✅ `Token_InvalidCredentials_ReturnsForbidden``ForgotPassword_EmailBased_Blocks11- ✅ `NotFoundException_Returns404WithErrorMessage`

---

## 🔍 Test Coverage Goals

| Component | Target Coverage | Priority |
|-----------|----------------|----------|
| Controllers | 90%+ | High |
| Middleware | 85%+ | High |
| Error Handling | 95%+ | High |
| Rate Limiting | 90%+ | High |
| Authentication | 85%+ | Medium |
| Authorization | 80%+ | Medium |

---

## 📚 Additional Resources

### Documentation to Create
1. **API Breaking Changes Guide** - Document all route changes from Phase 3
2. **Rate Limiting Configuration Guide** - How to configure limits per environment
3. **Error Response Format Specification** - Standard error response structure
4. **Testing Best Practices** - Guidelines for writing integration tests

### Tools & Libraries
- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **WebApplicationFactory** - In-memory test server
- **Testcontainers** - Docker containers for tests
- **Moq** - Mocking framework
- **Bogus** - Fake data generation

---

## ⚠️ Known Challenges & Mitigations

### Challenge 1: Redis Test Container Performance
**Issue**: Redis container startup a-3 seconds per test class  
**Mitigation**: Use shared fixture with `IClassFixtture>` to reuse container

### Challenge 2: Rate Limiting Time-Based Tests
**Issue**: Time-based tests can be flaky  
**Mitigation**: Use `FakeTimeProvider` where possible, or add tolerance to assertions

### Challenge 3: Email Service Mocking
**Issue**: Email queue is async and hard to verify  
**Mitigation**: Mock `IEmailQueue` and verify method calls with Moq

### Challenge 4: Test Data Isolation
**Issue**: Tests may interfere with each other  
**Mitigation**: Reset database and Redis between tests using fixtures

---

## 📅 Timeline

| Phase | Duration | Deliverable |
|-------|----------|-------------|
| Setup | 2 hours | Test project with infrastructure |
| Controller Tests | 4 hours | 30 passing tests |
| Rate Limiting Tests | 3 hours | 25 passing tests |
| Error Handling Tests | 2 hours | 20 passing tests |
| Auth/Authz Tests | 2 hours | 15 passing tests |
| Cleanup | 1 hour | Documentation + refactoring |
| **Total** | **14 hours** | **90+ passing tests** |

---

## ✅ Definition of Done

- [ ] All 80+ integration tests written and passing
- [ ] Ttion time < 60 seconds
- [ ] Zero flaky tests (run 10 times, all pass)
- [ ] Code coverage reports generated
- [ ] TESTING_SUMMARY.md updated
- [ ] Breaking API changes documented
- [ ] Test infrastructure documented
- [ ] CI/CD pipeline updated to run integration tests
- [ ] Code reviewed and approved
- [ ] Merged to main branch

---

## 🎉 Expected Outcomes

After completing this test plan:

1. ✅ **High Confidence** in new controller structure
2. ✅ **Validated Rate Limiting** with real Redis behavior
3. ✅ **Consistent Error Handling** across all endpoints
4. ✅ **Documented Breaking Changes** for API consumers
5. ✅ **Regression Protection** for future refactoring
6. ✅ **Production-Ready** authentication system

**Total Test Count**: 184 (existing) + 90 (new) = **274 tests**  
**Estimated Coverage**: **85-90%** of API layer

---

**Document Version**: 1.0  
**Created**: 2025-02-05  
**Author**: AI Assistant  
**Status**: Ready for Implementation

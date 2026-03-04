# Audit Gap Test Coverage Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add deterministic tests for the five audit gap behaviors (auth timing, unauthorized gating, cancellation propagation, soft-delete filters, token key cleanup).

**Architecture:** Use targeted unit tests for auth/token/cancellation behavior and SQLite in-memory integration tests for EF Core query filters. Avoid reflection and avoid timing-based flakiness by asserting immediate, synchronous outcomes.

**Tech Stack:** .NET 10, xUnit, Moq, Avalonia Dispatcher test helpers, EF Core SQLite.

---

### Task 1: Add failing auth timing tests

**Files:**
- Modify: `desktop/Quater.Desktop.Tests/Auth/AuthSessionManagerTests.cs`

**Step 1: Write the failing test**

Add two tests:

```csharp
[Fact]
public async Task InitializeAsync_UpdatesAppStateBeforeReturning()
{
    var authService = new Mock<IAuthService>();
    var accessTokenCache = new Mock<IAccessTokenCache>();
    var usersApi = new Mock<IUsersApi>();
    var apiClientFactory = new Mock<IApiClientFactory>();
    apiClientFactory.Setup(factory => factory.GetUsersApi()).Returns(usersApi.Object);
    var dialogService = new Mock<IDialogService>();
    var appState = new AppState { IsAuthenticated = false };
    var settingsUpdater = new SettingsUpdater(Mock.Of<ISettingsStore>(), new AppSettings());
    var logger = new Logger<AuthSessionManager>(new LoggerFactory());
    var labId = Guid.NewGuid();
    var labs = new List<UserLabDto> { new UserLabDto(labId: labId, labName: "Alpha Lab") };
    var userInfo = new UserDto(userName: "analyst", email: "analyst@lab.com", labs: labs);

    accessTokenCache.SetupGet(cache => cache.CurrentToken).Returns("valid-token");
    usersApi.Setup(api => api.ApiUsersMeGetAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(userInfo);

    var manager = new AuthSessionManager(
        authService.Object,
        accessTokenCache.Object,
        apiClientFactory.Object,
        appState,
        settingsUpdater,
        dialogService.Object,
        logger);

    await manager.InitializeAsync();

    Assert.True(appState.IsAuthenticated);
    Assert.Equal(userInfo, appState.CurrentUser);
}

[Fact]
public async Task HandleUnauthorizedAsync_SecondCallDoesNotRunBeforeStateUpdates()
{
    var authService = new Mock<IAuthService>();
    var accessTokenCache = new Mock<IAccessTokenCache>();
    var apiClientFactory = new Mock<IApiClientFactory>();
    var dialogService = new Mock<IDialogService>();
    var appState = new AppState
    {
        IsAuthenticated = true,
        CurrentUser = new UserDto(userName: "tester", email: "tester@example.com", labs: []),
        AvailableLabs = [new UserLabDto(labId: Guid.NewGuid(), labName: "Lab")],
        CurrentLabId = Guid.NewGuid(),
        CurrentLabName = "Lab",
        AuthNotice = string.Empty
    };
    var settingsUpdater = new SettingsUpdater(Mock.Of<ISettingsStore>(), new AppSettings());
    var logger = new Logger<AuthSessionManager>(new LoggerFactory());

    var manager = new AuthSessionManager(
        authService.Object,
        accessTokenCache.Object,
        apiClientFactory.Object,
        appState,
        settingsUpdater,
        dialogService.Object,
        logger);

    await manager.HandleUnauthorizedAsync();
    await manager.HandleUnauthorizedAsync();

    authService.Verify(service => service.LogoutAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~AuthSessionManagerTests.InitializeAsync_UpdatesAppStateBeforeReturning|FullyQualifiedName~AuthSessionManagerTests.HandleUnauthorizedAsync_SecondCallDoesNotRunBeforeStateUpdates"`

Expected: FAIL because `Dispatcher.UIThread.Post` is async and state is not set immediately; logout may be invoked twice.

**Step 3: Write minimal implementation**

No production changes in this plan. This plan only adds tests; fixes are out of scope.

**Step 4: Run test to verify it passes**

N/A for this plan (will remain failing until behavior is fixed).

**Step 5: Commit**

```bash
git add desktop/Quater.Desktop.Tests/Auth/AuthSessionManagerTests.cs
git commit -m "test: cover auth state timing and unauthorized gating"
```

### Task 2: Add failing cancellation propagation test (non-brittle)

**Files:**
- Modify: `desktop/Quater.Desktop.Tests/Api/ApiClientHooksTests.cs`

**Step 1: Write the failing test**

Add a test that triggers `ApiClient.GetAsync` (public API) and verifies that a canceled token provider does not surface `OperationCanceledException` from `InterceptRequest`. To avoid real HTTP, use an invalid base URL and assert that the exception is not `OperationCanceledException`.

```csharp
[Fact]
public async Task GetAsync_WhenTokenProviderCancels_DoesNotSurfaceOperationCanceledException()
{
    ApiClient.AccessTokenProvider = ct => Task.FromCanceled<string?>(ct);
    var client = new Quater.Desktop.Api.Client.ApiClient("http://127.0.0.1:0");
    var options = new RequestOptions();

    var exception = await Record.ExceptionAsync(async () =>
    {
        await client.GetAsync<object>("/health/live", options, cancellationToken: CancellationToken.None);
    });

    Assert.NotNull(exception);
    Assert.IsNotType<OperationCanceledException>(exception);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~ApiClientHooksTests.GetAsync_WhenTokenProviderCancels_DoesNotSurfaceOperationCanceledException"`

Expected: FAIL with `OperationCanceledException` due to rethrow in `InterceptRequest`.

**Step 3: Write minimal implementation**

No production changes in this plan. Tests only.

**Step 4: Run test to verify it passes**

N/A for this plan.

**Step 5: Commit**

```bash
git add desktop/Quater.Desktop.Tests/Api/ApiClientHooksTests.cs
git commit -m "test: cover canceled token provider behavior"
```

### Task 3: Add failing soft-delete query filter tests

**Files:**
- Create: `desktop/Quater.Desktop.Tests/Repositories/SoftDeleteQueryFilterTests.cs`

**Step 1: Write the failing tests**

Create a new test file with SQLite in-memory context (pattern from `SampleRepositoryDeleteTests`). Insert `Lab`, `Parameter`, and `TestResult` rows via raw SQL with `IsDeleted=1` and required fields (including `RowVersion` and `IsSynced`). Assert normal queries return null while `IgnoreQueryFilters()` returns the row.

```csharp
public sealed class SoftDeleteQueryFilterTests : IDisposable
{
    private readonly QuaterLocalContext _context;

    public SoftDeleteQueryFilterTests()
    {
        var options = new DbContextOptionsBuilder<QuaterLocalContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new QuaterLocalContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task Lab_IsSoftDeleted_FilterExcludesRow()
    {
        var labId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var labIdStr = labId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Labs (Id, Name, IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES ('{labIdStr}', 'Test Lab', 1, '{now}', '{createdBy}', 1, X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.Labs.FirstOrDefaultAsync(l => l.Id == labId);
        var ignored = await _context.Labs.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == labId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.True(ignored!.IsDeleted);
    }

    // Repeat for Parameter and TestResult with required fields and IsDeleted=1
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SoftDeleteQueryFilterTests"`

Expected: FAIL if filters are missing or not applied as expected.

**Step 3: Write minimal implementation**

No production changes in this plan.

**Step 4: Run test to verify it passes**

N/A for this plan.

**Step 5: Commit**

```bash
git add desktop/Quater.Desktop.Tests/Repositories/SoftDeleteQueryFilterTests.cs
git commit -m "test: cover soft-delete query filters"
```

### Task 4: Add failing key cleanup test

**Files:**
- Modify: `desktop/Quater.Desktop.Tests/Auth/SecureFileTokenStoreTests.cs`

**Step 1: Write the failing test**

Add a test that asserts `ClearAsync` removes the key file.

```csharp
[Fact]
public async Task ClearAsync_DeletesKeyFile()
{
    var store = CreateStore();
    await store.SaveAsync(SampleToken());

    var keyPath = Path.Combine(_tempDir, "quater-keystore");
    Assert.True(File.Exists(keyPath));

    await store.ClearAsync();

    Assert.False(File.Exists(keyPath));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test desktop/Quater.Desktop.Tests/Quater.Desktop.Tests.csproj --filter "FullyQualifiedName~SecureFileTokenStoreTests.ClearAsync_DeletesKeyFile"`

Expected: FAIL because `ClearAsync` only deletes the token file.

**Step 3: Write minimal implementation**

No production changes in this plan.

**Step 4: Run test to verify it passes**

N/A for this plan.

**Step 5: Commit**

```bash
git add desktop/Quater.Desktop.Tests/Auth/SecureFileTokenStoreTests.cs
git commit -m "test: cover token key cleanup"
```

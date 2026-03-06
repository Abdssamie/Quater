# Desktop Codebase Audit

> Generated: 2026-03-03  
> Scope: `desktop/src/` — all three projects + tests  
> Method: five parallel subagent scans (Core infra, Features, Data/API, AXAML/UI, Tests/Build)

---

## Verdict

The codebase is **salvageable without a full rewrite**, but it has a cluster of issues that will cause real problems if not addressed before the next feature sprint. The structure (layering, interfaces, DI, MVVM) is sound — the AI agents got the skeleton right. What's broken is the *flesh*: stubs masquerading as implementations, static global state mutations in wrong layers, security weaknesses, thread-safety gaps, and a test suite that covers less than 5% of the codebase.

Priority: fix the **Critical** and **High** items below before adding any new features.

---

## Issue Registry

Each item has a severity tag:

- `[CRITICAL]` — correctness or security bug, broken in production today
- `[HIGH]` — architectural flaw that will cause growing pain or data loss
- `[MEDIUM]` — design smell, duplication, or wrong-layer concern
- `[LOW]` — style, consistency, or minor quality issue

---

## CRITICAL

### C1 — `ShowConfirmationAsync` always returns `true` (never shows UI)
**File:** `Core/Dialogs/SukiDialogService.cs`  
`ShowConfirmationAsync` logs and returns `Task.FromResult(true)` without showing any dialog.  
`ShowAlertAsync` is a silent no-op. `ShowInputAsync` returns `defaultValue` immediately.  
Any delete confirmation in `SampleListViewModel` is bypassed — samples are deleted without asking the user.

### C2 — `SampleRepository.DeleteAsync` performs a hard DELETE
**File:** `Quater.Desktop.Data/Repositories/SampleRepository.cs`  
Calls `context.Samples.Remove(sample)` despite every entity having `IsDeleted / DeletedAt / DeletedBy` columns.  
The comment says "soft delete via interceptor" but no interceptor exists. Rows are permanently destroyed.

### C3 — Token encryption key is trivially derivable
**File:** `Core/Auth/Storage/SecureFileTokenStore.cs`  
`GetKey()` computes `SHA256(MachineName + ":" + UserName + ":quater")`. Any process running as the same OS user can compute this key independently and decrypt the stored token file.  
On Windows this should use DPAPI (`ProtectedData`). AES-CBC with no HMAC is also vulnerable to padding-oracle attacks — should use AES-GCM (`AesGcm`).

### C4 — `RequireIdentityTokenSignature = false` in production code
**File:** `Core/Auth/Services/OidcClientFactory.cs`  
Identity token signatures are never validated. A `// TODO: Enable in production` comment has been left unresolved.  
Also: `ClientId = "quater-mobile-client"` for a desktop app — wrong client ID.

### C5 — `AppState` properties set from background threads
**File:** `Core/State/AppState.cs`  
`AuthSessionManager.InitializeAsync` and similar async paths set `appState.IsAuthenticated`, `appState.CurrentUser`, etc. from thread-pool threads. Avalonia UI bindings will fire `PropertyChanged` on those threads, causing cross-thread exceptions. Every mutation of `AppState` from a non-UI context needs `Dispatcher.UIThread.Post`.

### C6 — `LoopbackBrowser.InvokeAsync` hangs if user closes the browser
**File:** `Core/Auth/Browser/LoopbackBrowser.cs`  
`listener.GetContextAsync()` does not accept a `CancellationToken`. If the user closes the system browser without completing OIDC, the app freezes indefinitely on the login screen.

### C7 — `InterceptRequest` sync-over-async deadlock risk
**File:** `Quater.Desktop.Api/Client/ApiClientHooks.cs`  
`InterceptRequest` is `partial void` (synchronous) and calls `tokenProvider(CancellationToken.None).GetAwaiter().GetResult()`. On the Avalonia UI thread (which has a dispatcher `SynchronizationContext`) this will deadlock if `AccessTokenCache.RefreshAsync` is triggered.

---

## HIGH

### H1 — Settings loaded and `GlobalConfiguration` set three times at startup
**Files:** `App.axaml.cs`, `Core/Startup/ApplicationStartupService.cs`, `Core/Settings/SettingsUpdater.cs`  
`JsonSettingsStore.LoadAsync` is called synchronously in `App.axaml.cs` (`.GetAwaiter().GetResult()`), then again in `ApplicationStartupService.InitializeAsync`. `GlobalConfiguration.Instance` is mutated in all three places. One canonical path should own this.

### H2 — `SukiNavigationService.NavigateTo` never calls `InitializeAsync`
**File:** `Core/Navigation/SukiNavigationService.cs`  
`ViewModelBase` defines the `InitializeAsync` lifecycle hook but navigation never invokes it. Navigating to `DashboardViewModel` or `SampleListViewModel` from the sidebar always shows empty/stale data because `LoadSamplesAsync` / `LoadStatsAsync` are never triggered.

### H3 — Dashboard is entirely hardcoded mock data
**File:** `Features/Dashboard/DashboardViewModel.cs`  
`LoadStatsAsync` and `LoadRecentSamplesAsync` return static strings and `Task.CompletedTask`. The "chart" in `DashboardView.axaml` is six static `Border` elements with fixed heights — never data-driven. Status bar shows hardcoded `"Backend: Connected (24ms)"`.

### H4 — `SampleListViewModel.CreateSample()` and `EditSample()` are empty stubs
**File:** `Features/Samples/List/SampleListViewModel.cs`  
Both command handlers have no implementation. Buttons render, are clickable, and do nothing.

### H5 — Search box in `SampleListView` does nothing
**File:** `Features/Samples/List/SampleListViewModel.cs`  
`_searchText` is declared and bound in XAML but never passed to `ISampleRepository.GetFilteredAsync`. Typing in the search field has no effect.

### H6 — Sidebar navigation is not wired to navigation commands
**File:** `MainWindow.axaml`  
Both `SukiSideMenuItem` entries bind `PageContent` to the same `{Binding CurrentView}` but no command changes `CurrentView` when the user clicks a menu item. Clicking "Samples" in the sidebar does not navigate to `SampleListViewModel`. Navigation is effectively unreachable from the UI after the initial load.

### H7 — `async void` in `SplashViewModel.OnOnboardingCompleted`
**File:** `Core/Splash/SplashViewModel.cs`  
`async void` event handlers swallow exceptions, crashing the process with no recovery. Should use `async Task` with an explicit catch/log wrapper.

### H8 — `AccessTokenCache.StartAutoRefresh` has a TOCTOU race
**File:** `Core/Api/AccessTokenCache.cs`  
`if (_autoRefreshTask is not null)` check is not protected by any lock. Two threads can both pass the check and launch duplicate refresh loops. `StopAutoRefresh` doesn't `await` the running task before nulling the reference, so a second loop can start while the first is still shutting down.

### H9 — `AuthSessionManager` violates SRP with 7 constructor dependencies
**File:** `Core/Auth/Services/AuthSessionManager.cs`  
Manages token cache, calls Users API, applies user info to state, manages lab selection, saves settings, shows dialogs, and handles unauthorized responses. Should be split (e.g., `UserSessionService`, `LabContextService`).

### H10 — `Core/Startup` layer has hard references to Feature ViewModels
**File:** `Core/Startup/ApplicationStartupService.cs`  
`RegisterNavigationRoutes()` directly references `DashboardViewModel` and `SampleListViewModel`. The Core layer must not depend on the Features layer. Routes should self-register via an `INavigationRoute[]` pattern.

### H11 — Three complete features are missing
`Features/Reports/`, `Features/Settings/`, `Features/TestResults/` — folders exist, zero code inside. `QuestPDF` is already referenced in the `.csproj` as a dead dependency.

### H12 — No global soft-delete query filter
**File:** `Quater.Desktop.Data/QuaterLocalContext.cs`  
`IsDeleted` filtering is manually applied per-query. A missing `HasQueryFilter(e => !e.IsDeleted)` means any future query that forgets the filter will silently return deleted records.

### H13 — No offline sync engine
The schema is fully prepared for sync (`IsSynced`/`LastSyncedAt` shadow properties, composite indexes, `RowVersion` concurrency tokens, `SyncLogs` table was created then dropped) but no `ISyncService`, background worker, or conflict resolution code exists anywhere. Sync is 0% implemented despite the schema investment.

---

## MEDIUM

### M1 — `SukiDialogService` stub breaks `_isHandlingUnauthorized` reentrance guard
**File:** `Core/Auth/Services/AuthSessionManager.cs`  
`_isHandlingUnauthorized` is a plain `bool` field used as a reentrance guard across async calls. Not thread-safe — two concurrent `HandleUnauthorizedAsync` calls from different threads can both pass the check before either sets it. Use `Interlocked.CompareExchange`.

### M2 — `ShellViewModel` has dead injected fields
**File:** `Core/Shell/ShellViewModel.cs`  
`_settingsStore` is injected and assigned but never read. `_connectionStatus` and `_syncStatus` are `[ObservableProperty]` fields initialized to hardcoded strings and never updated. The shell UI shows "Connected" and "Up to Date" forever. Also: 7 constructor dependencies — same SRP issue as `AuthSessionManager`.

### M3 — `SettingsUpdater` mutates `GlobalConfiguration` static and exposes mutable `AppSettings`
**File:** `Core/Settings/SettingsUpdater.cs`  
`UpdateBackendUrlAsync` mutates `GlobalConfiguration.Instance` (a concern that belongs in API client setup, not settings persistence). `SettingsUpdater.Current` exposes the live `AppSettings` reference directly, allowing callers to mutate it without going through the updater's normalization/persistence logic.

### M4 — `AppState` is a god object mixing three unrelated concerns
**File:** `Core/State/AppState.cs`  
Auth state (`IsAuthenticated`, `CurrentUser`), lab context (`CurrentLabId`, `CurrentLabName`, `AvailableLabs`), and sync/connectivity status (`IsSyncing`, `IsOffline`, `ConnectionStatus`, `PendingSyncCount`) should be three separate observable state objects.

### M5 — URL normalization is duplicated
`SettingsUpdater.UpdateBackendUrlAsync` normalizes via trim + `TrimEnd('/')`. `OnboardingViewModel.TryNormalizeUrl` normalizes via `Uri.GetLeftPart(UriPartial.Authority)` + `TrimEnd('/')`. Two code paths, one canonical path needed.

### M6 — `AuthService` 60-second refresh buffer duplicated
**Files:** `Core/Auth/Services/AuthService.cs`, `Core/Api/AccessTokenCache.cs`  
Both hardcode 60 seconds as the token expiry buffer. Should be a single named constant.

### M7 — `RestoreLastUsedLab` and `SaveLastUsedLabAsync` are near-identical
**File:** `Core/Auth/Services/AuthSessionManager.cs`  
Same 4-line block duplicated — only difference is that `SaveLastUsedLabAsync` also calls `settingsUpdater.SaveAsync`. Should be unified.

### M8 — Direct call to `ApiClient.ResetUnauthorizedSignal()` bypasses all abstractions
**File:** `Core/Auth/Services/AuthSessionManager.cs`  
A concrete implementation detail from the generated client library is called directly from the session manager. Should be encapsulated in `IAccessTokenCache` or `IApiClientFactory`.

### M9 — `AppSettings.LastUsedLabId` and `AppState.CurrentLabId` are manually synchronized across three classes
Synchronization logic scattered across `AuthSessionManager`, `ShellViewModel`, and `SettingsUpdater`. A single `LabContextService` should own this.

### M10 — `ApiClientFactory` constructor sets static delegates as a side effect
**File:** `Core/Api/ApiClientFactory.cs`  
`ApiClient.AccessTokenProvider` and `ApiClient.LabIdProvider` (static) are set in the constructor. Invisible side effect, untestable, breaks if the factory is constructed more than once. The private `_tokenCache` field is dead — set but never read after construction.

### M11 — `IsSynced` magic string used in three places
**File:** `Quater.Desktop.Data/Repositories/SampleRepository.cs`  
`"IsSynced"` is a string literal. Rename it and it silently breaks at runtime. Extract to a constant.

### M12 — `Console.WriteLine` in API hot path
**File:** `Quater.Desktop.Api/Client/ApiClientHooks.cs`  
6 `Console.WriteLine` calls in `InterceptRequest`/`InterceptResponse` bypass Serilog and will produce noisy output on every API request in production.

### M13 — `Generated.backup/` committed to repository
**File:** `Quater.Desktop.Api/Generated.backup/`  
Stale generated code directory should not be in source control. Add to `.gitignore` and delete.

### M14 — `AddQuaterLogging` does not modify `IServiceCollection`
**File:** `Core/ServiceCollectionExtensions.cs`  
The method mutates `Log.Logger` (Serilog static) but returns `services` unchanged. The method name implies DI registration but performs none. Misleading extension method API.

### M15 — Migration naming misleads
Migration `AddUserInvitations` does not add a `UserInvitations` table — it drops `MoroccanThreshold` and renames `WhoThreshold` → `Threshold`. The name is factually wrong and will confuse anyone reading migration history.

---

## LOW

### L1 — `SplashWindow.axaml.cs` manually overrides the source-generated `InitializeComponent`
**File:** `Core/Splash/SplashWindow.axaml.cs`  
Old `AvaloniaXamlLoader.Load(this)` pattern. `LoginView.axaml.cs` and `OnboardingView.axaml.cs` do the same. `DashboardView`, `SampleListView`, and `SplashView` correctly use the modern `InitializeComponent()` pattern. Should be consistent across all views.

### L2 — `BoolInvertConverter` and `BoolNotConverter` are duplicates
Both invert a boolean. Subtle null-handling difference (`false` vs. pass-through) that no caller relies on. Consolidate to one converter.

### L3 — Converters declared as local resources in each view instead of `App.axaml`
`BoolInvertConverter` and `StringNotEmptyConverter` are instantiated independently in `LoginView`, `OnboardingView`, and `MainWindow`. Should be declared once in `App.axaml` application resources.

### L4 — `LoginView` and `OnboardingView` use hardcoded hex colors
`Background="#FFFFFF"`, `Foreground="#C0392B"`, `BorderBrush="#E3E6E8"` bypass SukiUI semantic tokens. The dashboard uses `DynamicResource SukiPrimaryColor` correctly. The onboarding flow will break visually if a dark theme is ever enabled.

### L5 — `AppSettings` backed by relative DB path default
`AddQuaterData` has `dbPath = "Data Source=quater.db"` — a relative path that resolves differently depending on working directory. Should use `Environment.SpecialFolder.ApplicationData`.

### L6 — `ApplicationStartupService` log message is backwards
Line after early return for onboarding logs `"Needs onboarding"` — this line is only reached when onboarding is **not** needed.

### L7 — `IApplicationStartupService.cs` defines two types in one file
`IApplicationStartupService` and `StartupResult` should each have their own file.

### L8 — `ApiHeaders.cs` is an empty file
Zero bytes, dead code.

### L9 — French-language mock data in otherwise English codebase
`"Conforme"`, `"Non Conforme"`, `"Source Rivière A"`, `"Puits #4"` in `DashboardViewModel`. Remove when real data is wired.

### L10 — `SplashWindow` is dead code
`SplashWindow.axaml` / `SplashWindow.axaml.cs` are never referenced anywhere in the startup flow. The actual splash uses `SplashView` inside `MainWindow`. Either delete or document.

### L11 — `DashboardStat.IsGood` / `IsBad` flags are declared but never bound
`DashboardViewModel.cs` declares these record fields but no AXAML binding reads them.

### L12 — `OidcClientFactory` hardcodes port 7890 for the loopback callback
**File:** `Core/Auth/Services/OidcClientFactory.cs`, `Core/Auth/Browser/LoopbackBrowser.cs`  
If port 7890 is in use, the login flow silently fails. Should use dynamic port selection.

### L13 — `Quater.Desktop.Tests/exports/` is an empty directory
Suggests planned test-artifact output that was never implemented. Remove or document.

### L14 — Build artifacts may be committed
`bin/`, `obj/`, `quater.db`, `run.log` appear inside `src/Quater.Desktop/`. Verify `.gitignore` excludes them.

### L15 — `Directory.Build.props` is too sparse
Missing `<TreatWarningsAsErrors>`, `<LangVersion>`, shared `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`. Each project re-declares these independently.

---

## Test Coverage Summary

**3 tests in 1 file** — `AuthSessionManager` partial coverage only.  
Estimated surface coverage: **< 5%**.

| Area | Tests | Priority |
|---|---|---|
| `AuthSessionManager` happy path + edge cases | 0 of ~8 scenarios covered | High |
| `AuthService` token expiry/refresh logic | 0 | High |
| `AccessTokenCache` concurrent refresh | 0 | High |
| `SecureFileTokenStore` crypto + I/O | 0 | High |
| `OnboardingViewModel.TryNormalizeUrl` | 0 (pure static function, trivially testable) | High |
| `SettingsUpdater` URL normalization | 0 | Medium |
| `SampleRepository` CRUD + soft-delete | 0 | Medium |
| `ShellViewModel` auth routing + lab selection | 0 | Medium |
| UI Converters (pure functions) | 0 | Low |
| `SukiNavigationService` | 0 | Low |

`coverlet.collector` is referenced in the test project but no coverage thresholds are configured or enforced.  
`Quater.Desktop.Tests.csproj` does not reference `Quater.Desktop.Data`, making data-layer tests structurally impossible without adding the reference.

---

## What Is Working Well

- Clear interface segregation throughout — every major service has a corresponding `IXxx`, enabling mocking
- `AccessTokenCache` concurrency primitives are mostly correct (SemaphoreSlim + lock)
- `AddQuater*` DI extension methods are composable and well-organized
- `AuthResult` and `TokenData` as immutable records are clean
- `AvaloniaUseCompiledBindingsByDefault=true` in the main `.csproj` is correct
- `x:DataType` declared on every view — compiled bindings used correctly in modern views
- Code-behind files are near-empty — no business logic bleeds into `.axaml.cs`
- EF Core entity design (RowVersion, soft-delete columns, value objects as `OwnsOne`) is solid
- Unix file permissions on the token file show platform awareness

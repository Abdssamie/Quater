using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.Settings;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Tests.Auth;

[Collection("UIThread")]
public sealed class AuthSessionManagerTests
{
    [Fact]
    public async Task InitializeAsync_WhenNoToken_SetsIsAuthenticatedFalse()
    {
        var authService = new Mock<IAuthService>();
        var accessTokenCache = new Mock<IAccessTokenCache>();
        var usersApi = new Mock<IUsersApi>();
        var apiClientFactory = new Mock<IApiClientFactory>();
        apiClientFactory.Setup(factory => factory.GetUsersApi()).Returns(usersApi.Object);
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState { IsAuthenticated = true };
        var settingsUpdater = new SettingsUpdater(Mock.Of<ISettingsStore>(), new AppSettings());
        var logger = new Logger<AuthSessionManager>(new LoggerFactory());

        accessTokenCache.SetupGet(cache => cache.CurrentToken).Returns((string?)null);
        authService.Setup(service => service.GetValidAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var manager = new AuthSessionManager(
            authService.Object,
            accessTokenCache.Object,
            apiClientFactory.Object,
            appState,
            settingsUpdater,
            dialogService.Object,
            logger);

        await manager.InitializeAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.False(appState.IsAuthenticated);
        usersApi.Verify(api => api.ApiUsersMeGetAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_WhenTokenValidAndUserHasLabs_SetsCurrentUserAndAvailableLabs()
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
        Dispatcher.UIThread.RunJobs();

        Assert.True(appState.IsAuthenticated);
        Assert.Equal(string.Empty, appState.AuthNotice);
        Assert.Equal(userInfo, appState.CurrentUser);
        Assert.Equal(labs, appState.AvailableLabs);
        Assert.Equal(labId, appState.CurrentLabId);
        Assert.Equal("Alpha Lab", appState.CurrentLabName);
    }

    [Fact]
    public async Task InitializeAsync_WhenApiExceptionThrown_SetsIsAuthenticatedFalse()
    {
        var authService = new Mock<IAuthService>();
        var accessTokenCache = new Mock<IAccessTokenCache>();
        var usersApi = new Mock<IUsersApi>();
        var apiClientFactory = new Mock<IApiClientFactory>();
        apiClientFactory.Setup(factory => factory.GetUsersApi()).Returns(usersApi.Object);
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState { IsAuthenticated = true };
        var settingsUpdater = new SettingsUpdater(Mock.Of<ISettingsStore>(), new AppSettings());
        var logger = new Logger<AuthSessionManager>(new LoggerFactory());

        accessTokenCache.SetupGet(cache => cache.CurrentToken).Returns("valid-token");
        usersApi.Setup(api => api.ApiUsersMeGetAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(401, "Unauthorized"));

        var manager = new AuthSessionManager(
            authService.Object,
            accessTokenCache.Object,
            apiClientFactory.Object,
            appState,
            settingsUpdater,
            dialogService.Object,
            logger);

        await manager.InitializeAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.False(appState.IsAuthenticated);
    }

    [Fact]
    public async Task HandleLoginSuccessAsync_WhenSuccess_SetsIsAuthenticatedTrueAndClearsNotice()
    {
        var authService = new Mock<IAuthService>();
        var accessTokenCache = new Mock<IAccessTokenCache>();
        var usersApi = new Mock<IUsersApi>();
        var apiClientFactory = new Mock<IApiClientFactory>();
        apiClientFactory.Setup(factory => factory.GetUsersApi()).Returns(usersApi.Object);
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState
        {
            IsAuthenticated = false,
            AuthNotice = "Session expired"
        };
        var settingsUpdater = new SettingsUpdater(Mock.Of<ISettingsStore>(), new AppSettings());
        var userInfo = new UserDto(userName: "tester", email: "tester@example.com", labs: []);
        var logger = new Logger<AuthSessionManager>(new LoggerFactory());

        accessTokenCache.SetupGet(cache => cache.CurrentToken).Returns("token");
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

        await manager.HandleLoginSuccessAsync(new AuthResult(false, null, "token", "refresh", DateTime.UtcNow.AddHours(1)));
        Dispatcher.UIThread.RunJobs();

        Assert.True(appState.IsAuthenticated);
        Assert.Equal(string.Empty, appState.AuthNotice);
    }

    [Fact]
    public async Task HandleLoginSuccessAsync_SetsCurrentUserFromApiResponse()
    {
        var authService = new Mock<IAuthService>();
        var accessTokenCache = new Mock<IAccessTokenCache>();
        var usersApi = new Mock<IUsersApi>();
        var apiClientFactory = new Mock<IApiClientFactory>();
        apiClientFactory.Setup(factory => factory.GetUsersApi()).Returns(usersApi.Object);
        var dialogService = new Mock<IDialogService>();
        var appState = new AppState { IsAuthenticated = false };
        var settingsUpdater = new SettingsUpdater(Mock.Of<ISettingsStore>(), new AppSettings());
        var labId = Guid.NewGuid();
        var labs = new List<UserLabDto> { new UserLabDto(labId: labId, labName: "Beta Lab") };
        var userInfo = new UserDto(userName: "chemist", email: "chemist@lab.com", labs: labs);
        var logger = new Logger<AuthSessionManager>(new LoggerFactory());

        accessTokenCache.SetupGet(cache => cache.CurrentToken).Returns("token");
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

        await manager.HandleLoginSuccessAsync(new AuthResult(false, null, "token", "refresh", DateTime.UtcNow.AddHours(1)));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(userInfo, appState.CurrentUser);
        Assert.Equal(labs, appState.AvailableLabs);
        Assert.Equal(labId, appState.CurrentLabId);
        Assert.Equal("Beta Lab", appState.CurrentLabName);
    }

    [Fact]
    public async Task HandleUnauthorizedAsync_ClearsStateAndSetsNotice()
    {
        var authService = new Mock<IAuthService>();
        var accessTokenCache = new Mock<IAccessTokenCache>();
        var usersApi = new Mock<IUsersApi>();
        var apiClientFactory = new Mock<IApiClientFactory>();
        apiClientFactory.Setup(factory => factory.GetUsersApi()).Returns(usersApi.Object);
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
        Dispatcher.UIThread.RunJobs();

        Assert.False(appState.IsAuthenticated);
        Assert.Null(appState.CurrentUser);
        Assert.Empty(appState.AvailableLabs);
        Assert.Equal(Guid.Empty, appState.CurrentLabId);
        Assert.Equal(string.Empty, appState.CurrentLabName);
        Assert.Equal("Session expired. Please sign in again.", appState.AuthNotice);
        accessTokenCache.Verify(cache => cache.Clear(), Times.Once);
        accessTokenCache.Verify(cache => cache.StopAutoRefresh(), Times.Once);
        authService.Verify(service => service.LogoutAsync(It.IsAny<CancellationToken>()), Times.Once);
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
}

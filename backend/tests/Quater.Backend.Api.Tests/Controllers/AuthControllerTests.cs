using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Backend.Core.Constants;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Api.Tests.Controllers;

/// <summary>
/// Integration tests for AuthController.
/// Tests OAuth2 token endpoint, logout, and user info endpoints.
/// </summary>
[Collection("Api")]
public sealed class AuthControllerTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private const string TestPassword = "Test123!@#456";
    private const string TestEmail = "test@example.com";
    private const string InactiveUserEmail = "inactive@example.com";
    private const string LockedUserEmail = "locked@example.com";

    public AuthControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.Client;
    }

    public async Task InitializeAsync()
    {
        // Reset database and Redis before each test
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();

        // Create test lab
        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Test Lab",
            Location = "123 Test St, Test City, Test Country",
            ContactInfo = "lab@test.com, 123-456-7890",
            IsActive = true
        };
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        // Create active test user
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = TestEmail,
            Email = TestEmail,
            EmailConfirmed = true,
            Role = UserRole.Technician,
            LabId = lab.Id,
            IsActive = true
        };
        var result = await userManager.CreateAsync(activeUser, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Create inactive test user
        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = InactiveUserEmail,
            Email = InactiveUserEmail,
            EmailConfirmed = true,
            Role = UserRole.Viewer,
            LabId = lab.Id,
            IsActive = false
        };
        result = await userManager.CreateAsync(inactiveUser, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create inactive user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Create locked out test user
        var lockedUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = LockedUserEmail,
            Email = LockedUserEmail,
            EmailConfirmed = true,
            Role = UserRole.Admin,
            LabId = lab.Id,
            IsActive = true
        };
        result = await userManager.CreateAsync(lockedUser, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create locked user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Lock out the user
        await userManager.SetLockoutEndDateAsync(lockedUser, DateTimeOffset.UtcNow.AddHours(1));
    }

    public Task DisposeAsync()
    {
        _client.RemoveAuthToken();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Token_PasswordGrant_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", TestEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
        tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
        tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify JWT claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenResponse.AccessToken);

        jwtToken.Claims.Should().Contain(c => c.Type == OpenIddictConstants.Claims.Subject);
        jwtToken.Claims.Should().Contain(c => c.Type == OpenIddictConstants.Claims.Email && c.Value == TestEmail);
        jwtToken.Claims.Should().Contain(c => c.Type == QuaterClaimTypes.Role && c.Value == UserRole.Technician.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == QuaterClaimTypes.LabId);
    }

    [Fact]
    public async Task Token_PasswordGrant_ValidCredentials_UpdatesLastLogin()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", TestEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        var beforeLogin = DateTime.UtcNow;

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify LastLogin was updated
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(TestEmail);

        user.Should().NotBeNull();
        user!.LastLogin.Should().NotBeNull();
        user.LastLogin.Should().BeOnOrAfter(beforeLogin);
    }

    [Fact]
    public async Task Token_PasswordGrant_InvalidUsername_ReturnsForbidden()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "nonexistent@example.com"),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
        errorResponse.ErrorDescription.Should().Contain("username or password is invalid");
    }

    [Fact]
    public async Task Token_PasswordGrant_InvalidPassword_ReturnsForbidden()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", TestEmail),
            new KeyValuePair<string, string>("password", "WrongPassword123!"),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
        errorResponse.ErrorDescription.Should().Contain("username or password is invalid");
    }

    [Fact]
    public async Task Token_PasswordGrant_InvalidPassword_IncrementsAccessFailedCount()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", TestEmail),
            new KeyValuePair<string, string>("password", "WrongPassword123!"),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        // Act
        await _client.PostAsync("/api/auth/token", request);

        // Assert
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(TestEmail);

        user.Should().NotBeNull();
        var failedCount = await userManager.GetAccessFailedCountAsync(user!);
        failedCount.Should().Be(1);
    }

    [Fact]
    public async Task Token_PasswordGrant_SuccessfulLogin_ResetsAccessFailedCount()
    {
        // Arrange - First fail a login to increment the counter
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(TestEmail);
        await userManager.AccessFailedAsync(user!);
        await userManager.AccessFailedAsync(user!);

        var failedCountBefore = await userManager.GetAccessFailedCountAsync(user!);
        failedCountBefore.Should().Be(2);

        // Act - Successful login
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", TestEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify AccessFailedCount was reset
        user = await userManager.FindByEmailAsync(TestEmail);
        var failedCountAfter = await userManager.GetAccessFailedCountAsync(user!);
        failedCountAfter.Should().Be(0);
    }

    [Fact]
    public async Task Token_PasswordGrant_InactiveUser_ReturnsForbidden()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", InactiveUserEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
        errorResponse.ErrorDescription.Should().Contain("account is inactive");
    }

    [Fact]
    public async Task Token_PasswordGrant_LockedOutUser_ReturnsForbidden()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", LockedUserEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
        errorResponse.ErrorDescription.Should().Contain("locked out");
    }

    [Fact]
    public async Task Token_PasswordGrant_MissingUsername_ReturnsForbidden()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidRequest);
        errorResponse.ErrorDescription.Should().Contain("username is required");
    }

    [Fact]
    public async Task Token_RefreshTokenGrant_ValidToken_ReturnsNewToken()
    {
        // Arrange - First get a token with refresh token
        var loginRequest = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", TestEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        var loginResponse = await _client.PostAsync("/api/auth/token", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        var loginTokenResponse = JsonSerializer.Deserialize<TokenResponse>(loginJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var refreshToken = loginTokenResponse!.RefreshToken;
        refreshToken.Should().NotBeNullOrEmpty();

        // Act - Use refresh token to get new access token
        var refreshRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken!)
        ]);

        var refreshResponse = await _client.PostAsync("/api/auth/token", refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
        var refreshTokenResponse = JsonSerializer.Deserialize<TokenResponse>(refreshJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        refreshTokenResponse.Should().NotBeNull();
        refreshTokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        refreshTokenResponse.AccessToken.Should().NotBe(loginTokenResponse.AccessToken); // New token
        refreshTokenResponse.TokenType.Should().Be("Bearer");
        refreshTokenResponse.ExpiresIn.Should().BeGreaterThan(0);
        refreshTokenResponse.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify JWT claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(refreshTokenResponse.AccessToken);

        jwtToken.Claims.Should().Contain(c => c.Type == OpenIddictConstants.Claims.Subject);
        jwtToken.Claims.Should().Contain(c => c.Type == OpenIddictConstants.Claims.Email && c.Value == TestEmail);
        jwtToken.Claims.Should().Contain(c => c.Type == QuaterClaimTypes.Role && c.Value == UserRole.Technician.ToString());
    }

    [Fact]
    public async Task Token_RefreshTokenGrant_InvalidToken_ReturnsForbidden()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", "invalid_refresh_token_12345")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
        errorResponse.ErrorDescription.Should().Contain("refresh token is invalid");
    }

    [Fact]
    public async Task Token_RefreshTokenGrant_InactiveUser_ReturnsForbidden()
    {
        // Arrange - First get a token for the user while they're active
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(InactiveUserEmail);
        user!.IsActive = true;
        await userManager.UpdateAsync(user);

        var loginRequest = new FormUrlEncodedContent(
        [
                        new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", InactiveUserEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        var loginResponse = await _client.PostAsync("/api/auth/token", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        var loginTokenResponse = JsonSerializer.Deserialize<TokenResponse>(loginJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var refreshToken = loginTokenResponse!.RefreshToken;

        // Now deactivate the user
        user.IsActive = false;
        await userManager.UpdateAsync(user);

        // Act - Try to refresh token with inactive user
        var refreshRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken!)
        ]);

        var refreshResponse = await _client.PostAsync("/api/auth/token", refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await refreshResponse.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
        errorResponse.ErrorDescription.Should().Contain("account is inactive");
    }

    [Fact]
    public async Task Token_UnsupportedGrantType_ReturnsForbidden()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", "test_client"),
            new KeyValuePair<string, string>("client_secret", "test_secret")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.UnsupportedGrantType);
        errorResponse.ErrorDescription.Should().Contain("grant type is not supported");
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_RevokesTokens()
    {
        // Arrange - Login to get token
        var token = await AuthenticationHelper.GetAuthTokenAsync(_client, TestEmail, TestPassword);
        _client.AddAuthToken(token);

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var logoutResponse = JsonSerializer.Deserialize<LogoutResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        logoutResponse.Should().NotBeNull();
        logoutResponse!.Message.Should().Contain("Logged out successfully");
        logoutResponse.TokensRevoked.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Logout_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - No authentication token

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserInfo_AuthenticatedUser_ReturnsUserData()
    {
        // Arrange - Login to get token
        var token = await AuthenticationHelper.GetAuthTokenAsync(_client, TestEmail, TestPassword);
        _client.AddAuthToken(token);

        // Act
        var response = await _client.GetAsync("/api/auth/userinfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        userInfo.Should().NotBeNull();
        userInfo!.Id.Should().NotBeEmpty();
        userInfo.Email.Should().Be(TestEmail);
        userInfo.UserName.Should().Be(TestEmail);
        userInfo.Role.Should().Be(UserRole.Technician.ToString());
        userInfo.LabId.Should().NotBeEmpty();
        userInfo.IsActive.Should().BeTrue();
        userInfo.LastLogin.Should().NotBeNull();
    }

    [Fact]
    public async Task UserInfo_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - No authentication token

        // Act
        var response = await _client.GetAsync("/api/auth/userinfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserInfo_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange - Use invalid token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token_12345");

        // Act
        var response = await _client.GetAsync("/api/auth/userinfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #region Response Models

    private sealed class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
    }

    private sealed class OAuth2ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string ErrorDescription { get; set; } = string.Empty;
    }
    private sealed class LogoutResponse
    {
        public string Message { get; set; } = string.Empty;
        public int TokensRevoked { get; set; }
    }

    private sealed class UserInfoResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid LabId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    #endregion
}

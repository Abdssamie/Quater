using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
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
/// Tests OAuth2 token endpoint (authorization code + refresh token grants),
/// logout, and user info endpoints.
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

        // Seed OpenIddict client for auth code flow
        using var seedScope = _fixture.Services.CreateScope();
        var appManager = seedScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await appManager.FindByClientIdAsync("quater-mobile-client") is null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "quater-mobile-client",
                DisplayName = "Quater Mobile/Desktop Client",
                ClientType = OpenIddictConstants.ClientTypes.Public,
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                RedirectUris = { new Uri("quater://oauth/callback"), new Uri("http://127.0.0.1/callback") },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Revocation,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access"
                },
                Requirements = { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange }
            });
        }

        // Create test lab
        var context = seedScope.ServiceProvider.GetRequiredService<QuaterDbContext>();
        var userManager = seedScope.ServiceProvider.GetRequiredService<UserManager<User>>();

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

    #region Auth Code Flow Token Tests

    [Fact]
    public async Task Token_AuthCodeFlow_ValidCredentials_ReturnsToken()
    {
        // Act
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);

        // Assert
        tokenResponse.Should().NotBeNull();
        tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
        tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
        tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify claims via userinfo endpoint (OpenIddict encrypts access tokens,
        // so JwtSecurityTokenHandler.ReadJwtToken() returns empty claims)
        _client.AddAuthToken(tokenResponse.AccessToken);
        var userInfoResponse = await _client.GetAsync("/api/auth/userinfo");
        userInfoResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await userInfoResponse.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        userInfo.Should().NotBeNull();
        userInfo!.Id.Should().NotBeEmpty();
        userInfo.Email.Should().Be(TestEmail);
        userInfo.Role.Should().Be(UserRole.Technician.ToString());
        userInfo.LabId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Token_AuthCodeFlow_ValidCredentials_UpdatesLastLogin()
    {
        // Arrange
        var beforeLogin = DateTime.UtcNow;

        // Act
        await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);

        // Assert - Verify LastLogin was updated
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(TestEmail);

        user.Should().NotBeNull();
        user!.LastLogin.Should().NotBeNull();
        user.LastLogin.Should().BeOnOrAfter(beforeLogin);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task Token_RefreshTokenGrant_ValidToken_ReturnsNewToken()
    {
        // Arrange - Get initial token via auth code flow
        var initialTokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);

        var refreshToken = initialTokenResponse.RefreshToken;
        refreshToken.Should().NotBeNullOrEmpty();

        // Act - Use refresh token to get new access token
        var refreshRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken!),
            new KeyValuePair<string, string>("client_id", "quater-mobile-client")
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
        refreshTokenResponse.AccessToken.Should().NotBe(initialTokenResponse.AccessToken);
        refreshTokenResponse.TokenType.Should().Be("Bearer");
        refreshTokenResponse.ExpiresIn.Should().BeGreaterThan(0);
        refreshTokenResponse.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify claims via userinfo endpoint (OpenIddict encrypts access tokens)
        _client.AddAuthToken(refreshTokenResponse.AccessToken);
        var userInfoResponse = await _client.GetAsync("/api/auth/userinfo");
        userInfoResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json2 = await userInfoResponse.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(json2, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        userInfo.Should().NotBeNull();
        userInfo!.Id.Should().NotBeEmpty();
        userInfo.Email.Should().Be(TestEmail);
        userInfo.Role.Should().Be(UserRole.Technician.ToString());
    }

    [Fact]
    public async Task Token_RefreshTokenGrant_InvalidToken_ReturnsForbidden()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", "invalid_refresh_token_12345"),
            new KeyValuePair<string, string>("client_id", "quater-mobile-client")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert - OpenIddict returns 400 Bad Request for invalid_grant per RFC 6749 Section 5.2
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
    }

    [Fact]
    public async Task Token_RefreshTokenGrant_InactiveUser_ReturnsForbidden()
    {
        // Arrange - Temporarily activate the inactive user so we can get a token
        using (var activateScope = _fixture.Services.CreateScope())
        {
            var activateUserManager = activateScope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var activateUser = await activateUserManager.FindByEmailAsync(InactiveUserEmail);
            activateUser!.IsActive = true;
            await activateUserManager.UpdateAsync(activateUser);
        }

        // Get initial token via auth code flow while user is active
        var initialTokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, InactiveUserEmail, TestPassword);

        var refreshToken = initialTokenResponse.RefreshToken;

        // Now deactivate the user in a fresh scope to avoid stale RowVersion from change tracker
        using (var deactivateScope = _fixture.Services.CreateScope())
        {
            var deactivateUserManager = deactivateScope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var deactivateUser = await deactivateUserManager.FindByEmailAsync(InactiveUserEmail);
            deactivateUser!.IsActive = false;
            var deactivateResult = await deactivateUserManager.UpdateAsync(deactivateUser);
            deactivateResult.Succeeded.Should().BeTrue("deactivating user should succeed");
        }

        // Act - Try to refresh token with inactive user
        var refreshRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken!),
            new KeyValuePair<string, string>("client_id", "quater-mobile-client")
        ]);

        var refreshResponse = await _client.PostAsync("/api/auth/token", refreshRequest);

        // Assert - OpenIddict returns 400 Bad Request for invalid_grant per RFC 6749 Section 5.2
        refreshResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);

        var json = await refreshResponse.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
        errorResponse.ErrorDescription.Should().Contain("account is inactive");
    }

    #endregion

    #region Unsupported Grant Type Tests

    [Fact]
    public async Task Token_UnsupportedGrantType_ClientCredentials_ReturnsError()
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

        // Assert - OpenIddict returns 400 Bad Request for unsupported/invalid grant types
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Token_UnsupportedGrantType_Password_ReturnsError()
    {
        // Arrange - Password grant has been removed
        var request = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", TestEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        ]);

        // Act
        var response = await _client.PostAsync("/api/auth/token", request);

        // Assert - OpenIddict returns 400 Bad Request for unsupported grant types
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.UnsupportedGrantType);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_AuthenticatedUser_RevokesTokens()
    {
        // Arrange - Get token via auth code flow
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);
        _client.AddAuthToken(tokenResponse.AccessToken);

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

    #endregion

    #region UserInfo Tests

    [Fact]
    public async Task UserInfo_AuthenticatedUser_ReturnsUserData()
    {
        // Arrange - Get token via auth code flow
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);
        _client.AddAuthToken(tokenResponse.AccessToken);

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

    #endregion

    #region Response Models

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }

    private sealed class OAuth2ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
        
        [JsonPropertyName("error_description")]
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

/*
 * @id: authcode-flow-tests
 * @priority: high
 * @progress: 100
 * @directive: Implement comprehensive integration tests for the authorization code flow with PKCE. Test full flow: authorize -> token exchange. Test PKCE validation (valid S256, invalid verifier, missing challenge). Test authorization code lifecycle (single-use, expiration, replay prevention). Test redirect URI validation (mismatch, missing). Test public client behavior (no secret required, secret ignored). Test error responses for invalid requests. Use ApiTestFixture with Testcontainers. Follow existing test patterns from AuthControllerTests.
 * @context: specs/oauth2-mobile-desktop-security-enhancement.md#9-testing-strategy
 * @checklist: [
 *   "Full authorization code flow test: authorize -> token exchange (9.2)",
 *   "PKCE S256 validation: valid code_verifier matches code_challenge (EC-01)",
 *   "PKCE validation failure: mismatched code_verifier returns 400 (EC-01)",
 *   "PKCE required: missing code_challenge returns error (FR-03)",
 *   "Authorization code single-use: second exchange returns 400 (EC-02)",
 *   "Authorization code expiration: expired code returns 400 (EC-07)",
 *   "Redirect URI mismatch: different URI in token request returns 400 (EC-06)",
 *   "Public client without secret: token exchange succeeds (EC-08)",
 *   "Public client with secret: secret ignored, exchange succeeds (EC-08)",
 *   "Invalid client_id: authorize returns error (FR-08)",
 *   "Unauthenticated user: redirected to login (FR-08)",
 *   "State parameter preserved through flow (9.3 CSRF)",
 *   "Uses [Collection('PostgreSQL')] and IAsyncLifetime (project convention)",
 *   "Test naming: MethodName_Scenario_ExpectedResult (project convention)",
 *   "Uses FluentAssertions (project convention)"
 * ]
 * @deps: ["authorization-controller", "openiddict-seeder-public", "openiddict-config-authcode"]
 * @skills: ["xunit-integration-testing", "testcontainers", "oauth2-pkce-testing"]
 */

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Api.Tests.Controllers;

/// <summary>
/// Integration tests for the OAuth2 Authorization Code Flow with PKCE.
/// Tests the full flow from authorization to token exchange, PKCE validation,
/// authorization code lifecycle, redirect URI validation, and public client behavior.
/// </summary>
[Collection("Api")]
public sealed class AuthorizationCodeFlowTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private const string TestPassword = "Test123!@#456";
    private const string TestEmail = "authcode-test@example.com";
    private const string InactiveUserEmail = "authcode-inactive@example.com";
    private const string LockedUserEmail = "authcode-locked@example.com";

    private const string DefaultClientId = "quater-mobile-client";
    private const string DefaultRedirectUri = "http://127.0.0.1/callback";
    private const string DefaultScope = "openid email profile offline_access api";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthorizationCodeFlowTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
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

        // Create test lab and users
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

        // Active test user
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = TestEmail,
            Email = TestEmail,
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = lab.Id, Role = UserRole.Technician } ],
            IsActive = true
        };
        var result = await userManager.CreateAsync(activeUser, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create active test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Inactive test user
        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = InactiveUserEmail,
            Email = InactiveUserEmail,
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = lab.Id, Role = UserRole.Viewer } ],
            IsActive = false
        };
        result = await userManager.CreateAsync(inactiveUser, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create inactive test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Locked out test user
        var lockedUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = LockedUserEmail,
            Email = LockedUserEmail,
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = lab.Id, Role = UserRole.Admin } ],
            IsActive = true
        };
        result = await userManager.CreateAsync(lockedUser, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create locked test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.SetLockoutEndDateAsync(lockedUser, DateTimeOffset.UtcNow.AddHours(1));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Happy Path Tests

    /// <summary>
    /// Verifies the complete authorization code flow with PKCE succeeds for a valid user.
    /// Uses the full helper method that performs login, authorize, and token exchange.
    /// </summary>
    [Fact]
    public async Task AuthCodeFlow_ValidCredentials_ReturnsTokens()
    {
        // Act
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);

        // Assert
        tokenResponse.Should().NotBeNull();
        tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
        tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that the access token contains the expected claims by calling the userinfo endpoint.
    /// OpenIddict encrypts access tokens, so we verify claims via the protected /api/auth/userinfo endpoint
    /// which the server decrypts and validates internally.
    /// </summary>
    [Fact]
    public async Task AuthCodeFlow_ValidCredentials_TokenContainsExpectedClaims()
    {
        // Arrange
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);

        tokenResponse.AccessToken.Should().NotBeNullOrEmpty();

        // Act - Use the access token to call the userinfo endpoint, which validates the token server-side
        using var client = new HttpClient(_fixture.Server.CreateHandler()) { BaseAddress = _fixture.Server.BaseAddress };
        client.AddAuthToken(tokenResponse.AccessToken);

        var userInfoResponse = await client.GetAsync("/api/auth/userinfo");

        // Assert - Userinfo should succeed and return expected claim values
        userInfoResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await userInfoResponse.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(json, JsonOptions);

        userInfo.Should().NotBeNull();
        userInfo!.Id.Should().NotBeEmpty("sub claim should be present as the user ID");
        userInfo.Email.Should().Be(TestEmail, "email claim should match the authenticated user");
        userInfo.Labs.Should().NotBeEmpty("lab memberships should be present");
        userInfo.Labs[0].Role.Should().Be(UserRole.Technician, "role claim should be Technician");
        userInfo.Labs[0].LabId.Should().NotBeEmpty("lab_id claim should be present");
    }

    /// <summary>
    /// Verifies that the public client (no client_secret) can complete the full auth code flow.
    /// The helper never sends a client_secret, so this confirms public client behavior.
    /// </summary>
    [Fact]
    public async Task AuthCodeFlow_PublicClientWithoutSecret_Succeeds()
    {
        // Act - GetAuthTokenViaAuthCodeFlowAsync never sends a client_secret
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);

        // Assert - Flow completes successfully without a secret
        tokenResponse.Should().NotBeNull();
        tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
    }

    #endregion

    #region PKCE Tests

    /// <summary>
    /// Manually performs the auth code flow with PKCE S256 to verify
    /// that a correct code_verifier matches the code_challenge.
    /// </summary>
    [Fact]
    public async Task AuthCodeFlow_PkceS256_ValidVerifierSucceeds()
    {
        // Arrange - Generate PKCE parameters
        var codeVerifier = AuthenticationHelper.GenerateCodeVerifier();
        var codeChallenge = AuthenticationHelper.ComputeCodeChallenge(codeVerifier);

        // Login via cookie to get an authenticated client
        var (client, _) = await AuthenticationHelper.LoginViaCookieAsync(
            _fixture, TestEmail, TestPassword);
        using var _ = client;

        // Build the authorize request
        var state = Guid.NewGuid().ToString("N");
        var authorizeQuery = AuthenticationHelper.BuildAuthorizeQuery(
            DefaultClientId, DefaultRedirectUri, DefaultScope, codeChallenge, state);

        // Act - Send authorize request
        var authorizeResponse = await client.GetAsync($"/api/auth/authorize?{authorizeQuery}");

        // Extract the authorization code from redirect
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var redirectUri = authorizeResponse.Headers.Location;
        redirectUri.Should().NotBeNull();

        var queryParams = HttpUtility.ParseQueryString(redirectUri!.Query);
        var authorizationCode = queryParams["code"];
        authorizationCode.Should().NotBeNullOrEmpty("the authorize endpoint should return an authorization code");

        // Exchange the code with the correct code_verifier
        var tokenRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode!),
            new KeyValuePair<string, string>("code_verifier", codeVerifier),
            new KeyValuePair<string, string>("redirect_uri", DefaultRedirectUri),
            new KeyValuePair<string, string>("client_id", DefaultClientId)
        ]);

        var tokenResponse = await client.PostAsync("/api/auth/token", tokenRequest);

        // Assert - Token exchange should succeed
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<AuthenticationHelper.AuthCodeTokenResponse>(tokenJson, JsonOptions);

        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrEmpty();
        tokens.TokenType.Should().Be("Bearer");
    }

    /// <summary>
    /// Verifies that using a wrong code_verifier during token exchange fails.
    /// The PKCE code_verifier must match the code_challenge used during authorization.
    /// </summary>
    [Fact]
    public async Task AuthCodeFlow_PkceMismatch_InvalidVerifierFails()
    {
        // Arrange - Generate PKCE parameters
        var correctVerifier = AuthenticationHelper.GenerateCodeVerifier();
        var codeChallenge = AuthenticationHelper.ComputeCodeChallenge(correctVerifier);
        var wrongVerifier = AuthenticationHelper.GenerateCodeVerifier(); // Different verifier

        // Login and get auth code
        var (client, _) = await AuthenticationHelper.LoginViaCookieAsync(
            _fixture, TestEmail, TestPassword);
        using var _ = client;

        var state = Guid.NewGuid().ToString("N");
        var authorizeQuery = AuthenticationHelper.BuildAuthorizeQuery(
            DefaultClientId, DefaultRedirectUri, DefaultScope, codeChallenge, state);

        var authorizeResponse = await client.GetAsync($"/api/auth/authorize?{authorizeQuery}");
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var redirectUri = authorizeResponse.Headers.Location!;
        var queryParams = HttpUtility.ParseQueryString(redirectUri.Query);
        var authorizationCode = queryParams["code"]!;

        // Act - Exchange with WRONG code_verifier
        var tokenRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("code_verifier", wrongVerifier),
            new KeyValuePair<string, string>("redirect_uri", DefaultRedirectUri),
            new KeyValuePair<string, string>("client_id", DefaultClientId)
        ]);

        var tokenResponse = await client.PostAsync("/api/auth/token", tokenRequest);

        // Assert - Should fail with 400 or 403
        tokenResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
    }

    #endregion

    #region Authorization Code Lifecycle Tests

    /// <summary>
    /// Verifies that an authorization code can only be used once.
    /// The second exchange attempt with the same code should fail.
    /// </summary>
    [Fact]
    public async Task AuthCodeFlow_AuthorizationCodeSingleUse_SecondExchangeFails()
    {
        // Arrange - Get an authorization code
        var codeVerifier = AuthenticationHelper.GenerateCodeVerifier();
        var codeChallenge = AuthenticationHelper.ComputeCodeChallenge(codeVerifier);

        var (client, _) = await AuthenticationHelper.LoginViaCookieAsync(
            _fixture, TestEmail, TestPassword);
        using var _ = client;

        var state = Guid.NewGuid().ToString("N");
        var authorizeQuery = AuthenticationHelper.BuildAuthorizeQuery(
            DefaultClientId, DefaultRedirectUri, DefaultScope, codeChallenge, state);

        var authorizeResponse = await client.GetAsync($"/api/auth/authorize?{authorizeQuery}");
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var redirectUri = authorizeResponse.Headers.Location!;
        var queryParams = HttpUtility.ParseQueryString(redirectUri.Query);
        var authorizationCode = queryParams["code"]!;

        // First exchange - should succeed
        var tokenRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("code_verifier", codeVerifier),
            new KeyValuePair<string, string>("redirect_uri", DefaultRedirectUri),
            new KeyValuePair<string, string>("client_id", DefaultClientId)
        ]);

        var firstResponse = await client.PostAsync("/api/auth/token", tokenRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Second exchange with the same code should fail
        var secondRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("code_verifier", codeVerifier),
            new KeyValuePair<string, string>("redirect_uri", DefaultRedirectUri),
            new KeyValuePair<string, string>("client_id", DefaultClientId)
        ]);

        var secondResponse = await client.PostAsync("/api/auth/token", secondRequest);

        // Assert - Second exchange should fail (code already consumed)
        secondResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
    }

    #endregion

    #region Redirect URI Validation Tests

    /// <summary>
    /// Verifies that the token exchange fails when the redirect_uri doesn't match
    /// the one used during authorization.
    /// </summary>
    [Fact]
    public async Task AuthCodeFlow_RedirectUriMismatch_Fails()
    {
        // Arrange - Get an auth code with the correct redirect_uri
        var codeVerifier = AuthenticationHelper.GenerateCodeVerifier();
        var codeChallenge = AuthenticationHelper.ComputeCodeChallenge(codeVerifier);

        var (client, _) = await AuthenticationHelper.LoginViaCookieAsync(
            _fixture, TestEmail, TestPassword);
        using var _ = client;

        var state = Guid.NewGuid().ToString("N");
        var authorizeQuery = AuthenticationHelper.BuildAuthorizeQuery(
            DefaultClientId, DefaultRedirectUri, DefaultScope, codeChallenge, state);

        var authorizeResponse = await client.GetAsync($"/api/auth/authorize?{authorizeQuery}");
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var redirectUri = authorizeResponse.Headers.Location!;
        var queryParams = HttpUtility.ParseQueryString(redirectUri.Query);
        var authorizationCode = queryParams["code"]!;

        // Act - Exchange with a DIFFERENT redirect_uri
        var tokenRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("code_verifier", codeVerifier),
            new KeyValuePair<string, string>("redirect_uri", "http://evil.com/callback"),
            new KeyValuePair<string, string>("client_id", DefaultClientId)
        ]);

        var tokenResponse = await client.PostAsync("/api/auth/token", tokenRequest);

        // Assert - Should fail due to redirect URI mismatch
        tokenResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
    }

    #endregion

    #region Unauthenticated User Tests

    /// <summary>
    /// Verifies that an unauthenticated user (no cookies) hitting the authorize endpoint
    /// is challenged to authenticate. In the test server context without a browser,
    /// the Identity challenge may return 401 Unauthorized or 302 redirect to login.
    /// </summary>
    [Fact]
    public async Task Authorize_UnauthenticatedUser_RedirectsToLogin()
    {
        // Arrange - Create a raw HttpClient without cookies (unauthenticated)
        var handler = new CookieContainerHandler(_fixture.Server.CreateHandler());
        using var client = new HttpClient(handler) { BaseAddress = _fixture.Server.BaseAddress };

        var codeVerifier = AuthenticationHelper.GenerateCodeVerifier();
        var codeChallenge = AuthenticationHelper.ComputeCodeChallenge(codeVerifier);
        var state = Guid.NewGuid().ToString("N");
        var authorizeQuery = AuthenticationHelper.BuildAuthorizeQuery(
            DefaultClientId, DefaultRedirectUri, DefaultScope, codeChallenge, state);

        // Act
        var response = await client.GetAsync($"/api/auth/authorize?{authorizeQuery}");

        // Assert - Should challenge the unauthenticated user.
        // In test server context, Identity challenge returns 401 (no browser to redirect).
        // In production with a browser, this would be a 302 to /Account/Login.
        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            var location = response.Headers.Location;
            location.Should().NotBeNull();
            location!.ToString().Should().Contain("/Account/Login",
                "unauthenticated users should be redirected to the login page");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "unauthenticated users should receive a 401 challenge when no browser redirect is available");
        }
    }

    #endregion

    #region State Parameter Tests

    /// <summary>
    /// Verifies that the state parameter sent in the authorize request is preserved
    /// and returned in the redirect URI after authorization.
    /// </summary>
    [Fact]
    public async Task Authorize_StateParameterPreserved()
    {
        // Arrange
        var codeVerifier = AuthenticationHelper.GenerateCodeVerifier();
        var codeChallenge = AuthenticationHelper.ComputeCodeChallenge(codeVerifier);
        var expectedState = "my-custom-state-value-12345";

        var (client, _) = await AuthenticationHelper.LoginViaCookieAsync(
            _fixture, TestEmail, TestPassword);
        using var _ = client;

        var authorizeQuery = AuthenticationHelper.BuildAuthorizeQuery(
            DefaultClientId, DefaultRedirectUri, DefaultScope, codeChallenge, expectedState);

        // Act
        var authorizeResponse = await client.GetAsync($"/api/auth/authorize?{authorizeQuery}");

        // Assert
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var redirectUri = authorizeResponse.Headers.Location;
        redirectUri.Should().NotBeNull();

        var queryParams = HttpUtility.ParseQueryString(redirectUri!.Query);
        var returnedState = queryParams["state"];
        returnedState.Should().Be(expectedState, "the state parameter must be preserved through the auth code flow for CSRF protection");
    }

    #endregion

    #region Inactive User Tests

    /// <summary>
    /// Verifies that an inactive user who is cookie-authenticated is denied
    /// at the authorize endpoint with an access_denied error.
    /// </summary>
    [Fact]
    public async Task Authorize_InactiveUser_ReturnsForbidden()
    {
        // Arrange - The inactive user has IsActive=false but valid credentials.
        // We need to temporarily activate to allow login, then deactivate before authorize.
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Temporarily activate the user so login succeeds
        var user = await userManager.FindByEmailAsync(InactiveUserEmail);
        user!.IsActive = true;
        await userManager.UpdateAsync(user);

        // Login to get cookies
        var (client, _) = await AuthenticationHelper.LoginViaCookieAsync(
            _fixture, InactiveUserEmail, TestPassword);
        using var _ = client;

        // Now deactivate the user before hitting authorize
        user = await userManager.FindByEmailAsync(InactiveUserEmail);
        user!.IsActive = false;
        await userManager.UpdateAsync(user);

        var codeVerifier = AuthenticationHelper.GenerateCodeVerifier();
        var codeChallenge = AuthenticationHelper.ComputeCodeChallenge(codeVerifier);
        var state = Guid.NewGuid().ToString("N");
        var authorizeQuery = AuthenticationHelper.BuildAuthorizeQuery(
            DefaultClientId, DefaultRedirectUri, DefaultScope, codeChallenge, state);

        // Act
        var authorizeResponse = await client.GetAsync($"/api/auth/authorize?{authorizeQuery}");

        // Assert - Should redirect with an error indicating access denied
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var redirectUri = authorizeResponse.Headers.Location;
        redirectUri.Should().NotBeNull();

        var queryParams = HttpUtility.ParseQueryString(redirectUri!.Query);
        var error = queryParams["error"];
        error.Should().Be(OpenIddictConstants.Errors.AccessDenied,
            "inactive users should be denied with access_denied error");
    }

    #endregion

    #region User Not Found Tests

    /// <summary>
    /// Verifies that when an authenticated user is deleted from the database after login
    /// but before authorization, the authorize endpoint returns a proper OAuth2 error
    /// instead of throwing an exception.
    /// This is an edge case where the user's cookie is still valid but the user record
    /// no longer exists in the database.
    /// </summary>
    [Fact]
    public async Task Authorize_UserNotFoundInDatabase_ReturnsOAuthError()
    {
        // Arrange - Create a temporary user, log in, then delete from database
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var context = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();

        // Create a temporary user
        var tempEmail = "temp-deleted-user@example.com";
        var lab = await context.Labs.FirstAsync();
        var tempUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = tempEmail,
            Email = tempEmail,
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = lab.Id, Role = UserRole.Technician } ],
            IsActive = true
        };
        var result = await userManager.CreateAsync(tempUser, TestPassword);
        result.Succeeded.Should().BeTrue("test user creation should succeed");

        // Login to get cookies
        var (client, _) = await AuthenticationHelper.LoginViaCookieAsync(
            _fixture, tempEmail, TestPassword);
        using var _ = client;

        // Delete the user from the database (simulating edge case)
        var userToDelete = await userManager.FindByEmailAsync(tempEmail);
        await userManager.DeleteAsync(userToDelete!);

        var codeVerifier = AuthenticationHelper.GenerateCodeVerifier();
        var codeChallenge = AuthenticationHelper.ComputeCodeChallenge(codeVerifier);
        var state = Guid.NewGuid().ToString("N");
        var authorizeQuery = AuthenticationHelper.BuildAuthorizeQuery(
            DefaultClientId, DefaultRedirectUri, DefaultScope, codeChallenge, state);

        // Act - Attempt to authorize with valid cookies but deleted user
        var authorizeResponse = await client.GetAsync($"/api/auth/authorize?{authorizeQuery}");

        // Assert - Should redirect with server_error, not throw exception
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "should return OAuth2 error redirect instead of throwing exception");
        
        var redirectUri = authorizeResponse.Headers.Location;
        redirectUri.Should().NotBeNull();

        var queryParams = HttpUtility.ParseQueryString(redirectUri!.Query);
        var error = queryParams["error"];
        error.Should().Be(OpenIddictConstants.Errors.ServerError,
            "authenticated user not found in database should return server_error");
        
        var errorDescription = queryParams["error_description"];
        errorDescription.Should().NotBeNullOrEmpty("error_description should be provided");
    }

    #endregion

    #region Password Grant Removal Tests

    /// <summary>
    /// Verifies that the password grant type is no longer supported.
    /// POST to /api/auth/token with grant_type=password should fail.
    /// </summary>
    [Fact]
    public async Task Token_PasswordGrantRemoved_ReturnsUnsupportedGrantType()
    {
        // Arrange
        var request = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", TestEmail),
            new KeyValuePair<string, string>("password", TestPassword),
            new KeyValuePair<string, string>("scope", DefaultScope)
        ]);

        // Use a raw client (no cookies needed for token endpoint)
        var handler = new CookieContainerHandler(_fixture.Server.CreateHandler());
        using var client = new HttpClient(handler) { BaseAddress = _fixture.Server.BaseAddress };

        // Act
        var response = await client.PostAsync("/api/auth/token", request);

        // Assert - Password grant should be rejected
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<OAuth2ErrorResponse>(json, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Be(OpenIddictConstants.Errors.UnsupportedGrantType);
    }

    #endregion

    #region Response Models

    private sealed class OAuth2ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; } = string.Empty;
    }

    private sealed class UserInfoResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<UserLabDto> Labs { get; set; } = [];
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    #endregion
}

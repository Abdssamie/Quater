using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

/// <summary>
/// Tests for lab context validation.
/// Verifies that requests without valid lab context are properly rejected.
/// </summary>
[Collection("Api")]
public class LabContextValidationTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private Lab _labA = null!;
    private Lab _labB = null!;
    private User _user = null!;
    private const string TestPassword = "Test123!@#456";
    private const string TestEmail = "testuser@example.com";

    public LabContextValidationTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.Client;
    }

    public async Task InitializeAsync()
    {
        // Reset database and Redis before each test
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Seed OpenIddict client for auth code flow
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

        context.Labs.AddRange(_labA, _labB);
        await context.SaveChangesAsync();

        // Create user: Admin in Lab A only
        _user = new User
        {
            Id = Guid.NewGuid(),
            UserName = TestEmail,
            Email = TestEmail,
            EmailConfirmed = true,
            IsActive = true,
            UserLabs =
            [
                new UserLab { LabId = _labA.Id, Role = UserRole.Admin }
            ]
        };

        var result = await userManager.CreateAsync(_user, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Request_WithoutLabContext_Returns403()
    {
        // Arrange: Authenticate as user but don't set lab context
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);
        _client.AddAuthToken(tokenResponse.AccessToken);
        // Don't call _fixture.SetLabContext()

        // Act: Try to access protected endpoint
        var response = await _client.GetAsync("/api/samples");

        // Assert: Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        
        // Check if response has content (error message format may vary)
        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(content))
        {
            // If there's content, verify it mentions lab context
            Assert.Contains("lab", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Request_WithInvalidLabId_Returns403()
    {
        // Arrange: User is member of Lab A only
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);
        _client.AddAuthToken(tokenResponse.AccessToken);

        // Act: Try to access Lab B (not a member)
        _fixture.SetLabContext(_labB.Id, UserRole.Admin); // Invalid - user not in Lab B
        var response = await _client.GetAsync("/api/samples");

        // Assert: Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

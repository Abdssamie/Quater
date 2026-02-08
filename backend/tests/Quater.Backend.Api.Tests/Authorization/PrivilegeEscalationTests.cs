using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Quater.Shared.ValueObjects;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

/// <summary>
/// Tests for privilege escalation prevention in multi-lab scenarios.
/// Verifies that users cannot use privileges from one lab to access resources in another lab.
/// </summary>
[Collection("Api")]
public class PrivilegeEscalationTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private Lab _labA = null!;
    private Lab _labB = null!;
    private User _user = null!;
    private Sample _sampleInLabB = null!;
    private const string TestPassword = "Test123!@#456";
    private const string TestEmail = "testuser@example.com";

    public PrivilegeEscalationTests(ApiTestFixture fixture)
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

        // Create user: Admin in Lab A, Viewer in Lab B
        _user = new User
        {
            Id = Guid.NewGuid(),
            UserName = TestEmail,
            Email = TestEmail,
            EmailConfirmed = true,
            IsActive = true,
            UserLabs =
            [
                new UserLab { LabId = _labA.Id, Role = UserRole.Admin },
                new UserLab { LabId = _labB.Id, Role = UserRole.Viewer }
            ]
        };

        var result = await userManager.CreateAsync(_user, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Create sample in Lab B
        _sampleInLabB = new Sample
        {
            Id = Guid.NewGuid(),
            Type = SampleType.DrinkingWater,
            Location = new Location(0, 0, "Test Location"),
            LabId = _labB.Id,
            CollectionDate = DateTime.UtcNow,
            CollectorName = "Test Collector",
            Status = SampleStatus.Pending
        };

        context.Samples.Add(_sampleInLabB);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AdminInLabA_CannotDeleteSample_InLabB_WhenViewerInLabB()
    {
        // Arrange: Authenticate as user (Admin in Lab A, Viewer in Lab B)
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);
        _client.AddAuthToken(tokenResponse.AccessToken);

        // Set Lab B context (where user is Viewer)
        _fixture.SetLabContext(_labB.Id, UserRole.Viewer);

        // Act: Try to delete sample in Lab B (requires Admin role)
        var response = await _client.DeleteAsync($"/api/samples/{_sampleInLabB.Id}");

        // Assert: Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify sample still exists
        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();
        var sampleStillExists = await context.Samples
            .AnyAsync(s => s.Id == _sampleInLabB.Id && !s.IsDeleted);
        Assert.True(sampleStillExists);
    }

    [Fact]
    public async Task TechnicianInLabA_CannotCreateUser_InLabB_WhenViewerInLabB()
    {
        // Arrange: Authenticate as user (Admin in Lab A, Viewer in Lab B)
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);
        _client.AddAuthToken(tokenResponse.AccessToken);

        // Set Lab B context (where user is Viewer)
        _fixture.SetLabContext(_labB.Id, UserRole.Viewer);

        // Act: Try to create user in Lab B (requires Admin role)
        var createUserRequest = new
        {
            userName = "newuser",
            email = "newuser@test.com",
            password = "Password123!@#",
            role = UserRole.Viewer,
            labId = _labB.Id
        };

        var response = await _client.PostAsJsonAsync("/api/users", createUserRequest);

        // Assert: Should be forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

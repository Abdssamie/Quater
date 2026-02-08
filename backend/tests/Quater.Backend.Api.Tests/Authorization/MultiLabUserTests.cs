using System.Net;
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
/// Tests for multi-lab user scenarios.
/// Verifies that users can belong to multiple labs with different roles,
/// and that permissions are enforced correctly based on the lab context.
/// </summary>
[Collection("Api")]
public class MultiLabUserTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private Lab _labA = null!;
    private Lab _labB = null!;
    private User _user = null!;
    private Sample _sampleInLabA = null!;
    private Sample _sampleInLabB = null!;
    private const string TestPassword = "Test123!@#456";
    private const string TestEmail = "multilab@example.com";

    public MultiLabUserTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
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
                $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Create samples in both labs
        _sampleInLabA = new Sample
        {
            Id = Guid.NewGuid(),
            Type = SampleType.DrinkingWater,
            Location = new Location(0, 0, "Location A"),
            LabId = _labA.Id,
            CollectionDate = DateTime.UtcNow,
            CollectorName = "Test Collector A",
            Status = SampleStatus.Pending
        };

        _sampleInLabB = new Sample
        {
            Id = Guid.NewGuid(),
            Type = SampleType.DrinkingWater,
            Location = new Location(0, 0, "Location B"),
            LabId = _labB.Id,
            CollectionDate = DateTime.UtcNow,
            CollectorName = "Test Collector B",
            Status = SampleStatus.Pending
        };

        context.Samples.AddRange(_sampleInLabA, _sampleInLabB);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task User_CanSwitchBetweenLabs_WithDifferentRoles()
    {
        // Arrange: Authenticate as user
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);

        // Store sample IDs for verification
        var sampleAId = _sampleInLabA.Id;
        var sampleBId = _sampleInLabB.Id;

        // Create client for Lab A (Admin role)
        using var clientA = _fixture.CreateClient();
        clientA.AddAuthToken(tokenResponse.AccessToken);
        clientA.DefaultRequestHeaders.Add("X-Lab-Id", _labA.Id.ToString());

        // Create client for Lab B (Viewer role)
        using var clientB = _fixture.CreateClient();
        clientB.AddAuthToken(tokenResponse.AccessToken);
        clientB.DefaultRequestHeaders.Add("X-Lab-Id", _labB.Id.ToString());

        // Act: Perform admin action in Lab A (should succeed)
        var responseA = await clientA.DeleteAsync($"/api/samples/{sampleAId}");

        // Act: Try admin action in Lab B (should fail - user is Viewer)
        var responseB = await clientB.DeleteAsync($"/api/samples/{sampleBId}");

        // Assert: Admin action succeeded in Lab A, failed in Lab B
        Assert.Equal(HttpStatusCode.NoContent, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, responseB.StatusCode);
    }

    [Fact]
    public async Task User_CanReadInBothLabs_WithDifferentRoles()
    {
        // Arrange: Authenticate as user
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);

        // Create client for Lab A (Admin role)
        using var clientA = _fixture.CreateClient();
        clientA.AddAuthToken(tokenResponse.AccessToken);
        clientA.DefaultRequestHeaders.Add("X-Lab-Id", _labA.Id.ToString());

        // Create client for Lab B (Viewer role)
        using var clientB = _fixture.CreateClient();
        clientB.AddAuthToken(tokenResponse.AccessToken);
        clientB.DefaultRequestHeaders.Add("X-Lab-Id", _labB.Id.ToString());

        // Act: Read sample in Lab A (Admin role)
        var responseA = await clientA.GetAsync($"/api/samples/{_sampleInLabA.Id}");

        // Act: Read sample in Lab B (Viewer
        var responseB = await clientB.GetAsync($"/api/samples/{_sampleInLabB.Id}");

        // Assert: Both should succeed (Viewer can read)
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

/// <summary>
/// Tests for role hierarchy enforcement.
/// Verifies that role permissions are enforced correctly:
/// - Viewer: Can read data only
/// - Technician: Can read and create/update samples and tests
/// - Admin: Full access including user management
/// </summary>
[Collection("Api")]
public class RoleHierarchyTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private Lab _lab = null!;
    private User _viewerUser = null!;
    private User _technicianUser = null!;
    private const string TestPassword = "Test123!@#456";
    private const string ViewerEmail = "viewer@example.com";
    private const string TechnicianEmail = "technician@example.com";

    public RoleHierarchyTests(ApiTestFixture fixture)
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

        // Create lab
        _lab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Test Lab",
            Location = "Test Location",
            IsActive = true
        };

        context.Labs.Add(_lab);
        await context.SaveChangesAsync();

        // Create viewer user
        _viewerUser = new User
        {
         Id = Guid.NewGuid(),
            UserName = ViewerEmail,
            Email = ViewerEmail,
            EmailConfirmed = true,
            IsActive = true,
            UserLabs = [new UserLab { LabId = _lab.Id, Role = UserRole.Viewer }]
        };

        // Create technician user
        _technicianUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = TechnicianEmail,
            Email = TechnicianEmail,
            EmailConfirmed = true,
            IsActive = true,
            UserLabs = [new UserLab { LabId = _lab.Id, Role = UserRole.Technician }]
        };

        var viewerResult = await userManager.CreateAsync(_viewerUser, TestPassword);
        if (!viewerResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create viewer user: {string.Join(", ", viewerResult.Errors.Select(e => e.Description))}");
        }

        var technicianResult = await userManager.CreateAsync(_technicianUser, TestPassword);
        if (!technicianResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create technician user: {string.Join(", ", technicianResult.Errors.Select(e => e.Description))}");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Viewer_CanReadSamples_CannotCreateSamples()
    {
        // Arrange: Create a new client for this test and authenticate as viewer
        using var client = _fixture.CreateClient();
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, ViewerEmail, TestPassword);
        client.AddAuthToken(tokenResponse.AccessToken);
        
        // Set lab context via HTTP header
        client.DefaultRequestHeaders.Add("X-Lab-Id", _lab.Id.ToString());

        // Act: Try to read samples (allowed for Viewer)
        var readResponse = await client.GetAsync("/api/samples");

        // Act: Try to create sample (forbidden for Viewer, requires Technician)
        var createDto = new CreateSampleDto
        {
            Type = SampleType.DrinkingWater,
            LocationLatitude = 0,
            LocationLongitude = 0,
            LocationDescription = "Test Location",
            CollectionDate = DateTime.UtcNow,
            CollectorName = "Test Collector",
            LabId = _lab.Id
        };
        var createResponse = await client.PostAsJsonAsync("/api/samples", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task Technician_CanCreateSamples_CannotDeleteUsers()
    {
        // Arrange: Create a new client for this test and authenticate as technician
        using var client = _fixture.CreateClient();
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TechnicianEmail, TestPassword);
        client.AddAuthToken(tokenResponse.AccessToken);
        
        // Set lab context via HTTP header
        client.DefaultRequestHeaders.Add("X-Lab-Id", _lab.Id.ToString());

        // Act: Try to delete user (forbidden for Technician, requires Admin)
        var deleteResponse = await client.DeleteAsync($"/api/users/{_viewerUser.Id}");

        // Assert: Technician cannot delete users
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }
}

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
/// Tests for system administrator bypass functionality - accessing samples across labs.
/// Verifies that system admins can access resources across all labs without lab context.
/// </summary>
[Collection("SystemAdminSamples")]
public class SystemAdminSamplesTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private Lab _labA = null!;
    private Lab _labB = null!;
    private Sample _sampleInLabA = null!;
    private Sample _sampleInLabB = null!;
    private User _systemAdminUser = null!;
    private const string SystemAdminUserId = "eb4b0ebc-7a02-43ca-a858-656bd7e4357f";
    private const string TestPassword = "Test123!@#456";
    private const string TestEmail = "sysadmin@example.com";

    public SystemAdminSamplesTests(ApiTestFixture fixture)
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

        // Create system admin user with the configured system admin user ID
        _systemAdminUser = new User
        {
            Id = Guid.Parse(SystemAdminUserId),
            UserName = TestEmail,
            Email = TestEmail,
            EmailConfirmed = true,
            IsActive = true,
            UserLabs =
            [
                new UserLab { LabId = _labA.Id, Role = UserRole.Admin }
            ]
        };

        var result = await userManager.CreateAsync(_systemAdminUser, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create system admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
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
    public async Task SystemAdmin_CanAccessSamples_InAllLabs_WithoutLabContext()
    {
        // Arrange: Set system admin mode (bypasses lab context requirement)
        _fixture.SetSystemAdmin();
        
        // Authenticate as system admin user
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);
        _client.AddAuthToken(tokenResponse.AccessToken);

        // Act: Access samples in different labs without X-Lab-Id header
        var responseA = await _client.GetAsync($"/api/samples/{_sampleInLabA.Id}");
        var responseB = await _client.GetAsync($"/api/samples/{_sampleInLabB.Id}");

        // Assert: Both should succeed
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }
}

/// <summary>
/// Tests for system administrator bypass functionality - performing admin actions.
/// Verifies that system admins can perform admin actions across all labs.
/// </summary>
[Collection("SystemAdminActions")]
public class SystemAdminActionsTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private Lab _labA = null!;
    private User _systemAdminUser = null!;
    private const string SystemAdminUserId = "eb4b0ebc-7a02-43ca-a858-656bd7e4357f";
    private const string TestPassword = "Test123!@#456";
    private const string TestEmail = "admin@example.com";

    public SystemAdminActionsTests(ApiTestFixture fixture)
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

        // Create lab
        _labA = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Lab A",
            Location = "Location A",
            IsActive = true
        };

        context.Labs.Add(_labA);
        await context.SaveChangesAsync();

        // Create system admin user with the configured system admin user ID
        _systemAdminUser = new User
        {
            Id = Guid.Parse(SystemAdminUserId),
            UserName = TestEmail,
            Email = TestEmail,
            EmailConfirmed = true,
            IsActive = true,
            UserLabs =
            [
                new UserLab { LabId = _labA.Id, Role = UserRole.Admin }
            ]
        };

        var result = await userManager.CreateAsync(_systemAdminUser, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create system admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SystemAdmin_CanPerformAdminActions_InAnyLab()
    {
        // Arrange: Set system admin mode
        _fixture.SetSystemAdmin();
        
        // Authenticate as system admin user
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(
            _fixture, TestEmail, TestPassword);
        _client.AddAuthToken(tokenResponse.AccessToken);

        // Create a test user to delete
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "deleteuser@test.com",
            Email = "deleteuser@test.com",
            EmailConfirmed = true,
            IsActive = true,
            UserLabs = [new UserLab { LabId = _labA.Id, Role = UserRole.Viewer }]
        };
        await userManager.CreateAsync(testUser, TestPassword);

        // Act: Delete user (admin action)
        var response = await _client.DeleteAsync($"/api/users/{testUser.Id}");

        // Assert: Should succeed
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify user was deactivated
        using var verifyScope = _fixture.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var deletedUser = await verifyUserManager.FindByIdAsync(testUser.Id.ToString());
        Assert.NotNull(deletedUser);
        Assert.False(deletedUser.IsActive);
    }
}

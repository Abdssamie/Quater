using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Shared.Models;

namespace Quater.Backend.Api.Tests.Controllers;

[Collection("Api")]
public sealed class VersionControllerTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture = fixture;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

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

        _client = _fixture.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Get_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_AuthenticatedViewer_ReturnsOkWithVersionInfo()
    {
        // Arrange
        var (user, password) = await CreateTestUserAsync("version-test@test.com", "Password123!");
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, password);
        _client.AddAuthToken(tokenResponse.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<VersionResponse>();
        content.Should().NotBeNull();
        content!.Version.Should().NotBeNullOrEmpty();
        content.ApiVersion.Should().Be("v1");
    }

    private async Task<(User user, string password)> CreateTestUserAsync(string email, string password)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Quater.Backend.Data.QuaterDbContext>();

        var lab = new Quater.Shared.Models.Lab
        {
            Id = Guid.NewGuid(),
            Name = $"Test Lab for {email}",
            Location = "Test Location",
            ContactInfo = "test@test.com",
            IsActive = true
        };
        dbContext.Labs.Add(lab);
        await dbContext.SaveChangesAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            UserLabs = [new Quater.Shared.Models.UserLab { LabId = lab.Id, Role = Quater.Shared.Enums.UserRole.Admin }],
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return (user, password);
    }
}

public class VersionResponse
{
    public string Version { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string BuildDate { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}

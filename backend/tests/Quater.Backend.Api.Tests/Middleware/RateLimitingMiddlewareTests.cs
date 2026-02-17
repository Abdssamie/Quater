using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Shared.Models;
using StackExchange.Redis;

namespace Quater.Backend.Api.Tests.Middleware;

[Collection("Api")]
public sealed class RateLimitingMiddlewareTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture = fixture;

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
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_RedisConnectionException_ReturnsServiceUnavailable()
    {
        // Arrange - Create client with broken Redis
        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is down"));

        var factory = _fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer>(mockRedis.Object);
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Service temporarily unavailable");
    }

    [Fact]
    public async Task InvokeAsync_RedisTimeoutException_ReturnsServiceUnavailable()
    {
        // Arrange - Create client with timing-out Redis
        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisTimeoutException("Redis timeout", CommandStatus.Unknown));

        var factory = _fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer>(mockRedis.Object);
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task InvokeAsync_RedisWorking_NormalRequestSucceeds()
    {
        // Arrange - Use normal fixture with working Redis
        var (user, password) = await CreateTestUserAsync("ratelimit-test@test.com", "Password123!");
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, password);
        var client = _fixture.CreateClient();
        client.AddAuthToken(tokenResponse.AccessToken);

        // Act
        var response = await client.GetAsync("/api/version");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-RateLimit-Limit");
        response.Headers.Should().ContainKey("X-RateLimit-Remaining");
    }

    private async Task<(User user, string password)> CreateTestUserAsync(string email, string password)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Quater.Backend.Data.QuaterDbContext>();

        var lab = new Lab
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
            UserLabs = [new UserLab { LabId = lab.Id, Role = Quater.Shared.Enums.UserRole.Admin }],
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

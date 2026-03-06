using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Seeders;
using Quater.Backend.Api.Tests.Fixtures;

namespace Quater.Backend.Api.Tests.Seeders;

[Collection("Api")]
public sealed class OpenIddictSeederTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    [Fact]
    public async Task SeedAsync_AlwaysSeedsDesktopAndMobileClients()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var manager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await foreach (var application in manager.ListAsync())
        {
            await manager.DeleteAsync(application);
        }

        await OpenIddictSeeder.SeedAsync(serviceProvider);

        var desktopClient = await manager.FindByClientIdAsync("quater-desktop-client");
        var mobileClient = await manager.FindByClientIdAsync("quater-mobile-client");

        desktopClient.Should().NotBeNull();
        mobileClient.Should().NotBeNull();
    }
}

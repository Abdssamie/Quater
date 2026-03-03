using Duende.IdentityModel.OidcClient;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Settings;

namespace Quater.Desktop.Tests.Auth;

public sealed class OidcClientFactoryTests
{
    [Fact]
    public void Create_UsesClientIdFromSettings()
    {
        var settings = new AppSettings
        {
            BackendUrl = "https://auth.example.com",
            ClientId = "my-custom-desktop-client"
        };
        var factory = new OidcClientFactory(settings);

        var client = factory.Create();

        Assert.Equal("my-custom-desktop-client", client.Options.ClientId);
    }

    [Fact]
    public void Create_DefaultClientId_IsDesktopClient()
    {
        var settings = new AppSettings { BackendUrl = "https://auth.example.com" };
        var factory = new OidcClientFactory(settings);

        var client = factory.Create();

        Assert.Equal("quater-desktop-client", client.Options.ClientId);
    }

    [Fact]
    public void Create_DoesNotDisableIdentityTokenSignature()
    {
        var settings = new AppSettings { BackendUrl = "https://auth.example.com" };
        var factory = new OidcClientFactory(settings);

        var client = factory.Create();

        // The default is true; we must never override it to false.
        Assert.True(client.Options.Policy.RequireIdentityTokenSignature);
    }

    [Fact]
    public void Create_ClientId_IsNeverMobileClient()
    {
        var settings = new AppSettings { BackendUrl = "https://auth.example.com" };
        var factory = new OidcClientFactory(settings);

        var client = factory.Create();

        Assert.NotEqual("quater-mobile-client", client.Options.ClientId);
    }
}

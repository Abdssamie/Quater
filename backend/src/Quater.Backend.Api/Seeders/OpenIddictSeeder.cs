using OpenIddict.Abstractions;

namespace Quater.Backend.Api.Seeders;

/// <summary>
/// Seeds OpenIddict client applications for OAuth2/OIDC authentication.
/// </summary>
public static class OpenIddictSeeder
{
    private sealed record ClientSeedConfig(
        string ClientId,
        string DisplayName,
        IReadOnlyList<string> RedirectUris);

    private static readonly ClientSeedConfig[] ClientConfigs =
    [
        new(
            "quater-desktop-client",
            "Quater Desktop Client",
            [
                "http://127.0.0.1/callback",
                "http://127.0.0.1:7890/callback"
            ]),
        new(
            "quater-mobile-client",
            "Quater Mobile Client",
            [
                "quater://oauth/callback",
                "http://127.0.0.1/callback"
            ])
    ];

    /// <summary>
    /// Seeds the default OpenIddict client application for desktop apps.
    /// Uses authorization code flow with PKCE (public client, no client secret).
    /// Updates existing client if redirect URIs have changed.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var manager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var clientConfig in ClientConfigs)
        {
            var descriptor = CreateDescriptor(clientConfig);
            var existingClient = await manager.FindByClientIdAsync(clientConfig.ClientId);

            if (existingClient != null)
            {
                await manager.UpdateAsync(existingClient, descriptor);
                continue;
            }

            await manager.CreateAsync(descriptor);
        }
    }

    private static OpenIddictApplicationDescriptor CreateDescriptor(ClientSeedConfig clientConfig)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientConfig.ClientId,
            DisplayName = clientConfig.DisplayName,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
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
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var redirectUri in clientConfig.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        return descriptor;
    }
}

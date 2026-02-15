using OpenIddict.Abstractions;

namespace Quater.Backend.Api.Seeders;

/// <summary>
/// Seeds OpenIddict client applications for OAuth2/OIDC authentication.
/// </summary>
public static class OpenIddictSeeder
{
    /// <summary>
    /// Seeds the default OpenIddict client application for mobile/desktop apps.
    /// Uses authorization code flow with PKCE (public client, no client secret).
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var manager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Seed mobile/desktop client
        await SeedMobileClientAsync(manager);
        
        // Seed Swagger UI client (development only)
        await SeedSwaggerClientAsync(manager);
    }

    private static async Task SeedMobileClientAsync(IOpenIddictApplicationManager manager)
    {
        // Get client configuration from environment variables
        var clientId = Environment.GetEnvironmentVariable("OPENIDDICT_CLIENT_ID") ?? "quater-mobile-client";

        // Check if client already exists
        var existingClient = await manager.FindByClientIdAsync(clientId);
        if (existingClient != null)
        {
            return; // Client already exists
        }

        // Create OpenIddict application descriptor for public client with authorization code + PKCE
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = "Quater Mobile/Desktop Client",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            RedirectUris =
            {
                new Uri("quater://oauth/callback"),       // Mobile deep link
                new Uri("http://127.0.0.1/callback")      // Desktop loopback
         },
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

        await manager.CreateAsync(descriptor);
    }

    private static async Task SeedSwaggerClientAsync(IOpenIddictApplicationManager manager)
    {
        const string clientId = "quater-swagger-client";

        // Check if client already exists
        var existingClient = await manager.FindByClientIdAsync(clientId);
        if (existingClient != null)
        {
            return; // Client already exists
        }

        // Create OpenIddict application descriptor for Swagger UI (development only)
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = "Quater Swagger UI (Development)",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            RedirectUris =
            {
                new Uri("http://localhost:5198/swagger/oauth2-redirect.html"),
                new Uri("https://localhost:5198/swagger/oauth2-redirect.html")
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api"
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        await manager.CreateAsync(descriptor);
    }
}

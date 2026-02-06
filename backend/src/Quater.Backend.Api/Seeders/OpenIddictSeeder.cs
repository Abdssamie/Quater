using OpenIddict.Abstractions;

namespace Quater.Backend.Api.Seeders;

/// <summary>
/// Seeds OpenIddict client applications for OAuth2/OIDC authentication.
/// </summary>
public static class OpenIddictSeeder
{
    /*
     * @id: openiddict-seeder-public
     * @priority: high
     * @progress: 0
     * @directive: Update OpenIddict seeder to configure public client for authorization code flow with PKCE. Change ClientType from Confidential to Public. Remove ClientSecret. Add authorization endpoint permission. Replace password grant permission with authorization code grant. Add PKCE requirement via Requirements.Features.ProofKeyForCodeExchange. Add redirect URIs for mobile (quater://oauth/callback) and desktop (http://127.0.0.1/callback). Keep refresh token and scope permissions.
     * @context: specs/oauth2-mobile-desktop-security-enhancement.md#fr-07-update-openiddictseeder
     * @checklist: [
     *   "ClientType changed to OpenIddictConstants.ClientTypes.Public (FR-07)",
     *   "ClientSecret removed (public clients have no secret) (FR-04)",
     *   "Auto-generated secret logic removed (FR-04)",
     *   "Permissions.Endpoints.Authorization added (FR-07)",
     *   "Permissions.GrantTypes.Password replaced with GrantTypes.AuthorizationCode (FR-07)",
     *   "Requirements.Features.ProofKeyForCodeExchange added (FR-07)",
     *   "Redirect URIs configured for mobile deep link (FR-07)",
     *   "Redirect URIs configured for desktop loopback (FR-07)",
     *   "Existing scope permissions preserved (FR-07)",
     *   "Refresh token permission preserved (SC-05)"
     * ]
     * @deps: ["openiddict-config-authcode"]
     * @skills: ["openiddict-application-descriptor"]
     */
    /// <summary>
    /// Seeds the default OpenIddict client application for mobile/desktop apps.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var manager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Get client configuration from environment variables
        var clientId = Environment.GetEnvironmentVariable("OPENIDDICT_CLIENT_ID") ?? "quater-mobile-client";
        var clientSecret = Environment.GetEnvironmentVariable("OPENIDDICT_CLIENT_SECRET");

        // Check if client already exists
        var existingClient = await manager.FindByClientIdAsync(clientId);
        if (existingClient != null)
        {
            return; // Client already exists
        }

        // Generate secure client secret if not provided
        if (string.IsNullOrEmpty(clientSecret))
        {
            clientSecret = GenerateSecureClientSecret();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("IMPORTANT: OpenIddict client credentials generated!");
            Console.WriteLine($"Client ID: {clientId}");
            Console.WriteLine($"Client Secret: {clientSecret}");
            Console.WriteLine("Store these credentials securely in Infisical or your secrets manager.");
            Console.WriteLine("Set OPENIDDICT_CLIENT_ID and OPENIDDICT_CLIENT_SECRET environment variables.");
            Console.WriteLine("=".PadRight(80, '='));
        }

        // Create OpenIddict application descriptor
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = "Quater Mobile/Desktop Client",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access"
            }
        };

        await manager.CreateAsync(descriptor);
    }

    /// <summary>
    /// Generates a secure random client secret (64 characters).
    /// </summary>
    private static string GenerateSecureClientSecret()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var random = new Random();
        return new string(Enumerable.Range(0, 64)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}

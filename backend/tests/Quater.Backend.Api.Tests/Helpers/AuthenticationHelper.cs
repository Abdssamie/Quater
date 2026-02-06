using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quater.Backend.Api.Tests.Helpers;

/// <summary>
/// Helper methods for authentication in integration tests.
/// </summary>
/*
 * @id: auth-helper-authcode
 * @priority: medium
 * @progress: 0
 * @directive: Add GetAuthTokenViaAuthCodeFlowAsync method. Generate PKCE code_verifier (43-128 chars, base64url random bytes) and code_challenge (SHA-256 of verifier, base64url encoded). Send GET to /api/auth/authorize with required params (authenticated via cookie). Extract authorization code from redirect response. Exchange code for tokens via POST /api/auth/token with grant_type=authorization_code, code, code_verifier, redirect_uri, client_id. Add helper to generate DPoP proofs for tests. Keep existing GetAuthTokenAsync for backward compatibility during migration.
 * @context: specs/oauth2-mobile-desktop-security-enhancement.md#9-testing-strategy
 * @checklist: [
 *   "GetAuthTokenViaAuthCodeFlowAsync method added",
 *   "PKCE code_verifier generation (43-128 chars, base64url) (FR-03)",
 *   "PKCE code_challenge generation (SHA-256, base64url) (FR-03)",
 *   "Sends authorize request with all required params (FR-02)",
 *   "Extracts authorization code from redirect (FR-02)",
 *   "Exchanges code for tokens with code_verifier (FR-06)",
 *   "GenerateDPoPProof helper for DPoP-bound token tests (FR-09)",
 *   "Existing GetAuthTokenAsync preserved for migration tests (SC-05)",
 *   "AddDPoPHeader extension method for HttpClient"
 * ]
 * @deps: ["authorization-controller", "openiddict-config-authcode"]
 * @skills: ["oauth2-pkce-client", "integration-test-helpers"]
 */
public static class AuthenticationHelper
{
    /// <summary>
    /// Authenticates a user and returns an access token.
    /// </summary>
    public static async Task<string> GetAuthTokenAsync(
        HttpClient client,
        string email,
        string password)
    {
        var request = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", email),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("scope", "openid email profile offline_access api")
        });

        var response = await client.PostAsync("/api/auth/token", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return tokenResponse?.AccessToken ?? throw new InvalidOperationException("Failed to get access token");
    }

    /// <summary>
    /// Adds an authorization header with a bearer token to the HTTP client.
    /// </summary>
    public static void AddAuthToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Removes the authorization header from the HTTP client.
    /// </summary>
    public static void RemoveAuthToken(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}

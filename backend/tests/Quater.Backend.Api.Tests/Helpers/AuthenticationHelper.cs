using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quater.Backend.Api.Tests.Helpers;

/// <summary>
/// Helper methods for authentication in integration tests.
/// </summary>
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

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Quater.Backend.Api.Tests.Helpers;

/// <summary>
/// Helper methods for authentication in integration tests.
/// Supports the authorization code flow with PKCE for all test authentication.
/// </summary>
/*
 * @id: auth-helper-authcode
 * @priority: medium
 * @progress: 100
 * @directive: Add GetAuthTokenViaAuthCodeFlowAsync method. Generate PKCE code_verifier (43-128 chars, base64url random bytes) and code_challenge (SHA-256 of verifier, base64url encoded). Send GET to /api/auth/authorize with required params (authenticated via cookie). Extract authorization code from redirect response. Exchange code for tokens via POST /api/auth/token with grant_type=authorization_code, code, code_verifier, redirect_uri, client_id.
 * @context: specs/oauth2-mobile-desktop-security-enhancement.md#9-testing-strategy
 * @checklist: [
 *   "GetAuthTokenViaAuthCodeFlowAsync method added",
 *   "PKCE code_verifier generation (43-128 chars, base64url) (FR-03)",
 *   "PKCE code_challenge generation (SHA-256, base64url) (FR-03)",
 *   "Sends authorize request with all required params (FR-02)",
 *   "Extracts authorization code from redirect (FR-02)",
 *   "Exchanges code for tokens with code_verifier (FR-06)"
 * ]
 * @deps: ["authorization-controller", "openiddict-config-authcode"]
 * @skills: ["oauth2-pkce-client", "integration-test-helpers"]
 */
public static partial class AuthenticationHelper
{
    private const string DefaultClientId = "quater-mobile-client";
    private const string DefaultRedirectUri = "http://127.0.0.1/callback";
    private const string DefaultScope = "openid email profile offline_access api";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Authenticates a user via the authorization code flow with PKCE and returns the full token response.
    /// This is the recommended flow for mobile/desktop clients.
    ///
    /// Flow:
    /// 1. POST to /Account/Login to get an Identity cookie
    /// 2. GET /api/auth/authorize with PKCE params (code_challenge, code_challenge_method=S256)
    /// 3. Extract the authorization code from the redirect Location header
    /// 4. POST /api/auth/token to exchange the code + code_verifier for tokens
    /// </summary>
    /// <param name="factory">The WebApplicationFactory to create test clients from.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="clientId">OAuth client_id (defaults to seeded test client).</param>
    /// <param name="redirectUri">OAuth redirect_uri (defaults to desktop loopback).</param>
    /// <param name="scope">OAuth scopes to request.</param>
    /// <returns>The full token response including access_token, refresh_token, etc.</returns>
    public static async Task<AuthCodeTokenResponse> GetAuthTokenViaAuthCodeFlowAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string password,
        string clientId = DefaultClientId,
        string redirectUri = DefaultRedirectUri,
        string scope = DefaultScope)
    {
        // Generate PKCE parameters (client-side responsibility)
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = ComputeCodeChallenge(codeVerifier);

        // Create a client that does NOT follow redirects so we can inspect Location headers.
        // Use a CookieContainer to carry the Identity cookie across requests.
        var cookieHandler = new CookieContainerHandler(factory.Server.CreateHandler());
        using var client = new HttpClient(cookieHandler) { BaseAddress = factory.Server.BaseAddress };

        // Step 1: GET the login page to obtain the anti-forgery token
        var state = Guid.NewGuid().ToString("N");
        var authorizeQuery = BuildAuthorizeQuery(clientId, redirectUri, scope, codeChallenge, state);
        var returnUrl = $"/api/auth/authorize?{authorizeQuery}";

        var loginPageResponse = await client.GetAsync($"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
        loginPageResponse.EnsureSuccessStatusCode();

        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginPageHtml);

        // Step 2: POST credentials to the login page to get the Identity cookie
        var loginForm = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            new KeyValuePair<string, string>("ReturnUrl", returnUrl)
        ]);

        var loginResponse = await client.PostAsync(
            $"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}", loginForm);

        // Login should redirect (302) back to the authorize endpoint
        if (loginResponse.StatusCode != HttpStatusCode.Redirect)
        {
            var body = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Login failed with status {loginResponse.StatusCode}. Response: {body[..Math.Min(body.Length, 500)]}");
        }

        // Step 3: Follow the redirect to the authorize endpoint (now authenticated via cookie)
        var authorizeRedirectUri = loginResponse.Headers.Location
            ?? throw new InvalidOperationException("Login redirect did not include a Location header.");

        var authorizeResponse = await client.GetAsync(authorizeRedirectUri);

        // The authorize endpoint should redirect to the redirect_uri with the authorization code
        if (authorizeResponse.StatusCode != HttpStatusCode.Redirect)
        {
            var body = await authorizeResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Authorize endpoint returned {authorizeResponse.StatusCode} instead of redirect. Response: {body[..Math.Min(body.Length, 500)]}");
        }

        var codeRedirectUri = authorizeResponse.Headers.Location
            ?? throw new InvalidOperationException("Authorize redirect did not include a Location header.");

        // Step 4: Extract the authorization code from the redirect URI
        var queryParams = HttpUtility.ParseQueryString(codeRedirectUri.Query);
        var authorizationCode = queryParams["code"]
            ?? throw new InvalidOperationException(
                $"Authorization code not found in redirect URI: {codeRedirectUri}");

        // Verify state parameter was preserved (CSRF protection)
        var returnedState = queryParams["state"];
        if (returnedState != state)
        {
            throw new InvalidOperationException(
                $"State mismatch: sent '{state}', received '{returnedState}'");
        }

        // Step 5: Exchange the authorization code for tokens at the token endpoint
        var tokenRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("code_verifier", codeVerifier),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("client_id", clientId)
        ]);

        var tokenResponse = await client.PostAsync("/api/auth/token", tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthCodeTokenResponse>(tokenJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize token response.");
    }

    /// <summary>
    /// Performs only the login step and returns a cookie-authenticated HttpClient.
    /// Useful for tests that need to control the authorize step separately.
    /// </summary>
    /// <param name="factory">The WebApplicationFactory to create test clients from.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>An HttpClient with the Identity cookie set, and a CookieContainer for inspection.</returns>
    public static async Task<(HttpClient Client, CookieContainer Cookies)> LoginViaCookieAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string password)
    {
        var cookieHandler = new CookieContainerHandler(factory.Server.CreateHandler());
        var client = new HttpClient(cookieHandler) { BaseAddress = factory.Server.BaseAddress };

        // GET the login page for anti-forgery token
        var loginPageResponse = await client.GetAsync("/Account/Login");
        loginPageResponse.EnsureSuccessStatusCode();

        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginPageHtml);

        // POST credentials
        var loginForm = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
        ]);

        var loginResponse = await client.PostAsync("/Account/Login", loginForm);

        // Should redirect to "/" on success (no ReturnUrl specified)
        if (loginResponse.StatusCode != HttpStatusCode.Redirect)
        {
            var body = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Login failed with status {loginResponse.StatusCode}. Response: {body[..Math.Min(body.Length, 500)]}");
        }

        return (client, cookieHandler.CookieContainer);
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

    #region PKCE Helpers

    /// <summary>
    /// Generates a cryptographically random PKCE code_verifier (43-128 chars, base64url-encoded).
    /// Per RFC 7636 Section 4.1.
    /// </summary>
    public static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 32 bytes → 43 base64url chars
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Computes the PKCE code_challenge from a code_verifier using S256 method.
    /// code_challenge = BASE64URL(SHA256(code_verifier))
    /// Per RFC 7636 Section 4.2.
    /// </summary>
    public static string ComputeCodeChallenge(string codeVerifier)
    {
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    /// <summary>
    /// Base64url-encodes a byte array (no padding, URL-safe characters).
    /// Per RFC 4648 Section 5.
    /// </summary>
    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Builds the query string for the authorization endpoint.
    /// </summary>
    internal static string BuildAuthorizeQuery(
        string clientId,
        string redirectUri,
        string scope,
        string codeChallenge,
        string state) =>
        string.Join("&",
            $"response_type=code",
            $"client_id={Uri.EscapeDataString(clientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
            $"scope={Uri.EscapeDataString(scope)}",
            $"code_challenge={Uri.EscapeDataString(codeChallenge)}",
            $"code_challenge_method=S256",
            $"state={Uri.EscapeDataString(state)}");

    /// <summary>
    /// Extracts the anti-forgery token from the Razor login page HTML.
    /// Looks for <input name="__RequestVerificationToken" ... value="..."> in the rendered form.
    /// </summary>
    private static string ExtractAntiForgeryToken(string html)
    {
        var match = AntiForgeryTokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Could not find __RequestVerificationToken in the login page HTML. " +
                "Ensure the Razor login page includes @Html.AntiForgeryToken() or a <form> with asp-antiforgery.");
        }

        return match.Groups[1].Value;
    }

    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*\s+value=""([^""]+)""|value=""([^""]+)""[^>]*\s+name=""__RequestVerificationToken""")]
    private static partial Regex AntiForgeryTokenRegex();

    #endregion

    #region Response Models

    /// <summary>
    /// Full token response from the authorization code exchange.
    /// Includes all fields returned by the OpenIddict token endpoint.
    /// </summary>
    public sealed class AuthCodeTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    #endregion
}

/// <summary>
/// DelegatingHandler that uses a CookieContainer to manage cookies across requests.
/// Required for testing the auth code flow where Identity cookies must persist
/// between the login POST and the authorize GET.
/// </summary>
public sealed class CookieContainerHandler : DelegatingHandler
{
    public CookieContainer CookieContainer { get; } = new();

    public CookieContainerHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestUri = request.RequestUri
            ?? throw new InvalidOperationException("Request URI cannot be null.");

        // Attach cookies from the container to the outgoing request
        var cookieHeader = CookieContainer.GetCookieHeader(requestUri);
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Capture Set-Cookie headers from the response into the container
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var setCookie in setCookieHeaders)
            {
                CookieContainer.SetCookies(requestUri, setCookie);
            }
        }

        return response;
    }
}

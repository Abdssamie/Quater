using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Quater.Backend.Core.Constants;
using Quater.Shared.Models;
using System.Security.Claims;
using Microsoft.AspNetCore;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// OAuth 2.0 authorization endpoint for authorization code flow with PKCE.
/// Handles GET and POST /api/auth/authorize requests from mobile and desktop clients.
/// OpenIddict validates client_id, redirect_uri, and PKCE parameters automatically
/// before the request reaches this passthrough controller.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthorizationController(
    UserManager<User> userManager,
    ILogger<AuthorizationController> logger) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<AuthorizationController> _logger = logger;

    /// <summary>
    /// OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE.
    /// Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).
    ///
    /// Query parameters (validated by OpenIddict before reaching this endpoint):
    ///   response_type=code (required)
    ///   client_id=quater-mobile or quater-desktop (required)
    ///   redirect_uri=... (required, validated against registered URIs)
    ///   scope=openid email profile offline_access api (optional)
    ///   state=... (recommended, returned as-is in redirect)
    ///   code_challenge=... (required when PKCE is enforced)
    ///   code_challenge_method=S256 (required when PKCE is enforced)
    ///
    /// Flow:
    /// 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method
    /// 2. This controller checks if user is authenticated (via cookie)
    /// 3. If not authenticated, returns Challenge to redirect to login
    /// 4. If authenticated, creates claims principal and issues authorization code
    /// 5. Redirects to redirect_uri with code and state parameters
    /// </summary>
    /// <returns>
    /// Redirect to redirect_uri with authorization code and state,
    /// or Challenge result if user is not authenticated.
    /// </returns>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [AllowAnonymous] // Uses cookie-based authentication, not Bearer tokens
    public async Task<IActionResult> Authorize()
    {
        // Retrieve the OpenIddict server request.
        // OpenIddict has already validated client_id, redirect_uri, code_challenge,
        // and code_challenge_method before the request reaches this passthrough handler.
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        // Check if the user is already authenticated via cookie-based session.
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (result is not { Succeeded: true })
        {
            _logger.LogInformation(
                "Authorization request for client {ClientId} requires authentication, issuing challenge",
                request.ClientId);

            // User is not authenticated - challenge them to log in.
            // After login, the authentication middleware will redirect back to this endpoint
            // with the original query parameters preserved.
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        // User is authenticated - look up the user to get current claims.
        var user = await _userManager.GetUserAsync(result.Principal)
            ?? throw new InvalidOperationException("The authenticated user could not be found.");

        // Verify the user account is still active.
        if (!user.IsActive)
        {
            _logger.LogWarning(
                "Authorization denied: User {UserId} is inactive",
                user.Id);

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.AccessDenied,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user account is inactive."
                }));
        }

        // Create claims principal with all required claims.
        // This follows the same pattern as AuthController.Token for consistency.
        var claims = new List<Claim>
        {
            new(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
            new(OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty),
            new(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty),
            new(QuaterClaimTypes.Role, user.Role.ToString()),
            new(QuaterClaimTypes.LabId, user.LabId.ToString())
        };

        var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Set scopes based on what the client requested.
        principal.SetScopes(request.GetScopes());

        // Set destinations for each claim (access token, identity token, or both).
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim));
        }

        _logger.LogInformation(
            "Authorization code issued for user {UserId} to client {ClientId}",
            user.Id,
            request.ClientId);

        // Sign in via OpenIddict - this generates the authorization code,
        // binds it to the code_challenge (PKCE), and redirects to the redirect_uri
        // with the code and state parameters.
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Determines which tokens a claim should be included in (access token, identity token, or both).
    /// Standard OIDC claims (Subject, Name, Email) go to both access and identity tokens.
    /// Custom claims (Role, LabId) go only to access tokens.
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // All claims go to the access token.
        yield return OpenIddictConstants.Destinations.AccessToken;

        // Standard OIDC claims also go to the identity token.
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.IdentityToken;
                break;
        }
    }
}

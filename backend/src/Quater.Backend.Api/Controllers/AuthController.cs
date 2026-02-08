using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Quater.Backend.Api.Attributes;
using Quater.Shared.Models;
using Quater.Shared.Enums;
using System.Security.Claims;
using Microsoft.AspNetCore;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Authentication controller handling token management and user information.
/// Uses OAuth2/OpenIddict for authentication.
/// Supports authorization_code (with PKCE) and refresh_token grant types.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ILogger<AuthController> logger,
    IOpenIddictTokenManager tokenManager) : ControllerBase
{
    /// <summary>
    /// OAuth2 token endpoint - handles authorization_code and refresh_token grant types.
    /// 
    /// For authorization code exchange:
    ///   POST /api/auth/token
    ///   Content-Type: application/x-www-form-urlencoded
    ///   
    ///   grant_type=authorization_code&amp;code=AUTH_CODE&amp;code_verifier=PKCE_VERIFIER&amp;redirect_uri=quater://oauth/callback&amp;client_id=quater-mobile
    /// 
    /// For refresh:
    ///   POST /api/auth/token
    ///   Content-Type: application/x-www-form-urlencoded
    ///   
    ///   grant_type=refresh_token&amp;refresh_token=YOUR_REFRESH_TOKEN
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [Produces("application/json")]
    [EndpointRateLimit(10, 1, RateLimitTrackBy.IpAddress)]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
                      ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        // Handle authorization code grant type (PKCE-based - recommended)
        if (request.IsAuthorizationCodeGrantType())
        {
            // Retrieve the claims principal stored in the authorization code.
            // OpenIddict has already validated the code, redirect_uri, and code_verifier (PKCE)
            // before the request reaches this passthrough handler.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (result.Principal == null)
            {
                logger.LogWarning("Token exchange failed: Invalid authorization code");

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The authorization code is invalid or has expired."
                    }));
            }

            // Retrieve the user to ensure they still exist and are active.
            var userId = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
            var user = await userManager.Users
                .Include(u => u.UserLabs)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                logger.LogWarning("Token exchange failed: User {UserId} not found", userId);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The user no longer exists."
                    }));
            }

            if (!user.IsActive)
            {
                logger.LogWarning("Token exchange failed: User {UserId} is inactive", userId);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The account is inactive."
                    }));
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
                logger.LogWarning("Token exchange failed: User {UserId} is locked out until {LockoutEnd}",
                    userId, lockoutEnd);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] =
                            $"Account is locked out until {lockoutEnd?.DateTime:yyyy-MM-dd HH:mm:ss} UTC."
                    }));
            }

            // Update last login timestamp
            user.LastLogin = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            // Create a fresh claims principal with current user data.
            // Role and lab context are now determined per-request via middleware
            // based on the X-Lab-Id header and UserLab table.
            var claims = new List<Claim>
            {
                new(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
                new(OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty),
                new(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty)
            };

            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Restore the scopes from the original authorization request.
            claimsPrincipal.SetScopes(result.Principal.GetScopes());

            // Set destinations for claims (which tokens they should be included in).
            foreach (var claim in claimsPrincipal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim));
            }

            logger.LogInformation("User {UserId} authenticated successfully via authorization code exchange", userId);

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Handle refresh token grant type
        if (request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the refresh token
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (result.Principal == null)
            {
                logger.LogWarning("Token refresh failed: Invalid refresh token");

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The refresh token is invalid."
                    }));
            }

            // Retrieve the user profile corresponding to the refresh token
            var userId = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
            var user = await userManager.Users
                .Include(u => u.UserLabs)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                logger.LogWarning("Token refresh failed: User {UserId} not found", userId);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The user no longer exists."
                    }));
            }

            // Ensure the user is still active
            if (!user.IsActive)
            {
                logger.LogWarning("Token refresh failed: User {UserId} is inactive", userId);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The account is inactive."
                    }));
            }

            // Check if account is locked out
            if (await userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
                logger.LogWarning("Token refresh failed: User {UserId} is locked out until {LockoutEnd}", userId,
                    lockoutEnd);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] =
                            $"Account is locked out until {lockoutEnd?.DateTime:yyyy-MM-dd HH:mm:ss} UTC."
                    }));
            }

            // Create a new claims principal with updated claims.
            // Role and lab context are now determined per-request via middleware
            // based on the X-Lab-Id header and UserLab table.
            var claims = new List<Claim>
            {
                new(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
                new(OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty),
                new(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty)
            };

            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Restore the scopes from the original refresh token
            claimsPrincipal.SetScopes(result.Principal.GetScopes());

            // Set destinations for claims
            foreach (var claim in claimsPrincipal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim));
            }

            logger.LogInformation("User {UserId} refreshed token successfully", userId);

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Unsupported grant type
        logger.LogWarning("Token request failed: Unsupported grant type {GrantType}", request.GrantType);

        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [".error"] = OpenIddictConstants.Errors.UnsupportedGrantType,
                [".error_description"] = "The specified grant type is not supported."
            }));
    }

    /// <summary>
    /// Determines which tokens a claim should be included in (access token, identity token, etc.)
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Include the claim in access tokens
        yield return OpenIddictConstants.Destinations.AccessToken;

        // Include specific claims in identity tokens
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.IdentityToken;
                break;
        }
    }

    /// <summary>
    /// Logout and revoke all tokens for the user
    /// </summary>
    /// <remarks>
    /// This endpoint revokes ALL tokens for the user, effectively logging them out from all devices.
    /// For per-device logout, use the /revoke endpoint with the specific refresh token.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Revoke all tokens for this user
        var tokensRevoked = 0;
        await foreach (var token in tokenManager.FindBySubjectAsync(userId))
        {
            await tokenManager.TryRevokeAsync(token);
            tokensRevoked++;
        }

        await signInManager.SignOutAsync();

        logger.LogInformation("User {UserId} logged out successfully. Revoked {TokenCount} tokens",
            userId, tokensRevoked);

        return Ok(new { message = "Logged out successfully", tokensRevoked });
    }

    // Note: Token revocation endpoint (/api/auth/revoke) is handled automatically by OpenIddict
    // via SetRevocationEndpointUris configuration. No custom controller action needed.

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("userinfo")]
    [Authorize]
    public async Task<IActionResult> UserInfo()
    {
        var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.Users
            .Include(u => u.UserLabs)
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Get primary lab for legacy compatibility
        var primaryLab = user.UserLabs.FirstOrDefault();

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            userName = user.UserName,
            role = (primaryLab?.Role ?? UserRole.Viewer).ToString(),
            labId = primaryLab?.LabId ?? Guid.Empty,
            isActive = user.IsActive,
            lastLogin = user.LastLogin
        });
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Quater.Backend.Api.Attributes;
using Quater.Backend.Core.Constants;
using Quater.Shared.Models;
using System.Security.Claims;
using Microsoft.AspNetCore;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Authentication controller handling token management and user information.
/// Uses OAuth2/OpenIddict for authentication - clients should use the /token endpoint for login.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ILogger<AuthController> logger,
    IOpenIddictTokenManager tokenManager) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IOpenIddictTokenManager _tokenManager = tokenManager;

    /// <summary>
    /// OAuth2 token endpoint - handles password and refresh_token grant types.
    /// 
    /// For login, use:
    ///   POST /api/auth/token
    ///   Content-Type: application/x-www-form-urlencoded
    ///   
    ///   grant_type=password&amp;username=user@example.com&amp;password=secret&amp;scope=openid email profile offline_access api
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

        // Handle password grant type (username/password authentication)
        if (request.IsPasswordGrantType())
        {
            if (request.Username == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidRequest,
                        [".error_description"] = "The username is required."
                    }));
            }

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("Token request failed: User {Username} not found", request.Username);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The username or password is invalid."
                    }));
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Token request failed: User {Username} is inactive", request.Username);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The account is inactive."
                    }));
            }

            // Check if account is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                _logger.LogWarning("Token request failed: User {Username} is locked out until {LockoutEnd}",
                    request.Username, lockoutEnd);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] =
                            $"Account is locked out until {lockoutEnd?.DateTime:yyyy-MM-dd HH:mm:ss} UTC."
                    }));
            }

            // Verify password
            if (!await _userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
            {
                await _userManager.AccessFailedAsync(user);
                var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
                _logger.LogWarning(
                    "Token request failed: Invalid password for user {Username}. Failed attempts: {FailedAttempts}",
                    request.Username, failedAttempts);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The username or password is invalid."
                    }));
            }

            // Reset failed login attempts on successful authentication
            await _userManager.ResetAccessFailedCountAsync(user);

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Create claims principal
            var claims = new List<Claim>
            {
                new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
                new Claim(OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty),
                new Claim(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty),
                new Claim(QuaterClaimTypes.Role, user.Role.ToString()),
                new Claim(QuaterClaimTypes.LabId, user.LabId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Set scopes
            claimsPrincipal.SetScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.OfflineAccess, "api");

            // Set destinations for claims (which tokens they should be included in)
            foreach (var claim in claimsPrincipal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim));
            }

            _logger.LogInformation("User {Username} authenticated successfully via token endpoint", request.Username);

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Handle refresh token grant type
        if (request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the refresh token
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (result.Principal == null)
            {
                _logger.LogWarning("Token refresh failed: Invalid refresh token");

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
            var user = await _userManager.FindByIdAsync(userId ?? string.Empty);

            if (user == null)
            {
                _logger.LogWarning("Token refresh failed: User {UserId} not found", userId);

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
                _logger.LogWarning("Token refresh failed: User {UserId} is inactive", userId);

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [".error"] = OpenIddictConstants.Errors.InvalidGrant,
                        [".error_description"] = "The account is inactive."
                    }));
            }

            // Check if account is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                _logger.LogWarning("Token refresh failed: User {UserId} is locked out until {LockoutEnd}", userId,
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

            // Create a new claims principal with updated claims
            var claims = new List<Claim>
            {
                new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
                new Claim(OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty),
                new Claim(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty),
                new Claim(QuaterClaimTypes.Role, user.Role.ToString()),
                new Claim(QuaterClaimTypes.LabId, user.LabId.ToString())
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

            _logger.LogInformation("User {UserId} refreshed token successfully", userId);

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Unsupported grant type
        _logger.LogWarning("Token request failed: Unsupported grant type {GrantType}", request.GrantType);

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
            // Fall back to ClaimTypes.NameIdentifier if Subject claim not present
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Revoke all tokens for this user
        var tokensRevoked = 0;
        await foreach (var token in _tokenManager.FindBySubjectAsync(userId))
        {
            await _tokenManager.TryRevokeAsync(token);
            tokensRevoked++;
        }

        await _signInManager.SignOutAsync();

        _logger.LogInformation("User {UserId} logged out successfully. Revoked {TokenCount} tokens",
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            userName = user.UserName,
            role = user.Role.ToString(),
            labId = user.LabId,
            isActive = user.IsActive,
            lastLogin = user.LastLogin
        });
    }
}

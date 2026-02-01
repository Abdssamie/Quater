using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Quater.Backend.Core.Constants;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using System.Security.Claims;
using Microsoft.AspNetCore;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Authentication controller handling user registration, token management, and password operations.
/// Uses OAuth2/OpenIddict for authentication - clients should use the /token endpoint for login.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IOpenIddictTokenManager _tokenManager;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ILogger<AuthController> logger,
        IOpenIddictTokenManager tokenManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _tokenManager = tokenManager;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Role = request.Role,
            LabId = request.LabId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {Email} registered successfully with role {Role}", request.Email, request.Role);

        return Ok(new
        {
            message = "User registered successfully",
            userId = user.Id,
            email = user.Email,
            role = user.Role.ToString()
        });
    }

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
                new (OpenIddictConstants.Claims.Subject, user.Id),
                new (OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty),
                new (OpenIddictConstants.Claims.Email, user.Email ?? string.Empty),
                new (QuaterClaimTypes.Role, user.Role.ToString()),
                new (QuaterClaimTypes.LabId, user.LabId.ToString())
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
                new Claim(OpenIddictConstants.Claims.Subject, user.Id),
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
    /// Change password for authenticated user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

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

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {UserId} changed password successfully", userId);

        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Request password reset token (forgot password)
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            // Don't reveal that the user doesn't exist or is inactive
            return Ok(new { message = "If the email exists, a password reset link has been sent" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // TODO: Send email with reset token
        // In a real application, you would send an email here with a link containing the token
        _logger.LogInformation("Password reset requested for {Email}", request.Email);

        return Ok(new
        {
            message = "If the email exists, a password reset link has been sent"
        });
    }

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
            createdDate = user.CreatedDate,
            lastLogin = user.LastLogin
        });
    }
}

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid LabId { get; set; }
}

/// <summary>
/// Request model for password change
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request model for forgot password
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
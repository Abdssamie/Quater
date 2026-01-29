using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Quater.Backend.Api.Models.Auth;
using Quater.Backend.Core.Enums;
using Quater.Backend.Core.Models;
using Quater.Backend.Data;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Authentication controller for user registration, login, token refresh, and user info
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly QuaterDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly TimeProvider _timeProvider;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        QuaterDbContext context,
        ILogger<AuthController> logger,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>User information</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validate role
        if (!Enum.TryParse<UserRole>(request.Role, out var userRole))
        {
            return BadRequest(new { error = "Invalid role. Must be Admin, Technician, or Viewer." });
        }

        // Check if lab exists
        var lab = await _context.Labs.FindAsync(request.LabId);
        if (lab == null)
        {
            return BadRequest(new { error = "Lab not found." });
        }

        // Create user
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Role = userRole,
            LabId = request.LabId,
            CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {Email} registered successfully with role {Role}", user.Email, user.Role);

        return CreatedAtAction(nameof(GetUserInfo), new
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            LabId = user.LabId,
            IsActive = user.IsActive,
            LastLogin = user.LastLogin
        });
    }

    /// <summary>
    /// Issue access token and refresh token (OAuth2 password flow)
    /// </summary>
    /// <returns>Token response</returns>
    [HttpPost("token")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordFlow(request);
        }
        else if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenFlow(request);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>User information</returns>
    [HttpGet("userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new UserInfoResponse
        {
            Id = user.Id,
            Email = user.Email!,
            Role = user.Role.ToString(),
            LabId = user.LabId,
            IsActive = user.IsActive,
            LastLogin = user.LastLogin
        });
    }

    /// <summary>
    /// Logout (revoke refresh token)
    /// </summary>
    /// <returns>Success response</returns>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        // Sign out the user
        await _signInManager.SignOutAsync();

        _logger.LogInformation("User logged out successfully");

        return Ok(new { message = "Logged out successfully" });
    }

    #region Private Helper Methods

    private async Task<IActionResult> HandlePasswordFlow(OpenIddictRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Username!);
        if (user == null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidGrant,
                ErrorDescription = "The username or password is invalid."
            });
        }

        // Validate the password
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidGrant,
                    ErrorDescription = "Account is locked out. Please try again later."
                });
            }

            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidGrant,
                ErrorDescription = "The username or password is invalid."
            });
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidGrant,
                ErrorDescription = "Account is inactive."
            });
        }

        // Update last login
        user.LastLogin = _timeProvider.GetUtcNow().UtcDateTime;
        await _userManager.UpdateAsync(user);

        // Create claims principal
        var principal = await CreateClaimsPrincipal(user);

        // Sign in and return tokens
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenFlow(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the refresh token
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // Retrieve the user profile corresponding to the refresh token
        var userId = result.Principal?.GetClaim(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidGrant,
                ErrorDescription = "The refresh token is no longer valid."
            });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidGrant,
                ErrorDescription = "The refresh token is no longer valid."
            });
        }

        // Ensure the user is still allowed to sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidGrant,
                ErrorDescription = "The user is no longer allowed to sign in."
            });
        }

        // Create a new claims principal
        var principal = await CreateClaimsPrincipal(user);

        // Sign in and return new tokens
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<ClaimsPrincipal> CreateClaimsPrincipal(User user)
    {
        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        // Add standard claims
        identity.AddClaim(ClaimTypes.NameIdentifier, user.Id);
        identity.AddClaim(ClaimTypes.Name, user.Email!);
        identity.AddClaim(ClaimTypes.Email, user.Email!);
        identity.AddClaim(ClaimTypes.Role, user.Role.ToString());
        identity.AddClaim("lab_id", user.LabId.ToString());

        // Set destinations for claims (which tokens they should be included in)
        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id);
        identity.SetClaim(OpenIddictConstants.Claims.Email, user.Email!);
        identity.SetClaim(OpenIddictConstants.Claims.Role, user.Role.ToString());

        // Set destinations
        identity.SetDestinations(static claim => claim.Type switch
        {
            // Include in both access and identity tokens
            ClaimTypes.Name or
            ClaimTypes.Email or
            ClaimTypes.Role or
            OpenIddictConstants.Claims.Subject or
            OpenIddictConstants.Claims.Email or
            OpenIddictConstants.Claims.Role
                => new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken },

            // Include only in access token
            _ => new[] { OpenIddictConstants.Destinations.AccessToken }
        });

        var principal = new ClaimsPrincipal(identity);

        // Set scopes
        principal.SetScopes(new[]
        {
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.OfflineAccess
        });

        return await Task.FromResult(principal);
    }

    #endregion
}

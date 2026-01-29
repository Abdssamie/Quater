using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Authentication controller handling user registration, login, token management, and password operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
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
    /// Login with username and password to obtain access and refresh tokens
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt failed: User {Email} not found", request.Email);
            return Unauthorized(new { error = "Invalid email or password" });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt failed: User {Email} is inactive", request.Email);
            return Unauthorized(new { error = "Account is inactive" });
        }

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            _logger.LogWarning("Login attempt failed: User {Email} is locked out until {LockoutEnd}", request.Email, lockoutEnd);
            return Unauthorized(new { error = $"Account is locked out until {lockoutEnd?.DateTime:yyyy-MM-dd HH:mm:ss} UTC" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                _logger.LogWarning("User {Email} locked out after failed login attempts until {LockoutEnd}", request.Email, lockoutEnd);
                return Unauthorized(new { error = $"Account locked out until {lockoutEnd?.DateTime:yyyy-MM-dd HH:mm:ss} UTC due to multiple failed login attempts" });
            }

            var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
            _logger.LogWarning("Login attempt failed for user {Email}. Failed attempts: {FailedAttempts}", request.Email, failedAttempts);
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Reset failed login attempts on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("role", user.Role.ToString()),
            new Claim("lab_id", user.LabId.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Sign in and return tokens
        claimsPrincipal.SetScopes(new[] { Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.OfflineAccess, "api" });

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Refresh access token using refresh token (placeholder - use OAuth2 token endpoint)
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        // Note: OpenIddict handles refresh tokens through the OAuth2 token endpoint
        // Clients should use POST /api/auth/token with grant_type=refresh_token
        return BadRequest(new 
        { 
            error = "Use the OAuth2 token endpoint",
            message = "POST /api/auth/token with grant_type=refresh_token and refresh_token parameter"
        });
    }

    /// <summary>
    /// Logout and revoke tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _signInManager.SignOutAsync();

        _logger.LogInformation("User {UserId} logged out successfully", userId);

        return Ok(new { message = "Logged out successfully" });
    }

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
        // For now, we'll just log it (DO NOT do this in production!)
        _logger.LogInformation("Password reset token for {Email}: {Token}", request.Email, token);

        return Ok(new
        {
            message = "If the email exists, a password reset link has been sent",
            // TODO: Remove this in production - only for development/testing
            resetToken = token
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
/// Request model for user login
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request model for refresh token
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Web;
using Microsoft.AspNetCore;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Authentication controller handling user registration, token management, and password operations.
/// Uses OAuth2/OpenIddict for authentication - clients should use the /token endpoint for login.
/// </summary>
// TODO: [MEDIUM PRIORITY] Split AuthController into focused controllers (Est: 4 hours)
// This controller is large (800+ lines) and handles multiple responsibilities.
// Consider splitting into:
//   - AuthController (token endpoint only)
//   - RegistrationController (user registration)
//   - PasswordController (password operations)
//   - EmailVerificationController (email verification)
// This is functional but could improve maintainability.
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly IEmailQueue _emailQueue;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IConfiguration _configuration;
    private readonly EmailSettings _emailSettings;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ILogger<AuthController> logger,
        IOpenIddictTokenManager tokenManager,
        IEmailQueue emailQueue,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        IOptions<EmailSettings> emailSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _tokenManager = tokenManager;
        _emailQueue = emailQueue;
        _emailTemplateService = emailTemplateService;
        _configuration = configuration;
        _emailSettings = emailSettings.Value;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    // TODO: [MEDIUM PRIORITY] Add stricter rate limiting for auth endpoints (Est: 2 hours)
    // Currently uses global rate limit (100 req/min for authenticated, 20 for anonymous).
    // Auth endpoints should have stricter limits to prevent brute force attacks:
    //   - Register: 3 attempts per hour per IP
    //   - ForgotPassword: 5 attempts per 15 minutes per email
    //   - ResetPassword: 5 attempts per 15 minutes per email
    // Consider creating a [StrictRateLimit] attribute or using ASP.NET Core rate limiting.
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
            IsActive = true,
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {Email} registered successfully with role {Role}", request.Email, request.Role);

        // Send verification email
        try
        {
            await SendVerificationEmailAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            // Don't fail registration if email fails
        }

        return Ok(new
        {
            message = "User registered successfully. Please check your email to verify your account.",
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

        // Always return success to prevent email enumeration
        if (user == null || !user.IsActive)
        {
            return Ok(new { message = "If the email exists, a password reset link has been sent" });
        }

        try
        {
            await SendPasswordResetEmailAsync(user);
            _logger.LogInformation("Password reset email sent to {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", request.Email);
            // Don't reveal the error to prevent information disclosure
        }

        // TODO: [LOW PRIORITY] Fix timing attack vulnerability (Est: 1 hour)
        // The response time differs when email exists vs doesn't exist, potentially
        // leaking user existence information. Add constant-time delay or always
        // perform same operations regardless of email existence.
        // Risk is low for MVP but should be addressed before public launch.
        return Ok(new { message = "If the email exists, a password reset link has been sent" });
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
            lastLogin = user.LastLogin
        });
    }

    /// <summary>
    /// Verify user email address using a token
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (user.EmailConfirmed)
        {
            return Ok(new { message = "Email already verified" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Code);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Email verification failed for user {UserId}: {Errors}",
                request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { error = "Invalid or expired verification code" });
        }

        _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);

        // Send welcome email
        try
        {
            await SendWelcomeEmailAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
            // Don't fail verification if welcome email fails
        }

        return Ok(new { message = "Email verified successfully" });
    }

    /// <summary>
    /// Resend the email verification link
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always return success to prevent email enumeration
        if (user == null || user.EmailConfirmed)
        {
            return Ok(new { message = "If the email exists and is not verified, a verification link has been sent" });
        }

        try
        {
            await SendVerificationEmailAsync(user);
            _logger.LogInformation("Verification email resent to {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email to {Email}", request.Email);
        }

        return Ok(new { message = "If the email exists and is not verified, a verification link has been sent" });
    }

    /// <summary>
    /// Reset password using a valid token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Code, request.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Password reset failed for user {Email}: {Errors}",
                request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { error = "Invalid or expired reset token" });
        }

        _logger.LogInformation("Password reset successfully for user {Email}", user.Email);

        // Send security alert email
        try
        {
            await SendSecurityAlertEmailAsync(user, "Password Reset", "Your password was successfully reset.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send security alert email to {Email}", user.Email);
            // Don't fail password reset if alert email fails
        }

        return Ok(new { message = "Password reset successfully" });
    }

    /// <summary>
    /// Helper method to send verification email
    /// </summary>
    private async Task SendVerificationEmailAsync(User user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var frontendUrl = _emailSettings.FrontendUrl;

        // URL encode the token and userId
        var encodedToken = HttpUtility.UrlEncode(token);
        var verificationUrl = $"{frontendUrl}/verify-email?userId={user.Id}&code={encodedToken}";

        var model = new VerificationEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            VerificationUrl = verificationUrl,
            ExpirationHours = 24
        };

        var htmlBody = await _emailTemplateService.RenderAsync("verification", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = "Verify Your Email Address - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await _emailQueue.QueueAsync(new EmailQueueItem(emailDto));
    }

    /// <summary>
    /// Helper method to send welcome email
    /// </summary>
    private async Task SendWelcomeEmailAsync(User user)
    {
        var frontendUrl = _emailSettings.FrontendUrl;

        var model = new WelcomeEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            LoginUrl = $"{frontendUrl}/login"
        };

        var htmlBody = await _emailTemplateService.RenderAsync("welcome", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = "Welcome to Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await _emailQueue.QueueAsync(new EmailQueueItem(emailDto));
    }

    /// <summary>
    /// Helper method to send password reset email
    /// </summary>
    private async Task SendPasswordResetEmailAsync(User user)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var frontendUrl = _emailSettings.FrontendUrl;

        // URL encode the token and email
        var encodedToken = HttpUtility.UrlEncode(token);
        var encodedEmail = HttpUtility.UrlEncode(user.Email);
        var resetUrl = $"{frontendUrl}/reset-password?email={encodedEmail}&code={encodedToken}";

        var model = new PasswordResetEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            ResetUrl = resetUrl,
            ExpirationMinutes = 60
        };

        var htmlBody = await _emailTemplateService.RenderAsync("password-reset", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = "Reset Your Password - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await _emailQueue.QueueAsync(new EmailQueueItem(emailDto));
    }

    /// <summary>
    /// Helper method to send security alert email
    /// </summary>
    private async Task SendSecurityAlertEmailAsync(User user, string alertType, string alertMessage)
    {
        var model = new SecurityAlertEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            AlertType = alertType,
            AlertMessage = alertMessage,
            Timestamp = DateTimeOffset.UtcNow
        };

        var htmlBody = await _emailTemplateService.RenderAsync("security-alert", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = $"Security Alert: {alertType} - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await _emailQueue.QueueAsync(new EmailQueueItem(emailDto));
    }
}

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }

    [Required(ErrorMessage = "Lab ID is required")]
    public Guid LabId { get; set; }
}

/// <summary>
/// Request model for password change
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request model for forgot password
/// </summary>
public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request model for resetting password with token
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reset code is required")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request model for email verification
/// </summary>
public class VerifyEmailRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Request model for resending verification email
/// </summary>
public class ResendVerificationRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
}
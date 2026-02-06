using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Attributes;
using Quater.Backend.Api.Helpers;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Password management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PasswordController(
    UserManager<User> userManager,
    ILogger<PasswordController> logger,
    IEmailQueue emailQueue,
    IEmailTemplateService emailTemplateService,
    IOptions<EmailSettings> emailSettings) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<PasswordController> _logger = logger;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [HttpPost("change")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
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
    [HttpPost("forgot")]
    [AllowAnonymous]
    [EndpointRateLimit(10, 60, RateLimitTrackBy.Email)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Start timing to ensure consistent response time regardless of email existence
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user != null && user.IsActive)
        {
            try
            {
                await AuthHelpers.SendPasswordResetEmailAsync(
                    user,
                    _userManager,
                    _emailQueue,
                    _emailTemplateService,
                    _emailSettings);
                _logger.LogInformation("Password reset email sent to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", request.Email);
                // Don't reveal the error to prevent information disclosure
            }
        }

        // Add constant-time delay to prevent timing attack vulnerability
        // This ensures response time is consistent regardless of whether email exists
        var elapsed = stopwatch.ElapsedMilliseconds;
        var remainingDelay = 200 - (int)elapsed;
        if (remainingDelay > 0)
        {
            await Task.Delay(remainingDelay);
        }

        return Ok(new { message = "If the email exists, a password reset link has been sent" });
    }

    /// <summary>
    /// Reset password using a valid token
    /// </summary>
    [HttpPost("reset")]
    [AllowAnonymous]
    [EndpointRateLimit(10, 60, RateLimitTrackBy.Email)]
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
            await AuthHelpers.SendSecurityAlertEmailAsync(
                user,
                "Password Reset",
                "Your password was successfully reset.",
                _emailQueue,
                _emailTemplateService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send security alert email to {Email}", user.Email);
            // Don't fail password reset if alert email fails
        }

        return Ok(new { message = "Password reset successfully" });
    }
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

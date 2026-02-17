using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quater.Backend.Api.Attributes;
using Quater.Backend.Api.Helpers;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Email verification controller
/// </summary>
[ApiController]
[Route("api/email-verification")]
public sealed class EmailVerificationController(
    UserManager<User> userManager,
    ILogger<EmailVerificationController> logger,
    IEmailQueue emailQueue,
    IEmailTemplateService emailTemplateService,
    IOptions<EmailSettings> emailSettings) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<EmailVerificationController> _logger = logger;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    /// <summary>
    /// Verify user email address using a token
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous]
    [EndpointRateLimit(10, 60, RateLimitTrackBy.IpAddress)]
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
            await AuthHelpers.SendWelcomeEmailAsync(
                user,
                _emailQueue,
                _emailTemplateService,
                _emailSettings);
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
    [HttpPost("resend")]
    [AllowAnonymous]
    [EndpointRateLimit(5, 60, RateLimitTrackBy.Email)]
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
            await AuthHelpers.SendVerificationEmailAsync(
                user,
                _userManager,
                _emailQueue,
                _emailTemplateService,
                _emailSettings);
            _logger.LogInformation("Verification email resent to {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email to {Email}", request.Email);
        }

        return Ok(new { message = "If the email exists and is not verified, a verification link has been sent" });
    }
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

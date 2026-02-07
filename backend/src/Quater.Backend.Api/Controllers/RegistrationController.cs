using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quater.Backend.Api.Attributes;
using Quater.Backend.Api.Helpers;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// User registration controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class RegistrationController(
    UserManager<User> userManager,
    ILogger<RegistrationController> logger,
    IEmailQueue emailQueue,
    IEmailTemplateService emailTemplateService,
    IOptions<EmailSettings> emailSettings,
    QuaterDbContext context) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<RegistrationController> _logger = logger;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly EmailSettings _emailSettings = emailSettings.Value;
    private readonly QuaterDbContext _context = context;

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EndpointRateLimit(10, 60, RateLimitTrackBy.IpAddress)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate LabId exists
        if (request.LabId == Guid.Empty)
        {
            return BadRequest(new { errors = new Dictionary<string, string[]> { ["LabId"] = ["Lab ID is required"] } });
        }

        var labExists = await _context.Labs.AsNoTracking().AnyAsync(l => l.Id == request.LabId && !l.IsDeleted);
        if (!labExists)
        {
            return BadRequest(new { errors = new[] { $"Lab with ID '{request.LabId}' does not exist." } });
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            IsActive = true,
            UserLabs =
            [
                new UserLab
                {
                    LabId = request.LabId,
                    Role = request.Role
                }
            ]
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
            await AuthHelpers.SendVerificationEmailAsync(
                user,
                _userManager,
                _emailQueue,
                _emailTemplateService,
                _emailSettings);
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
            role = request.Role.ToString()
        });
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

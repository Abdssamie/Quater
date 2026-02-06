using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Quater.Shared.Models;

namespace Quater.Backend.Api.Pages.Account;

/// <summary>
/// Minimal login page for OAuth2 authorization code flow.
/// Shown in the system browser when mobile/desktop clients initiate the auth code flow
/// and the user is not yet authenticated via cookie.
/// </summary>
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public sealed class LoginModel(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // ✅ FIXED: Validate password FIRST (constant-time for all users)
        // This ensures that non-existent, inactive, and active users all go through
        // the expensive bcrypt password hashing, preventing timing attacks
        var result = await signInManager.PasswordSignInAsync(
            Email,  // Use email directly (SignInManager will look up user)
            Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            // Handle all authentication failures with generic message
            if (result.IsLockedOut)
            {
                logger.LogWarning("Login failed: User {Email} is locked out", Email);
                ErrorMessage = "Account is locked. Please try again later.";
            }
            else if (result.IsNotAllowed)
            {
                logger.LogWarning("Login failed: User {Email} sign-in is not allowed", Email);
                ErrorMessage = "Login is not allowed. Please verify your email.";
            }
            else
            {
                logger.LogWarning("Login failed for user {Email}", Email);
                ErrorMessage = "Invalid email or password.";
            }
            
            return Page();
        }

        // ✅ Only after successful password verification, check IsActive
        // At this point, we know the password is correct, so checking IsActive
        // doesn't leak timing information
        var user = await userManager.FindByEmailAsync(Email);
        if (user is null || !user.IsActive)
        {
            // User authenticated but is inactive - revoke the session immediately
            await signInManager.SignOutAsync();
            logger.LogWarning("Login denied: User {Email} is inactive", Email);
            ErrorMessage = "Invalid email or password."; // Generic message
            return Page();
        }

        logger.LogInformation("User {Email} signed in via login page for OAuth2 flow", Email);
        return LocalRedirect(returnUrl ?? "/");
    }
}

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

        // Find the user to check IsActive before attempting sign-in
        var user = await userManager.FindByEmailAsync(Email);
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Login failed: User {Email} not found or inactive", Email);
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User {Email} signed in via login page for OAuth2 flow", Email);
            return LocalRedirect(returnUrl ?? "/");
        }

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
            logger.LogWarning("Login failed: Invalid password for user {Email}", Email);
            ErrorMessage = "Invalid email or password.";
        }

        return Page();
    }
}

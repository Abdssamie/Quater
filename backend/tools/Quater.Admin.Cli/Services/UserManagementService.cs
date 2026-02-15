using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Quater.Shared.Models;

namespace Quater.Admin.Cli.Services;

/// <summary>
/// Business logic for user management operations.
/// Decoupled from CLI framework - can be tested independently.
/// </summary>
public sealed class UserManagementService(
    UserManager<User> userManager,
    ILogger<UserManagementService> logger)
{
    public async Task<IdentityResult> ResetPasswordAsync(
        string email,
        string newPassword,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new InvalidOperationException($"User with email '{email}' not found");
        }

        logger.LogInformation("Resetting password for user {Email} (ID: {UserId})", email, user.Id);

        // For CLI admin tool, directly set password without token-based reset
        // This avoids dependency on IDataProtectionProvider
        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            logger.LogWarning(
                "Failed to remove existing password for user {UserId}: {Errors}",
                user.Id,
                string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            return removeResult;
        }

        var result = await userManager.AddPasswordAsync(user, newPassword);

        if (result.Succeeded)
        {
            logger.LogInformation("Password reset successful for user {UserId}", user.Id);
        }
        else
        {
            logger.LogWarning(
                "Password reset failed for user {UserId}: {Errors}",
                user.Id,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return result;
    }
}
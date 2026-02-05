using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Models;
using System.Web;

namespace Quater.Backend.Api.Helpers;

/// <summary>
/// Static helper methods for authentication-related email operations
/// </summary>
public static class AuthHelpers
{
    /// <summary>
    /// Helper method to send verification email
    /// </summary>
    public static async Task SendVerificationEmailAsync(
        User user,
        UserManager<User> userManager,
        IEmailQueue emailQueue,
        IEmailTemplateService emailTemplateService,
        EmailSettings emailSettings)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var frontendUrl = emailSettings.FrontendUrl;

        // URL encode the token and userId
        var encodedToken = HttpUtility.UrlEncode(token);
        var verificationUrl = $"{frontendUrl}/verify-email?userId={user.Id}&code={encodedToken}";

        var model = new VerificationEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            VerificationUrl = verificationUrl,
            ExpirationHours = 24
        };

        var htmlBody = await emailTemplateService.RenderAsync("verification", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = "Verify Your Email Address - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await emailQueue.QueueAsync(new EmailQueueItem(emailDto));
    }

    /// <summary>
    /// Helper method to send password reset email
    /// </summary>
    public static async Task SendPasswordResetEmailAsync(
        User user,
        UserManager<User> userManager,
        IEmailQueue emailQueue,
        IEmailTemplateService emailTemplateService,
        EmailSettings emailSettings)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var frontendUrl = emailSettings.FrontendUrl;

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

        var htmlBody = await emailTemplateService.RenderAsync("password-reset", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = "Reset Your Password - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await emailQueue.QueueAsync(new EmailQueueItem(emailDto));
    }

    /// <summary>
    /// Helper method to send welcome email
    /// </summary>
    public static async Task SendWelcomeEmailAsync(
        User user,
        IEmailQueue emailQueue,
        IEmailTemplateService emailTemplateService,
        EmailSettings emailSettings)
    {
        var frontendUrl = emailSettings.FrontendUrl;

        var model = new WelcomeEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            LoginUrl = $"{frontendUrl}/login"
        };

        var htmlBody = await emailTemplateService.RenderAsync("welcome", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = "Welcome to Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await emailQueue.QueueAsync(new EmailQueueItem(emailDto));
    }

    /// <summary>
    /// Helper method to send security alert email
    /// </summary>
    public static async Task SendSecurityAlertEmailAsync(
        User user,
        string alertType,
        string alertMessage,
        IEmailQueue emailQueue,
        IEmailTemplateService emailTemplateService)
    {
        var model = new SecurityAlertEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            AlertType = alertType,
            AlertMessage = alertMessage,
            Timestamp = DateTimeOffset.UtcNow
        };

        var htmlBody = await emailTemplateService.RenderAsync("security-alert", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = $"Security Alert: {alertType} - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await emailQueue.QueueAsync(new EmailQueueItem(emailDto));
    }
}

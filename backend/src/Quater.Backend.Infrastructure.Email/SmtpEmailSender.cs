namespace Quater.Backend.Infrastructure.Email;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

/// <summary>
/// SMTP-based email sender implementation using MailKit
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailSettings> settings,
    IEmailTemplateService templateService,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task<EmailSendResult> SendAsync(SendEmailDto email, CancellationToken cancellationToken = default)
    {
        // Check if email sending is enabled
        if (!_settings.Enabled)
        {
            logger.LogWarning(
                "Email sending disabled. Would send to {To}: {Subject}",
                email.To, email.Subject);
            return EmailSendResult.Success("disabled-" + Guid.NewGuid());
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(email.To));
            message.Subject = email.Subject;

            if (!string.IsNullOrEmpty(email.ReplyTo))
            {
                message.ReplyTo.Add(MailboxAddress.Parse(email.ReplyTo));
            }

            var bodyBuilder = new BodyBuilder();
            if (email.IsHtml)
            {
                bodyBuilder.HtmlBody = email.Body;
            }
            else
            {
                bodyBuilder.TextBody = email.Body;
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Connect to SMTP server
            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_settings.SmtpUsername) && !string.IsNullOrEmpty(_settings.SmtpPassword))
            {
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);
            }

            // Send email
            var messageId = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'. MessageId: {MessageId}",
                email.To, email.Subject, messageId);

            return EmailSendResult.Success(messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'", email.To, email.Subject);
            return EmailSendResult.Failure(ex.Message);
        }
    }

    public async Task<EmailSendResult> SendVerificationEmailAsync(
        string email,
        string userName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var verificationUrl = $"{_settings.FrontendUrl}/auth/verify-email?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(email)}";

        var model = new VerificationEmailModel
        {
            UserName = userName,
            VerificationUrl = verificationUrl,
            ExpirationHours = 24
        };

        var htmlBody = await templateService.RenderAsync("verification", model, cancellationToken);

        var emailDto = new SendEmailDto
        {
            To = email,
            Subject = "Verify Your Email Address - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        return await SendAsync(emailDto, cancellationToken);
    }

    public async Task<EmailSendResult> SendPasswordResetEmailAsync(
        string email,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_settings.FrontendUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";

        var model = new PasswordResetEmailModel
        {
            UserName = userName,
            ResetUrl = resetUrl,
            ExpirationMinutes = 60
        };

        var htmlBody = await templateService.RenderAsync("password-reset", model, cancellationToken);

        var emailDto = new SendEmailDto
        {
            To = email,
            Subject = "Reset Your Password - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        return await SendAsync(emailDto, cancellationToken);
    }

    public async Task<EmailSendResult> SendWelcomeEmailAsync(
        string email,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var model = new WelcomeEmailModel
        {
            UserName = userName,
            LoginUrl = $"{_settings.FrontendUrl}/auth/login"
        };

        var htmlBody = await templateService.RenderAsync("welcome", model, cancellationToken);

        var emailDto = new SendEmailDto
        {
            To = email,
            Subject = "Welcome to Quater Water Quality!",
            Body = htmlBody,
            IsHtml = true
        };

        return await SendAsync(emailDto, cancellationToken);
    }

    public async Task<EmailSendResult> SendSecurityAlertAsync(
        string email,
        string userName,
        string alertType,
        string alertMessage,
        CancellationToken cancellationToken = default)
    {
        var model = new SecurityAlertEmailModel
        {
            UserName = userName,
            AlertType = alertType,
            AlertMessage = alertMessage
        };

        var htmlBody = await templateService.RenderAsync("security-alert", model, cancellationToken);

        var emailDto = new SendEmailDto
        {
            To = email,
            Subject = $"Security Alert: {alertType} - Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        return await SendAsync(emailDto, cancellationToken);
    }
}

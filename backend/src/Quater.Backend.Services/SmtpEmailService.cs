namespace Quater.Backend.Services;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

/// <summary>
/// SMTP-based email service using MailKit and Scriban templates
/// </summary>
public sealed class SmtpEmailService : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly IEmailTemplateService _templateService;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly string _baseUrl;

    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15)
    ];

    public SmtpEmailService(
        IConfiguration configuration,
        ILogger<SmtpEmailService> logger,
        IEmailTemplateService templateService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));

        _fromAddress = _configuration["Email:From:Address"] ?? "noreply@quater.app";
        _fromName = _configuration["Email:From:Name"] ?? "Quater Water Quality";
        _baseUrl = _configuration["Email:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<EmailSendResult> SendAsync(SendEmailDto email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        var message = CreateMessage(email);
        return await SendWithRetryAsync(message, cancellationToken);
    }

    public async Task<EmailSendResult> SendVerificationEmailAsync(
        string email,
        string userName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var verificationUrl = $"{_baseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(email)}";

        var model = new VerificationEmailModel
        {
            UserName = userName,
            VerificationUrl = verificationUrl
        };

        var body = await _templateService.RenderAsync("verification", model, cancellationToken);

        var dto = new SendEmailDto
        {
            To = email,
            Subject = "Verify Your Email - Quater Water Quality",
            Body = body,
            IsHtml = true
        };

        _logger.LogInformation(
            "Sending verification email to {Email} for user {UserName}",
            email, userName);

        return await SendAsync(dto, cancellationToken);
    }

    public async Task<EmailSendResult> SendPasswordResetEmailAsync(
        string email,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";

        var model = new PasswordResetEmailModel
        {
            UserName = userName,
            ResetUrl = resetUrl
        };

        var body = await _templateService.RenderAsync("password-reset", model, cancellationToken);

        var dto = new SendEmailDto
        {
            To = email,
            Subject = "Reset Your Password - Quater Water Quality",
            Body = body,
            IsHtml = true
        };

        _logger.LogInformation(
            "Sending password reset email to {Email} for user {UserName}",
            email, userName);

        return await SendAsync(dto, cancellationToken);
    }

    public async Task<EmailSendResult> SendWelcomeEmailAsync(
        string email,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var loginUrl = $"{_baseUrl}/login";

        var model = new WelcomeEmailModel
        {
            UserName = userName,
            LoginUrl = loginUrl
        };

        var body = await _templateService.RenderAsync("welcome", model, cancellationToken);

        var dto = new SendEmailDto
        {
            To = email,
            Subject = "Welcome to Quater Water Quality!",
            Body = body,
            IsHtml = true
        };

        _logger.LogInformation(
            "Sending welcome email to {Email} for user {UserName}",
            email, userName);

        return await SendAsync(dto, cancellationToken);
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

        var body = await _templateService.RenderAsync("security-alert", model, cancellationToken);

        var dto = new SendEmailDto
        {
            To = email,
            Subject = $"Security Alert - {alertType} - Quater Water Quality",
            Body = body,
            IsHtml = true
        };

        _logger.LogWarning(
            "Sending security alert email to {Email} for user {UserName}: {AlertType}",
            email, userName, alertType);

        return await SendAsync(dto, cancellationToken);
    }

    private MimeMessage CreateMessage(SendEmailDto email)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromAddress));
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
        return message;
    }

    private async Task<EmailSendResult> SendWithRetryAsync(
        MimeMessage message,
        CancellationToken cancellationToken)
    {
        var lastError = string.Empty;

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                using var client = new SmtpClient();

                var host = _configuration["Email:Smtp:Host"] ?? "localhost";
                var port = _configuration.GetValue("Email:Smtp:Port", 1025);
                var enableSsl = _configuration.GetValue("Email:Smtp:EnableSsl", false);
                var username = _configuration["Email:Smtp:Username"];
                var password = _configuration["Email:Smtp:Password"];

                var secureSocketOptions = enableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

                await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password, cancellationToken);
                }

                var messageId = await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation(
                    "Email sent successfully to {To} with MessageId {MessageId}",
                    message.To, messageId);

                return EmailSendResult.Success(messageId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex.Message;

                _logger.LogWarning(
                    ex,
                    "Failed to send email to {To} on attempt {Attempt}/{MaxRetries}",
                    message.To, attempt + 1, MaxRetries);

                if (attempt < MaxRetries - 1)
                {
                    await Task.Delay(RetryDelays[attempt], cancellationToken);
                }
            }
        }

        _logger.LogError(
            "Failed to send email to {To} after {MaxRetries} attempts: {Error}",
            message.To, MaxRetries, lastError);

        return EmailSendResult.Failure(lastError);
    }
}

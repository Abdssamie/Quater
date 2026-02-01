namespace Quater.Backend.Core.Interfaces;

using Quater.Backend.Core.DTOs;

/// <summary>
/// Email sending service interface
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Send a generic email
    /// </summary>
    Task<EmailSendResult> SendAsync(SendEmailDto email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email verification link to new user
    /// </summary>
    Task<EmailSendResult> SendVerificationEmailAsync(
        string email,
        string userName,
        string verificationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send password reset link
    /// </summary>
    Task<EmailSendResult> SendPasswordResetEmailAsync(
        string email,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send welcome email after successful registration
    /// </summary>
    Task<EmailSendResult> SendWelcomeEmailAsync(
        string email,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send security alert (login from new device, password changed, etc.)
    /// </summary>
    Task<EmailSendResult> SendSecurityAlertAsync(
        string email,
        string userName,
        string alertType,
        string alertMessage,
        CancellationToken cancellationToken = default);
}

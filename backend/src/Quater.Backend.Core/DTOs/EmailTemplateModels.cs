namespace Quater.Backend.Core.DTOs;

/// <summary>
/// Base model for all email templates
/// </summary>
public abstract record EmailTemplateModel
{
    public required string UserName { get; init; }
    public int Year { get; init; } = DateTime.UtcNow.Year;
    public string AppName { get; init; } = "Quater Water Quality";
}

/// <summary>
/// Model for email verification template
/// </summary>
public sealed record VerificationEmailModel : EmailTemplateModel
{
    public required string VerificationUrl { get; init; }
    public int ExpirationHours { get; init; } = 24;
}

/// <summary>
/// Model for password reset template
/// </summary>
public sealed record PasswordResetEmailModel : EmailTemplateModel
{
    public required string ResetUrl { get; init; }
    public int ExpirationMinutes { get; init; } = 60;
}

/// <summary>
/// Model for invitation email template
/// </summary>
public sealed record InvitationEmailModel : EmailTemplateModel
{
    public required string InvitationUrl { get; init; }
    public required string InvitedByName { get; init; }
    public int ExpirationDays { get; init; }
}

/// <summary>
/// Model for welcome email template
/// </summary>
public sealed record WelcomeEmailModel : EmailTemplateModel
{
    public required string LoginUrl { get; init; }

    public string[] Features { get; init; } =
    [
        "Track and analyze water quality samples",
        "Monitor compliance with WHO standards",
        "Access your data from any device",
        "Sync data automatically across your team"
    ];
}

/// <summary>
/// Model for security alert template
/// </summary>
public sealed record SecurityAlertEmailModel : EmailTemplateModel
{
    public required string AlertType { get; init; }
    public required string AlertMessage { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string TimestampFormatted => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
}

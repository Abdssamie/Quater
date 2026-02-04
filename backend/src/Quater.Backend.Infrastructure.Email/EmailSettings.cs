namespace Quater.Backend.Infrastructure.Email;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Email configuration settings
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server hostname
    /// </summary>
    [Required(ErrorMessage = "SMTP host is required")]
    public required string SmtpHost { get; init; }

    /// <summary>
    /// SMTP server port (typically 587 for TLS, 465 for SSL, 25 for unencrypted)
    /// </summary>
    [Range(1, 65535, ErrorMessage = "SMTP port must be between 1 and 65535")]
    public int SmtpPort { get; init; } = 587;

    /// <summary>
    /// Use SSL/TLS for connection
    /// </summary>
    public bool UseSsl { get; init; } = true;

    /// <summary>
    /// SMTP username for authentication
    /// </summary>
    public string? SmtpUsername { get; init; }

    /// <summary>
    /// SMTP password for authentication
    /// </summary>
    public string? SmtpPassword { get; init; }

    /// <summary>
    /// From email address
    /// </summary>
    [Required(ErrorMessage = "From address is required")]
    [EmailAddress(ErrorMessage = "From address must be a valid email")]
    public required string FromAddress { get; init; }

    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; init; } = "Quater Water Quality";

    /// <summary>
    /// Frontend URL for generating links in emails
    /// </summary>
    [Required(ErrorMessage = "Frontend URL is required")]
    [Url(ErrorMessage = "Frontend URL must be a valid URL")]
    public required string FrontendUrl { get; init; }

    /// <summary>
    /// Enable email sending (false for development/testing)
    /// </summary>
    public bool Enabled { get; init; } = true;
}

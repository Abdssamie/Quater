namespace Quater.Backend.Services;

using Microsoft.Extensions.Logging;
using Quater.Backend.Core.Interfaces;
using Scriban;
using Scriban.Runtime;

/// <summary>
/// Scriban-based email template rendering service
/// </summary>
public sealed class ScribanEmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<ScribanEmailTemplateService> _logger;
    private readonly Dictionary<string, Template> _templateCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();

    public ScribanEmailTemplateService(ILogger<ScribanEmailTemplateService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeTemplates();
    }

    public Task<string> RenderAsync<TModel>(
        string templateName,
        TModel model,
        CancellationToken cancellationToken = default) where TModel : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(model);

        lock (_cacheLock)
        {
            if (!_templateCache.TryGetValue(templateName, out var template))
            {
                _logger.LogError("Email template {TemplateName} not found", templateName);
                throw new InvalidOperationException($"Template '{templateName}' not found");
            }

            var scriptObject = new ScriptObject();
            scriptObject.Import(model, renamer: member => member.Name.ToLowerInvariant());

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            var result = template.Render(context);

            _logger.LogDebug("Rendered email template {TemplateName}", templateName);

            return Task.FromResult(result);
        }
    }

    private void InitializeTemplates()
    {
        RegisterTemplate("verification", VerificationTemplate);
        RegisterTemplate("password-reset", PasswordResetTemplate);
        RegisterTemplate("welcome", WelcomeTemplate);
        RegisterTemplate("security-alert", SecurityAlertTemplate);

        _logger.LogInformation("Initialized {Count} email templates", _templateCache.Count);
    }

    private void RegisterTemplate(string name, string templateContent)
    {
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            var errors = string.Join(", ", template.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"Template '{name}' has errors: {errors}");
        }

        _templateCache[name] = template;
    }

    #region Template Definitions

    private const string BaseStyle = """
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
            .header { background: linear-gradient(135deg, #0066cc, #004d99); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }
            .header h1 { margin: 0; font-size: 24px; }
            .content { background: #f9f9f9; padding: 30px; border: 1px solid #e0e0e0; }
            .button { display: inline-block; background: #0066cc; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }
            .button:hover { background: #004d99; }
            .footer { text-align: center; padding: 20px; font-size: 12px; color: #666; }
            .alert { background: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 6px; margin: 15px 0; }
            .alert-danger { background: #f8d7da; border-color: #f5c6cb; }
            code { background: #e9ecef; padding: 2px 6px; border-radius: 3px; font-family: monospace; }
        </style>
        """;

    private static readonly string VerificationTemplate = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
        """ + BaseStyle + """
        </head>
        <body>
            <div class="header">
                <h1>🔬 {{ appname }}</h1>
            </div>
            <div class="content">
                <h2>Verify Your Email Address</h2>
                <p>Hello {{ username }},</p>
                <p>Thank you for registering with {{ appname }}. Please click the button below to verify your email address:</p>
                <p style="text-align: center;">
                    <a href="{{ verificationurl }}" class="button">Verify Email</a>
                </p>
                <p>Or copy and paste this link into your browser:</p>
                <p style="word-break: break-all; background: #e9ecef; padding: 10px; border-radius: 4px; font-size: 12px;">{{ verificationurl }}</p>
                <p>This link will expire in {{ expirationhours }} hours.</p>
                <p>If you didn't create an account, you can safely ignore this email.</p>
            </div>
            <div class="footer">
                <p>&copy; {{ year }} {{ appname }} Management System</p>
                <p>This is an automated message. Please do not reply to this email.</p>
            </div>
        </body>
        </html>
        """;

    private static readonly string PasswordResetTemplate = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
        """ + BaseStyle + """
        </head>
        <body>
            <div class="header">
                <h1>🔬 {{ appname }}</h1>
            </div>
            <div class="content">
                <h2>Reset Your Password</h2>
                <p>Hello {{ username }},</p>
                <p>We received a request to reset your password. Click the button below to create a new password:</p>
                <p style="text-align: center;">
                    <a href="{{ reseturl }}" class="button">Reset Password</a>
                </p>
                <p>Or copy and paste this link into your browser:</p>
                <p style="word-break: break-all; background: #e9ecef; padding: 10px; border-radius: 4px; font-size: 12px;">{{ reseturl }}</p>
                <p>This link will expire in {{ expirationminutes }} minutes for security reasons.</p>
                <div class="alert">
                    <strong>⚠️ Didn't request this?</strong>
                    <p style="margin: 5px 0 0 0;">If you didn't request a password reset, please ignore this email. Your password will remain unchanged.</p>
                </div>
            </div>
            <div class="footer">
                <p>&copy; {{ year }} {{ appname }} Management System</p>
                <p>This is an automated message. Please do not reply to this email.</p>
            </div>
        </body>
        </html>
        """;

    private static readonly string WelcomeTemplate = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
        """ + BaseStyle + """
        </head>
        <body>
            <div class="header">
                <h1>🔬 Welcome to {{ appname }}!</h1>
            </div>
            <div class="content">
                <h2>Your Account is Ready</h2>
                <p>Hello {{ username }},</p>
                <p>Welcome to {{ appname }} Management System! Your account has been successfully created and verified.</p>
                <p>With {{ appname }}, you can:</p>
                <ul>
                {{ for feature in features }}
                    <li>{{ feature }}</li>
                {{ end }}
                </ul>
                <p style="text-align: center;">
                    <a href="{{ loginurl }}" class="button">Go to Dashboard</a>
                </p>
                <p>If you have any questions, please don't hesitate to contact our support team.</p>
            </div>
            <div class="footer">
                <p>&copy; {{ year }} {{ appname }} Management System</p>
                <p>This is an automated message. Please do not reply to this email.</p>
            </div>
        </body>
        </html>
        """;

    private static readonly string SecurityAlertTemplate = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
        """ + BaseStyle + """
        </head>
        <body>
            <div class="header" style="background: linear-gradient(135deg, #dc3545, #c82333);">
                <h1>🔒 Security Alert</h1>
            </div>
            <div class="content">
                <h2>{{ alerttype }}</h2>
                <p>Hello {{ username }},</p>
                <div class="alert alert-danger">
                    <strong>⚠️ Security Notice</strong>
                    <p style="margin: 5px 0 0 0;">{{ alertmessage }}</p>
                </div>
                <p>This activity was detected at: <code>{{ timestamp | date.to_string '%Y-%m-%d %H:%M:%S' }} UTC</code></p>
                <p>If this was you, no action is needed.</p>
                <p><strong>If this wasn't you:</strong></p>
                <ul>
                    <li>Change your password immediately</li>
                    <li>Review your recent account activity</li>
                    <li>Contact support if you need assistance</li>
                </ul>
            </div>
            <div class="footer">
                <p>&copy; {{ year }} {{ appname }} Management System</p>
                <p>This is an automated security notification.</p>
            </div>
        </body>
        </html>
        """;

    #endregion
}

namespace Quater.Backend.Services;

/// <summary>
/// Static email templates for authentication flows
/// </summary>
public static class EmailTemplates
{
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

    public static string VerificationEmail(string userName, string verificationUrl) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            {{BaseStyle}}
        </head>
        <body>
            <div class="header">
                <h1>🔬 Quater Water Quality</h1>
            </div>
            <div class="content">
                <h2>Verify Your Email Address</h2>
                <p>Hello {{userName}},</p>
                <p>Thank you for registering with Quater Water Quality. Please click the button below to verify your email address:</p>
                <p style="text-align: center;">
                    <a href="{{verificationUrl}}" class="button">Verify Email</a>
                </p>
                <p>Or copy and paste this link into your browser:</p>
                <p style="word-break: break-all; background: #e9ecef; padding: 10px; border-radius: 4px; font-size: 12px;">{{verificationUrl}}</p>
                <p>This link will expire in 24 hours.</p>
                <p>If you didn't create an account, you can safely ignore this email.</p>
            </div>
            <div class="footer">
                <p>&copy; {{DateTime.UtcNow.Year}} Quater Water Quality Management System</p>
                <p>This is an automated message. Please do not reply to this email.</p>
            </div>
        </body>
        </html>
        """;

    public static string PasswordResetEmail(string userName, string resetUrl) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            {{BaseStyle}}
        </head>
        <body>
            <div class="header">
                <h1>🔬 Quater Water Quality</h1>
            </div>
            <div class="content">
                <h2>Reset Your Password</h2>
                <p>Hello {{userName}},</p>
                <p>We received a request to reset your password. Click the button below to create a new password:</p>
                <p style="text-align: center;">
                    <a href="{{resetUrl}}" class="button">Reset Password</a>
                </p>
                <p>Or copy and paste this link into your browser:</p>
                <p style="word-break: break-all; background: #e9ecef; padding: 10px; border-radius: 4px; font-size: 12px;">{{resetUrl}}</p>
                <p>This link will expire in 1 hour for security reasons.</p>
                <div class="alert">
                    <strong>⚠️ Didn't request this?</strong>
                    <p style="margin: 5px 0 0 0;">If you didn't request a password reset, please ignore this email. Your password will remain unchanged.</p>
                </div>
            </div>
            <div class="footer">
                <p>&copy; {{DateTime.UtcNow.Year}} Quater Water Quality Management System</p>
                <p>This is an automated message. Please do not reply to this email.</p>
            </div>
        </body>
        </html>
        """;

    public static string WelcomeEmail(string userName, string loginUrl) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            {{BaseStyle}}
        </head>
        <body>
            <div class="header">
                <h1>🔬 Welcome to Quater!</h1>
            </div>
            <div class="content">
                <h2>Your Account is Ready</h2>
                <p>Hello {{userName}},</p>
                <p>Welcome to Quater Water Quality Management System! Your account has been successfully created and verified.</p>
                <p>With Quater, you can:</p>
                <ul>
                    <li>📊 Track and analyze water quality samples</li>
                    <li>✅ Monitor compliance with WHO standards</li>
                    <li>📱 Access your data from any device</li>
                    <li>🔄 Sync data automatically across your team</li>
                </ul>
                <p style="text-align: center;">
                    <a href="{{loginUrl}}" class="button">Go to Dashboard</a>
                </p>
                <p>If you have any questions, please don't hesitate to contact our support team.</p>
            </div>
            <div class="footer">
                <p>&copy; {{DateTime.UtcNow.Year}} Quater Water Quality Management System</p>
                <p>This is an automated message. Please do not reply to this email.</p>
            </div>
        </body>
        </html>
        """;

    public static string SecurityAlertEmail(string userName, string alertType, string alertMessage) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            {{BaseStyle}}
        </head>
        <body>
            <div class="header" style="background: linear-gradient(135deg, #dc3545, #c82333);">
                <h1>🔒 Security Alert</h1>
            </div>
            <div class="content">
                <h2>{{alertType}}</h2>
                <p>Hello {{userName}},</p>
                <div class="alert alert-danger">
                    <strong>⚠️ Security Notice</strong>
                    <p style="margin: 5px 0 0 0;">{{alertMessage}}</p>
                </div>
                <p>This activity was detected at: <code>{{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}} UTC</code></p>
                <p>If this was you, no action is needed.</p>
                <p><strong>If this wasn't you:</strong></p>
                <ul>
                    <li>Change your password immediately</li>
                    <li>Review your recent account activity</li>
                    <li>Contact support if you need assistance</li>
                </ul>
            </div>
            <div class="footer">
                <p>&copy; {{DateTime.UtcNow.Year}} Quater Water Quality Management System</p>
                <p>This is an automated security notification.</p>
            </div>
        </body>
        </html>
        """;
}

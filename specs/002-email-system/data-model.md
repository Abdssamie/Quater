# Data Model: Email System

## 1. Security Tokens

**Decision**: No database table.
We utilize **ASP.NET Core Identity's** built-in token providers for generating and validating tokens. These tokens are stateless, relying on cryptographic signatures protected by the Data Protection API.

- **Email Confirmation Token**: generated via `UserManager.GenerateEmailConfirmationTokenAsync`.
- **Password Reset Token**: generated via `UserManager.GeneratePasswordResetTokenAsync`.

**Storage**:
- Tokens are **not** stored in the database.
- They are transmitted to the user via email and validated upon return.

---

## 2. In-Memory Queue Entities

These entities exist only in the application memory (RAM) within the `IEmailQueue` implementation.

### EmailQueueItem

Represents a single email job waiting to be processed by the background worker.

```csharp
namespace Quater.Backend.Core.Models.Email;

public sealed record EmailQueueItem(
    string RecipientEmail,
    string Subject,
    string TemplateName,
    object Model, // The dynamic data for the template
    int RetryCount = 0
);
```

---

## 3. Data Transfer Objects (DTOs)

### SendEmailDto
Used internally by services to request an email.

```csharp
public sealed record SendEmailDto(
    string To,
    string Subject,
    string TemplateName,
    object Model
);
```

### EmailTemplateModel
Base class or common structure for template data. Specific emails will extend this or use anonymous objects.

```csharp
public record EmailTemplateModel
{
    public string BaseUrl { get; init; } = string.Empty;
    public string CurrentYear { get; init; } = DateTime.UtcNow.Year.ToString();
    public string AppName { get; init; } = "Quater";
}

// Example: Welcome Email Model
public sealed record WelcomeEmailModel : EmailTemplateModel
{
    public required string Name { get; init; }
    public required string VerificationLink { get; init; }
}

// Example: Reset Password Model
public sealed record ResetPasswordModel : EmailTemplateModel
{
    public required string ResetLink { get; init; }
    public required string ValidFor { get; init; } // e.g., "1 hour"
}
```

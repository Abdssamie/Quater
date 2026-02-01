namespace Quater.Backend.Core.DTOs;

/// <summary>
/// DTO for sending an email
/// </summary>
public sealed record SendEmailDto
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public bool IsHtml { get; init; } = true;
    public string? ReplyTo { get; init; }
}

/// <summary>
/// Item for the email queue background processing
/// </summary>
public sealed record EmailQueueItem(
    SendEmailDto Email,
    int RetryCount = 0,
    DateTimeOffset? ScheduledAt = null);

/// <summary>
/// Result of an email send operation
/// </summary>
public sealed record EmailSendResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public string? MessageId { get; init; }

    public static EmailSendResult Success(string messageId) =>
        new() { IsSuccess = true, MessageId = messageId };

    public static EmailSendResult Failure(string error) =>
        new() { IsSuccess = false, Error = error };
}

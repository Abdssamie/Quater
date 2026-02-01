namespace Quater.Backend.Core.Interfaces;

using Quater.Backend.Core.DTOs;

/// <summary>
/// Email queue for background processing
/// </summary>
public interface IEmailQueue
{
    /// <summary>
    /// Queue an email for sending
    /// </summary>
    ValueTask QueueAsync(EmailQueueItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeue an email for processing
    /// </summary>
    ValueTask<EmailQueueItem> DequeueAsync(CancellationToken cancellationToken);
}

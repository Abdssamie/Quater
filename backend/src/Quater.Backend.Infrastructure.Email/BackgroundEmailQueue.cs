namespace Quater.Backend.Infrastructure.Email;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using System.Threading.Channels;

/// <summary>
/// In-memory email queue using System.Threading.Channels
/// </summary>
public sealed class BackgroundEmailQueue : IEmailQueue
{
    private readonly Channel<EmailQueueItem> _queue;

    public BackgroundEmailQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<EmailQueueItem>(options);
    }

    /// <summary>
    /// Gets the approximate number of items currently in the queue
    /// </summary>
    public int ApproximateCount => _queue.Reader.Count;

    public async ValueTask QueueAsync(EmailQueueItem item, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(item, cancellationToken);
    }

    public async ValueTask<EmailQueueItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}

/// <summary>
/// Background service that processes queued emails
/// </summary>
public sealed class EmailQueueProcessor(
    IEmailQueue emailQueue,
    IEmailSender emailSender,
    ILogger<EmailQueueProcessor> logger) : BackgroundService
{
    private const int MaxRetries = 3;
    private const int RetryDelaySeconds = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email queue processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await emailQueue.DequeueAsync(stoppingToken);
                await ProcessEmailAsync(item, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing email queue");
                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
            }
        }

        logger.LogInformation("Email queue processor stopped");
    }

    private async Task ProcessEmailAsync(EmailQueueItem item, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Processing email to {To} with subject '{Subject}'",
                item.Email.To, item.Email.Subject);

            var result = await emailSender.SendAsync(item.Email, cancellationToken);

            if (!result.IsSuccess)
            {
                if (item.RetryCount < MaxRetries)
                {
                    logger.LogWarning("Email send failed (attempt {Attempt}/{MaxRetries}): {Error}. Requeueing...",
                        item.RetryCount + 1, MaxRetries, result.Error);

                    // Requeue immediately, delay will happen in the background
                    var retryItem = item with { RetryCount = item.RetryCount + 1 };
                    
                    // Use fire-and-forget pattern to avoid blocking the processor
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds * retryItem.RetryCount), cancellationToken);
                            await emailQueue.QueueAsync(retryItem, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to requeue email to {To}", item.Email.To);
                        }
                    }, cancellationToken);
                }
                else
                {
                    logger.LogError(
                        "DEAD LETTER: Email failed after {MaxRetries} attempts. " +
                        "To: {To}, Subject: {Subject}, Body: {Body}, Error: {Error}",
                        MaxRetries, item.Email.To, item.Email.Subject, 
                        item.Email.Body, result.Error);
                }
            }
            else
            {
                logger.LogInformation("Email sent successfully to {To}. MessageId: {MessageId}",
                    item.Email.To, result.MessageId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing email to {To}", item.Email.To);
        }
    }
}

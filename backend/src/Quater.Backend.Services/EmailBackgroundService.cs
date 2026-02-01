namespace Quater.Backend.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

/// <summary>
/// Background service for processing queued emails
/// </summary>
public sealed class EmailBackgroundService : BackgroundService
{
    private readonly IEmailQueue _emailQueue;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailBackgroundService> _logger;

    private const int MaxRetries = 3;

    public EmailBackgroundService(
        IEmailQueue emailQueue,
        IEmailSender emailSender,
        ILogger<EmailBackgroundService> logger)
    {
        _emailQueue = emailQueue ?? throw new ArgumentNullException(nameof(emailQueue));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await _emailQueue.DequeueAsync(stoppingToken);
                await ProcessEmailAsync(item, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email from queue");

                // Small delay before retrying to prevent tight loop on persistent errors
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Email background service stopped");
    }

    private async Task ProcessEmailAsync(EmailQueueItem item, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Processing queued email to {To} (attempt {RetryCount})",
            item.Email.To, item.RetryCount + 1);

        var result = await _emailSender.SendAsync(item.Email, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Queued email to {To} sent successfully with MessageId {MessageId}",
                item.Email.To, result.MessageId);
        }
        else if (item.RetryCount < MaxRetries - 1)
        {
            var retryItem = item with { RetryCount = item.RetryCount + 1 };

            _logger.LogWarning(
                "Failed to send queued email to {To}, requeuing (retry {RetryCount}/{MaxRetries})",
                item.Email.To, retryItem.RetryCount, MaxRetries);

            await _emailQueue.QueueAsync(retryItem, cancellationToken);
        }
        else
        {
            _logger.LogError(
                "Failed to send queued email to {To} after {MaxRetries} attempts: {Error}",
                item.Email.To, MaxRetries, result.Error);
        }
    }
}

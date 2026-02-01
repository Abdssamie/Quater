namespace Quater.Backend.Services;

using System.Threading.Channels;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;

/// <summary>
/// In-memory email queue using System.Threading.Channels
/// </summary>
public sealed class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailQueueItem> _channel;

    public EmailQueue()
    {
        // Bounded channel to prevent memory issues
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<EmailQueueItem>(options);
    }

    public async ValueTask QueueAsync(EmailQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public async ValueTask<EmailQueueItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}

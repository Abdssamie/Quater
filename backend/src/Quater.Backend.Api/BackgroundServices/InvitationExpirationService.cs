using Microsoft.Extensions.DependencyInjection;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Api.BackgroundServices;

public sealed class InvitationExpirationService(
    IServiceProvider serviceProvider,
    ILogger<InvitationExpirationService> logger) : BackgroundService
{
    private static readonly TimeSpan SuccessDelay = TimeSpan.FromHours(24);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var invitationService = scope.ServiceProvider.GetRequiredService<IUserInvitationService>();

                await invitationService.ExpireOldInvitationsAsync(stoppingToken);

                logger.LogInformation("Invitation expiration job completed successfully.");

                await Task.Delay(SuccessDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Invitation expiration job failed.");

                try
                {
                    await Task.Delay(ErrorDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}

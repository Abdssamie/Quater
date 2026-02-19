using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace Quater.Desktop.Core.Dialogs;

public sealed class SukiDialogService(ILogger<SukiDialogService> logger) : IDialogService
{
    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        logger.LogInformation("Confirmation requested: {Title} - {Message}", title, message);
        return Task.FromResult(true);
    }

    public Task ShowAlertAsync(string title, string message)
    {
        logger.LogInformation("Alert: {Title} - {Message}", title, message);
        return Task.CompletedTask;
    }

    public Task<string?> ShowInputAsync(string title, string message, string defaultValue = "")
    {
        logger.LogInformation("Input requested: {Title} - {Message}", title, message);
        return Task.FromResult<string?>(defaultValue);
    }

    public void ShowToast(string message, NotificationType type = NotificationType.Information)
    {
        logger.LogInformation("[{Type}] {Message}", type, message);
    }

    public void ShowSuccess(string message) => ShowToast(message, NotificationType.Success);
    public void ShowWarning(string message) => ShowToast(message, NotificationType.Warning);
    public void ShowError(string message) => ShowToast(message, NotificationType.Error);
}

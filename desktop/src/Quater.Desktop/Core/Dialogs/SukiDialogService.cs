using Microsoft.Extensions.Logging;
using SukiUI.Toasts;

namespace Quater.Desktop.Core.Dialogs;

public sealed class SukiDialogService(
    ILogger<SukiDialogService> logger,
    ISukiToastManager toastManager) : IDialogService
{
    public Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        _ = confirmText;
        _ = cancelText;
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
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var toastType = type switch
        {
            NotificationType.Success => Avalonia.Controls.Notifications.NotificationType.Success,
            NotificationType.Warning => Avalonia.Controls.Notifications.NotificationType.Warning,
            NotificationType.Error => Avalonia.Controls.Notifications.NotificationType.Error,
            _ => Avalonia.Controls.Notifications.NotificationType.Information
        };

        toastManager.CreateToast()
            .WithContent(message)
            .OfType(toastType)
            .Queue();
        logger.LogInformation("[{Type}] {Message}", type, message);
    }

    public void ShowSuccess(string message) => ShowToast(message, NotificationType.Success);
    public void ShowWarning(string message) => ShowToast(message, NotificationType.Warning);
    public void ShowError(string message) => ShowToast(message, NotificationType.Error);
}

using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Quater.Desktop.Core.Dialogs;

public sealed class SukiDialogService(
    ILogger<SukiDialogService> logger,
    ISukiToastManager toastManager,
    ISukiDialogManager dialogManager) : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string confirmText = "OK",
        string cancelText = "Cancel")
    {
        logger.LogInformation("Showing confirmation dialog: {Title}", title);

        // TryShowAsync must be called on UI thread because it drives Avalonia's
        // dialog host which interacts with the visual tree.
        // Note: although InvokeAsync<Task<bool>> nominally returns DispatcherOperation<Task<bool>>,
        // Avalonia's DispatcherOperation awaiter automatically unwraps Task<T> results, so a
        // single await here correctly suspends until the dialog is dismissed — no .Unwrap() needed.
        return await Dispatcher.UIThread.InvokeAsync(() =>
            dialogManager
                .CreateDialog()
                .WithTitle(title)
                .WithContent(message)
                .WithYesNoResult(confirmText, cancelText)
                .TryShowAsync());
    }

    public async Task ShowAlertAsync(string title, string message)
    {
        logger.LogInformation("Showing alert dialog: {Title}", title);

        // Note: although InvokeAsync<Task<bool>> nominally returns DispatcherOperation<Task<bool>>,
        // Avalonia's DispatcherOperation awaiter automatically unwraps Task<T> results, so a
        // single await here correctly suspends until the dialog is dismissed — no .Unwrap() needed.
        await Dispatcher.UIThread.InvokeAsync(() =>
            dialogManager
                .CreateDialog()
                .WithTitle(title)
                .WithContent(message)
                .WithOkResult("OK")
                .TryShowAsync());
    }

    public async Task<string?> ShowInputAsync(string title, string message, string defaultValue = "")
    {
        logger.LogInformation("Showing input dialog: {Title}", title);

        var completion = new TaskCompletionSource<string?>();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var textBox = new TextBox
            {
                Text = defaultValue,
                Watermark = message,
                MinWidth = 300,
            };

            dialogManager
                .CreateDialog()
                .WithTitle(title)
                .WithContent(textBox)
                .WithActionButton("OK", _ => completion.TrySetResult(textBox.Text), dismissOnClick: true)
                .WithActionButton("Cancel", _ => completion.TrySetResult(null), dismissOnClick: true)
                .OnDismissed(_ => completion.TrySetResult(null))
                .TryShow();
        });

        return await completion.Task;
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

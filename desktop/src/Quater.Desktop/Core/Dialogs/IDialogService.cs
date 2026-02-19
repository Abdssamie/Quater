namespace Quater.Desktop.Core.Dialogs;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowAlertAsync(string title, string message);
    Task<string?> ShowInputAsync(string title, string message, string defaultValue = "");
    
    void ShowToast(string message, NotificationType type = NotificationType.Information);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}

public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}

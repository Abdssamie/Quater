namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains standard error messages used throughout the application.
/// Only includes messages that are actively used in the codebase.
/// </summary>
public static class ErrorMessages
{
    // General errors
    public const string ConcurrencyConflict = "The resource has been modified by another user. Please refresh and try again.";

    // Sample errors
    public const string SampleNotFound = "Sample not found.";

    // Test result errors
    public const string TestResultNotFound = "Test result not found.";

    // Parameter errors
    public const string ParameterNotFound = "Parameter not found.";
    public const string ParameterAlreadyExists = "A parameter with this name already exists.";

    // Lab errors
    public const string LabNotFound = "Lab not found.";
    public const string LabAlreadyExists = "A lab with this name already exists.";

    // User errors
    public const string UserNotFound = "User not found.";
    public const string UserCreationFailed = "Failed to create user.";
    public const string UserUpdateFailed = "Failed to update user.";
}


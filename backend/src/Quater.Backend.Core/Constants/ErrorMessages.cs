namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains standard error messages used throughout the application.
/// </summary>
public static class ErrorMessages
{
    // General errors
    public const string InternalServerError = "An internal server error occurred. Please try again later.";
    public const string UnauthorizedAccess = "You are not authorized to perform this action.";
    public const string InvalidRequest = "The request is invalid.";
    public const string ResourceNotFound = "The requested resource was not found.";
    public const string ConcurrencyConflict = "The resource has been modified by another user. Please refresh and try again.";

    // Authentication errors
    public const string InvalidCredentials = "Invalid username or password.";
    public const string AccountLocked = "Your account has been locked due to multiple failed login attempts. Please try again later.";
    public const string TokenExpired = "Your session has expired. Please log in again.";
    public const string InvalidToken = "Invalid authentication token.";

    // Validation errors
    public const string RequiredField = "This field is required.";
    public const string InvalidFormat = "The format is invalid.";
    public const string InvalidDateRange = "The end date must be after the start date.";
    public const string InvalidCoordinates = "Invalid GPS coordinates.";

    // Sample errors
    public const string SampleNotFound = "Sample not found.";
    public const string SampleAlreadyCompleted = "Cannot modify a completed sample.";
    public const string SampleAlreadyDeleted = "Sample has been deleted.";

    // Test result errors
    public const string TestResultNotFound = "Test result not found.";
    public const string TestResultAlreadyExists = "A test result for this parameter already exists.";
    public const string InvalidTestValue = "The test value is outside the valid range.";

    // Parameter errors
    public const string ParameterNotFound = "Parameter not found.";
    public const string ParameterAlreadyExists = "A parameter with this name already exists.";
    public const string ParameterInUse = "Cannot delete parameter because it is referenced by test results.";

    // Lab errors
    public const string LabNotFound = "Lab not found.";
    public const string LabAlreadyExists = "A lab with this name already exists.";
    public const string LabInUse = "Cannot delete lab because it has associated samples.";

    // User errors
    public const string UserNotFound = "User not found.";
    public const string UserAlreadyExists = "A user with this email already exists.";
    public const string InvalidRole = "Invalid user role.";
    public const string UserCreationFailed = "Failed to create user.";
    public const string UserUpdateFailed = "Failed to update user.";

    // Sync errors
    public const string SyncFailed = "Synchronization failed. Please try again.";
    public const string SyncConflict = "A synchronization conflict occurred.";
    public const string SyncInProgress = "A synchronization is already in progress.";
    public const string NoChangesToSync = "No changes to synchronize.";
    public const string SyncLogNotFound = "Sync log not found.";
    public const string ConflictBackupNotFound = "Conflict backup not found.";
    public const string ManualResolutionRequired = "Manual conflict resolution requires user intervention.";

    // Import/Export errors
    public const string ImportFailed = "Import failed. Please check the file format and try again.";
    public const string ExportFailed = "Export failed. Please try again.";
    public const string InvalidFileFormat = "Invalid file format. Please upload a valid CSV or Excel file.";
    public const string FileEmpty = "The uploaded file is empty.";
    public const string FileTooLarge = "The uploaded file is too large.";

    // Report errors
    public const string ReportGenerationFailed = "Report generation failed. Please try again.";
    public const string NoDataForReport = "No data available for the selected criteria.";
}

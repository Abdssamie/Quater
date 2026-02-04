namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains application-wide constants.
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Pagination constants.
    /// </summary>
    public static class Pagination
    {
        public const int DefaultPageSize = 50;
        public const int MaxPageSize = 100;
        public const int MinPageSize = 1;
        public const int DefaultPageNumber = 1;
    }

    /// <summary>
    /// Rate limiting constants.
    /// </summary>
    public static class RateLimiting
    {
        public const int MaxFailedLoginAttempts = 5;
        public const int LockoutDurationMinutes = 15;
        public const int ApiRequestsPerMinute = 60;
        public const int BulkOperationRequestsPerHour = 10;
    }

    /// <summary>
    /// File upload constants.
    /// </summary>
    public static class FileUpload
    {
        public const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        public static readonly string[] AllowedFileExtensions = { ".csv", ".xlsx", ".xls" };
        public const string AllowedMimeTypes = "text/csv,application/vnd.ms-excel,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }

    /// <summary>
    /// Synchronization constants.
    /// </summary>
    public static class Sync
    {
        public const int MaxBatchSize = 100;
        public const int SyncTimeoutSeconds = 300; // 5 minutes
        public const int MaxRetryAttempts = 3;
        public const int RetryDelayMilliseconds = 1000;
    }

    /// <summary>
    /// Audit log constants.
    /// </summary>
    public static class AuditLog
    {
        public const int ArchiveAfterDays = 90;
        public const int RetentionDays = 365;
        public const int BatchSize = 1000;
    }

    /// <summary>
    /// Cache constants.
    /// </summary>
    public static class Cache
    {
        public const int DefaultExpirationMinutes = 30;
        public const int ParametersCacheExpirationMinutes = 60;
        public const int LabsCacheExpirationMinutes = 60;
        public const string ParametersCacheKey = "parameters:all";
        public const string LabsCacheKey = "labs:all";
    }

    /// <summary>
    /// Validation constants.
    /// </summary>
    public static class Validation
    {
        public const int MaxNameLength = 100;
        public const int MaxDescriptionLength = 1000;
        public const int MaxNotesLength = 2000;
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 100;
        public const double MinLatitude = -90.0;
        public const double MaxLatitude = 90.0;
        public const double MinLongitude = -180.0;
        public const double MaxLongitude = 180.0;
    }

    /// <summary>
    /// Report constants.
    /// </summary>
    public static class Reports
    {
        public const string DefaultDateFormat = "yyyy-MM-dd";
        public const string DefaultTimeFormat = "HH:mm:ss";
        public const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const int MaxReportRows = 10000;
    }

    /// <summary>
    /// API versioning constants.
    /// </summary>
    public static class ApiVersioning
    {
        public const string CurrentVersion = "1.0";
        public const string HeaderName = "api-version";
        public const string QueryParameterName = "api-version";
    }

    /// <summary>
    /// Security constants.
    /// </summary>
    public static class Security
    {
        public const int TokenExpirationMinutes = 60;
        public const int RefreshTokenExpirationDays = 7;
        public const int MaxConcurrentSessions = 5;
        public const string DefaultCorsPolicy = "DefaultCorsPolicy";
    }
}

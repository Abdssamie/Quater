using Quater.Desktop.Api.Client;

namespace Quater.Desktop.Core.Api;

public sealed class ApiErrorFormatter : IApiErrorFormatter
{
    public string Format(ApiException exception, string action)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.ErrorCode is 401 or 403)
        {
            return $"You do not have permission to {action}.";
        }

        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            return $"Failed to {action}: {exception.Message}";
        }

        return $"Failed to {action}.";
    }
}

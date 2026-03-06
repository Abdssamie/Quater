using Quater.Desktop.Api.Client;

namespace Quater.Desktop.Core.Api;

public sealed class ApiErrorFormatter : IApiErrorFormatter
{
    public string Format(ApiException exception, string fallbackMessage)
    {
        return exception.ErrorCode switch
        {
            401 => "Session expired",
            403 => "Permission denied",
            400 => "Invalid request",
            _ => fallbackMessage
        };
    }
}

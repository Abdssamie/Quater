using System;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace Quater.Desktop.Api.Client;

public sealed class ApiUnauthorizedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiUnauthorizedEventArgs"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by the API.</param>
    public ApiUnauthorizedEventArgs(int statusCode)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code returned by the API.
    /// </summary>
    public int StatusCode { get; }
}

public partial class ApiClient
{
    /// <summary>
    /// Raised when an unauthorized response is detected.
    /// </summary>
    public static event EventHandler<ApiUnauthorizedEventArgs>? UnauthorizedResponse;
    /// <summary>
    /// Optional handler invoked when an unauthorized response is detected.
    /// </summary>
    public static Func<int, Task>? UnauthorizedResponseHandler { get; set; }
    private static int _unauthorizedSignaled;

    partial void InterceptResponse(RestRequest request, RestResponse response)
    {
        if (response == null)
        {
            return;
        }

        var statusCode = (int)response.StatusCode;
        if (statusCode != 401 && statusCode != 403)
        {
            return;
        }

        if (Interlocked.Exchange(ref _unauthorizedSignaled, 1) == 1)
        {
            return;
        }

        UnauthorizedResponse?.Invoke(null, new ApiUnauthorizedEventArgs(statusCode));

        var handler = UnauthorizedResponseHandler;
        if (handler != null)
        {
            _ = handler(statusCode);
        }
    }

    /// <summary>
    /// Clears the unauthorized response signal gate.
    /// </summary>
    public static void ResetUnauthorizedSignal()
    {
        Interlocked.Exchange(ref _unauthorizedSignaled, 0);
    }
}

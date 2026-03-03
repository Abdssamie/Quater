using System;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace Quater.Desktop.Api.Client;

/// <summary>
/// Event arguments for an unauthorized (401/403) API response.
/// </summary>
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
    private const string AuthorizationHeader = "Authorization";
    private const string BearerPrefix = "Bearer";
    private const string LabIdHeader = "X-Lab-Id";

    /// <summary>
    /// Provides access tokens for API requests.
    /// </summary>
    public static Func<CancellationToken, Task<string?>>? AccessTokenProvider { get; set; }

    /// <summary>
    /// Provides the current lab ID.
    /// </summary>
    public static Func<Guid?>? LabIdProvider { get; set; }

    /// <summary>
    /// Raised when an unauthorized response is detected.
    /// </summary>
    public static event EventHandler<ApiUnauthorizedEventArgs>? UnauthorizedResponse;

    /// <summary>
    /// Optional handler invoked when an unauthorized response is detected.
    /// </summary>
    public static Func<int, Task>? UnauthorizedResponseHandler { get; set; }

    private static int _unauthorizedSignaled;

    partial void InterceptRequest(RestRequest request)
    {
        try
        {
            var tokenProvider = AccessTokenProvider;
            if (tokenProvider is not null)
            {
                // Run on the thread pool to avoid deadlock when called from the UI thread.
                // A direct .GetAwaiter().GetResult() on the UI thread (Avalonia dispatcher
                // SynchronizationContext) can deadlock when the token provider triggers an
                // async refresh whose continuation tries to resume on the same UI thread.
                // Task.Run schedules the work on the thread pool where there is no
                // SynchronizationContext, so continuations are free to run on any thread.
                var token = Task.Run(() => tokenProvider(CancellationToken.None)).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.AddOrUpdateHeader(AuthorizationHeader, $"{BearerPrefix} {token}");
                }
            }

            var labIdProvider = LabIdProvider;
            var labId = labIdProvider?.Invoke();
            if (labId.HasValue && labId.Value != Guid.Empty)
            {
                request.AddOrUpdateHeader(LabIdHeader, labId.Value.ToString());
            }
        }
        catch (Exception)
        {
            // Swallow to avoid breaking the request pipeline; auth failures will be
            // surfaced by the 401/403 response handling in InterceptResponse.
        }
    }

    partial void InterceptResponse(RestRequest request, RestResponse response)
    {
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
using System;
using System.Threading;
using System.Diagnostics;
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
                var token = tokenProvider(CancellationToken.None).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine("ApiClient adding Authorization header");
                    request.AddOrUpdateHeader(AuthorizationHeader, $"{BearerPrefix} {token}");
                }
                else
                {
                    Console.WriteLine("ApiClient missing access token for Authorization header");
                }
            }
            else
            {
                Console.WriteLine("ApiClient AccessTokenProvider is not configured");
            }

            var labIdProvider = LabIdProvider;
            var labId = labIdProvider?.Invoke();
            if (labId.HasValue && labId.Value != Guid.Empty)
            {
                Console.WriteLine($"ApiClient adding X-Lab-Id header: {labId}");
                request.AddOrUpdateHeader(LabIdHeader, labId.Value.ToString());
            }
            else
            {
                Console.WriteLine("ApiClient no lab id available for X-Lab-Id header");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ApiClient.InterceptRequest failed: {ex}");
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
using System;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace Quater.Desktop.Api.Client;

public partial class ApiClient
{
    /// <summary>
    /// Provides bearer token for API requests.
    /// </summary>
    public static Func<CancellationToken, Task<string?>>? AccessTokenProvider { get; set; }
    /// <summary>
    /// Provides current lab id for API requests.
    /// </summary>
    public static Func<Guid?>? LabIdProvider { get; set; }

    partial void InterceptRequest(RestRequest request)
    {
        var tokenProvider = AccessTokenProvider;
        if (tokenProvider != null)
        {
            var token = tokenProvider(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.AddOrUpdateHeader("Authorization", $"Bearer {token}");
            }
        }

        var labProvider = LabIdProvider;
        if (labProvider != null)
        {
            var labId = labProvider();
            if (labId.HasValue && labId.Value != Guid.Empty)
            {
                request.AddOrUpdateHeader("X-Lab-Id", labId.Value.ToString());
            }
        }
    }
}

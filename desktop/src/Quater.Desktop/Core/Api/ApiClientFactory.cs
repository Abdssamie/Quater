using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Core.Api;

public sealed class ApiClientFactory : IApiClientFactory
{
    private readonly IAccessTokenCache _tokenCache;

    public ApiClientFactory(IAccessTokenCache tokenCache, AppState appState)
    {
        _tokenCache = tokenCache;

        ApiClient.AccessTokenProvider = async ct =>
        {
            var token = tokenCache.CurrentToken;
            return string.IsNullOrEmpty(token) ? await RefreshAsync(ct) : token;
        };
        ApiClient.LabIdProvider = () => appState.CurrentLabId == Guid.Empty ? null : appState.CurrentLabId;
    }

    private async Task<string?> RefreshAsync(CancellationToken ct)
    {
        await _tokenCache.RefreshAsync(ct);
        return _tokenCache.CurrentToken;
    }

    public IAuthApi GetAuthApi() => new AuthApi();
    public IUsersApi GetUsersApi() => new UsersApi();
    public ISamplesApi GetSamplesApi() => new SamplesApi();
    public ILabsApi GetLabsApi() => new LabsApi();
    public IParametersApi GetParametersApi() => new ParametersApi();
    public ITestResultsApi GetTestResultsApi() => new TestResultsApi();
    public IAuditLogsApi GetAuditLogsApi() => new AuditLogsApi();
    public IHealthApi GetHealthApi() => new HealthApi();
    public IEmailVerificationApi GetEmailVerificationApi() => new EmailVerificationApi();
    public IAuthorizationApi GetAuthorizationApi() => new AuthorizationApi();
    public IPasswordApi GetPasswordApi() => new PasswordApi();
    public IVersionApi GetVersionApi() => new VersionApi();
}

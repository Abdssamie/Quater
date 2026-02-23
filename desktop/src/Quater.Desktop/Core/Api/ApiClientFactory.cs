using Quater.Desktop.Api.Api;

namespace Quater.Desktop.Core.Api;

public sealed class ApiClientFactory : IApiClientFactory
{
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

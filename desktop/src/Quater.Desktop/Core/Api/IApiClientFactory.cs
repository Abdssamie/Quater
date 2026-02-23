using Quater.Desktop.Api.Api;

namespace Quater.Desktop.Core.Api;

public interface IApiClientFactory
{
    IAuthApi GetAuthApi();
    IUsersApi GetUsersApi();
    ISamplesApi GetSamplesApi();
    ILabsApi GetLabsApi();
    IParametersApi GetParametersApi();
    ITestResultsApi GetTestResultsApi();
    IAuditLogsApi GetAuditLogsApi();
    IHealthApi GetHealthApi();
    IEmailVerificationApi GetEmailVerificationApi();
    IAuthorizationApi GetAuthorizationApi();
    IPasswordApi GetPasswordApi();
    IVersionApi GetVersionApi();
}

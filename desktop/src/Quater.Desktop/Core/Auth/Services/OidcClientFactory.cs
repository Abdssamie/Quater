using Duende.IdentityModel.OidcClient;
using Quater.Desktop.Core.Auth.Browser;
using Quater.Desktop.Core.Settings;

namespace Quater.Desktop.Core.Auth.Services;

public sealed class OidcClientFactory(AppSettings appSettings)
{
    public OidcClient Create()
    {
        var redirectUrl = "http://127.0.0.1:7890/callback";
        var browser = new LoopbackBrowser(redirectUrl);

        var options = new OidcClientOptions
        {
            Authority = appSettings.BackendUrl,
            ClientId = appSettings.ClientId,
            RedirectUri = redirectUrl,
            Scope = "openid profile email api offline_access",
            Browser = browser,
            IdentityTokenValidator = new JwtIdentityTokenValidator(),
            Policy = new Policy
            {
                RequireIdentityTokenSignature = true
            }
        };

        return new OidcClient(options);
    }
}

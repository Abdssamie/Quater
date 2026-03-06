using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Results;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Quater.Desktop.Core.Auth.Services;

public sealed class JwtIdentityTokenValidator : IIdentityTokenValidator
{
    public async Task<IdentityTokenValidationResult> ValidateAsync(
        string identityToken,
        OidcClientOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityToken);
        ArgumentNullException.ThrowIfNull(options);

        var providerInfo = options.ProviderInformation;
        if (providerInfo is null)
        {
            providerInfo = await LoadProviderInformationAsync(options, cancellationToken);
            options.ProviderInformation = providerInfo;
        }

        if (providerInfo.KeySet?.Keys is null || providerInfo.KeySet.Keys.Count == 0)
        {
            return new IdentityTokenValidationResult
            {
                Error = "invalid_identity_token",
                ErrorDescription = "No signing keys available from OIDC provider."
            };
        }

        if (string.IsNullOrWhiteSpace(providerInfo.KeySet.RawData))
        {
            return new IdentityTokenValidationResult
            {
                Error = "invalid_identity_token",
                ErrorDescription = "OIDC provider key set is empty."
            };
        }

        var tokenHandler = new JsonWebTokenHandler();
        var signingKeys = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(providerInfo.KeySet.RawData).GetSigningKeys();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateIssuer = options.Policy.ValidateTokenIssuerName,
            ValidIssuer = providerInfo.IssuerName,
            ValidateAudience = true,
            ValidAudience = options.ClientId,
            ValidateLifetime = true,
            ClockSkew = options.ClockSkew
        };

        var validationResult = await tokenHandler.ValidateTokenAsync(identityToken, validationParameters);
        if (!validationResult.IsValid)
        {
            return new IdentityTokenValidationResult
            {
                Error = "invalid_identity_token",
                ErrorDescription = validationResult.Exception?.Message ?? "Identity token validation failed."
            };
        }

        var jsonWebToken = new JsonWebToken(identityToken);
        return new IdentityTokenValidationResult
        {
            User = new System.Security.Claims.ClaimsPrincipal(validationResult.ClaimsIdentity),
            SignatureAlgorithm = jsonWebToken.Alg
        };
    }

    private static async Task<ProviderInformation> LoadProviderInformationAsync(
        OidcClientOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            throw new InvalidOperationException("OIDC authority is required for discovery when provider info is not preconfigured.");
        }

        using var httpClient = options.HttpClientFactory?.Invoke(options) ?? new HttpClient();
        var metadataAddress = options.Authority.TrimEnd('/') + "/.well-known/openid-configuration";
        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(httpClient)
            {
                RequireHttps = metadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            });

        var configuration = await configManager.GetConfigurationAsync(cancellationToken);
        return new ProviderInformation
        {
            IssuerName = configuration.Issuer,
            KeySet = new Duende.IdentityModel.Jwk.JsonWebKeySet(configuration.JsonWebKeySet.ToString()),
            AuthorizeEndpoint = configuration.AuthorizationEndpoint,
            TokenEndpoint = configuration.TokenEndpoint,
            EndSessionEndpoint = configuration.EndSessionEndpoint,
            UserInfoEndpoint = configuration.UserInfoEndpoint,
            PushedAuthorizationRequestEndpoint = configuration.PushedAuthorizationRequestEndpoint
        };
    }
}

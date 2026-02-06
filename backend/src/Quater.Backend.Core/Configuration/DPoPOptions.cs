/*
 * @id: dpop-options
 * @priority: medium
 * @progress: 0
 * @directive: Create DPoP configuration options record that binds to appsettings.json "OpenIddict:DPoP" section. Include Enabled (bool), RequireNonce (bool), NonceLifetimeSeconds (int, default 300), AllowedClockSkewSeconds (int, default 60), RequiredForPublicClients (bool, default false for migration period). Register as IOptions<DPoPOptions> in DI. Use record type for immutability.
 * @context: specs/oauth2-mobile-desktop-security-enhancement.md#6-5-configuration-changes
 * @checklist: [
 *   "DPoPOptions record with all config properties (FR-05)",
 *   "Default values match spec (NonceLifetime=300, ClockSkew=60)",
 *   "Binds to OpenIddict:DPoP config section",
 *   "Registered in DI via IOptions<DPoPOptions>",
 *   "RequiredForPublicClients defaults to false for migration period (SC-05)",
 *   "Validation: NonceLifetimeSeconds > 0, AllowedClockSkewSeconds >= 0"
 * ]
 * @deps: []
 * @skills: ["aspnetcore-options-pattern"]
 */

namespace Quater.Backend.Core.Configuration;

public static class _DPoPOptionsHole
{
    public const string Hole = "placeholder";
}

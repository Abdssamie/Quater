namespace Quater.Backend.Api.Middleware;

// TODO: CRITICAL - Implement DPoP (Demonstration of Proof-of-Possession) middleware per RFC 9449.
// Currently this is a placeholder stub with no token binding protection.
// Risk: Stolen access tokens can be used without proof of possession.
// Must validate DPoP proof header, verify jkt claim matches proof jwk thumbprint,
// validate htm/htu/ath claims, and return 401 with WWW-Authenticate: DPoP for missing proofs.

/// <summary>
/// Placeholder for DPoP middleware implementation.
/// </summary>
public static class _DPoPMiddlewareHole
{
    /// <summary>
    /// Placeholder constant.
    /// </summary>
    public const string Hole = "placeholder";
}

namespace Quater.Backend.Api.Middleware;

// NOT YET IMPLEMENTED — see P1-03 in docs/plans/2026-03-03-architecture-review-findings.md
//
// DPoP (RFC 9449) proof-of-possession has NOT been implemented.
// This file is retained as a stub for future implementation.
// Tokens are currently unbound bearer tokens with no replay protection.
//
// When implementing:
//   - Validate DPoP proof header, verify jkt claim matches proof jwk thumbprint
//   - Validate htm/htu/ath claims
//   - Return 401 with WWW-Authenticate: DPoP for missing proofs
//   - Register via app.UseMiddleware<DPoPMiddleware>() in Startup.Configure (currently absent)

/// <summary>
/// Placeholder for DPoP middleware implementation. NOT YET IMPLEMENTED — see P1-03.
/// </summary>
public static class _DPoPMiddlewareHole
{
    /// <summary>
    /// Placeholder constant.
    /// </summary>
    public const string Hole = "placeholder";
}

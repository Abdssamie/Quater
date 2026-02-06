/*
 * @id: dpop-proof-validator-interface
 * @priority: high
 * @progress: 0
 * @directive: Define IDPoPProofValidator interface with ValidateAsync method. Accept DPoP proof string, HTTP method, request URI, and optional access token hash. Return DPoPValidationResult record containing IsValid, ErrorCode, ErrorDescription, and JwkThumbprint (for token binding). Define DPoPValidationResult as a record in the same file.
 * @context: specs/oauth2-mobile-desktop-security-enhancement.md#fr-05-implement-dpop-rfc-9449
 * @checklist: [
 *   "IDPoPProofValidator interface with ValidateAsync method",
 *   "DPoPValidationResult record with IsValid, ErrorCode, ErrorDescription, JwkThumbprint",
 *   "Method accepts dpopProof, httpMethod, requestUri, accessTokenHash parameters",
 *   "CancellationToken parameter (project convention)",
 *   "XML documentation comments (project convention)",
 *   "Located in Core/Interfaces (clean architecture)"
 * ]
 * @deps: []
 * @skills: ["interface-design", "clean-architecture"]
 */

namespace Quater.Backend.Core.Interfaces;

public static class _IDPoPProofValidatorHole
{
    public const string Hole = "placeholder";
}

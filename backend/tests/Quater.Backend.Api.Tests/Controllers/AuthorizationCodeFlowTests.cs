/*
 * @id: authcode-flow-tests
 * @priority: high
 * @progress: 0
 * @directive: Implement comprehensive integration tests for the authorization code flow with PKCE. Test full flow: authorize -> token exchange. Test PKCE validation (valid S256, invalid verifier, missing challenge). Test authorization code lifecycle (single-use, expiration, replay prevention). Test redirect URI validation (mismatch, missing). Test public client behavior (no secret required, secret ignored). Test error responses for invalid requests. Use ApiTestFixture with Testcontainers. Follow existing test patterns from AuthControllerTests.
 * @context: specs/oauth2-mobile-desktop-security-enhancement.md#9-testing-strategy
 * @checklist: [
 *   "Full authorization code flow test: authorize -> token exchange (9.2)",
 *   "PKCE S256 validation: valid code_verifier matches code_challenge (EC-01)",
 *   "PKCE validation failure: mismatched code_verifier returns 400 (EC-01)",
 *   "PKCE required: missing code_challenge returns error (FR-03)",
 *   "Authorization code single-use: second exchange returns 400 (EC-02)",
 *   "Authorization code expiration: expired code returns 400 (EC-07)",
 *   "Redirect URI mismatch: different URI in token request returns 400 (EC-06)",
 *   "Public client without secret: token exchange succeeds (EC-08)",
 *   "Public client with secret: secret ignored, exchange succeeds (EC-08)",
 *   "Invalid client_id: authorize returns error (FR-08)",
 *   "Unauthenticated user: redirected to login (FR-08)",
 *   "State parameter preserved through flow (9.3 CSRF)",
 *   "Uses [Collection('PostgreSQL')] and IAsyncLifetime (project convention)",
 *   "Test naming: MethodName_Scenario_ExpectedResult (project convention)",
 *   "Uses FluentAssertions (project convention)"
 * ]
 * @deps: ["authorization-controller", "openiddict-seeder-public", "openiddict-config-authcode"]
 * @skills: ["xunit-integration-testing", "testcontainers", "oauth2-pkce-testing"]
 */

namespace Quater.Backend.Api.Tests.Controllers;

public static class _AuthorizationCodeFlowTestsHole
{
    public const string Hole = "placeholder";
}

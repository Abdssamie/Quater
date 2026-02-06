# Feature: OAuth 2.0 Security Enhancement for Mobile & Desktop Applications

## 1. Context

### Goal
Implement industry-standard OAuth 2.0 security best practices for mobile and desktop applications, replacing the current password grant flow with Authorization Code Flow + PKCE and adding DPoP (Demonstrating Proof of Possession) for token binding.

### User Value
- **Enhanced Security**: Prevents token theft and replay attacks through cryptographic proof of possession
- **Industry Compliance**: Aligns with OAuth 2.0 Security Best Current Practice (RFC 8252, RFC 7636, RFC 9449)
- **Future-Proof Architecture**: Supports modern authentication patterns for native applications
- **Better User Experience**: Enables secure single sign-on and token refresh without re-authentication

### Current State Analysis

**Existing Implementation:**
- ✅ OpenIddict 5.8.0 configured with password grant flow
- ✅ Token encryption enabled (access tokens are encrypted)
- ✅ Refresh token rotation enabled
- ❌ Using password grant (deprecated for native apps per OAuth 2.0 Security BCP)
- ❌ No PKCE implementation (line 210: `RequireProofKeyForCodeExchange()` incorrectly applied to password flow)
- ❌ No authorization code flow endpoints
- ❌ No DPoP support for sender-constrained tokens
- ❌ Client authentication required (incompatible with public clients like mobile apps)

**Critical Issues Identified:**
1. **Wrong Grant Type**: Password grant is deprecated for native apps (OAuth 2.0 Security BCP Section 2.4)
2. **PKCE Misconfiguration**: `RequireProofKeyForCodeExchange()` only applies to authorization code flow, not password flow
3. **Public Client Incompatibility**: Removed `AcceptAnonymousClients()` breaks mobile app authentication (can't store secrets)
4. **Missing Authorization Endpoint**: No `/authorize` endpoint for authorization code flow

---

## 2. User Stories (Prioritized)

### P0: Critical Security Fixes
- **US-01**: As a **security engineer**, I want to **remove the password grant flow** so that **user credentials are never exposed to the client application**
- **US-02**: As a **mobile app developer**, I want to **use authorization code flow with PKCE** so that **my public client can authenticate securely without storing secrets**

### P1: Authorization Code Flow Implementation
- **US-03**: As a **mobile app user**, I want to **authenticate via system browser** so that **my credentials are protected by the OS security sandbox**
- **US-04**: As a **desktop app user**, I want to **use OAuth 2.0 authorization code flow** so that **I can authenticate without entering credentials in the app**
- **US-05**: As a **backend developer**, I want to **implement the authorization endpoint** so that **clients can initiate the OAuth flow**

### P2: PKCE Implementation
- **US-06**: As a **mobile app**, I want to **generate PKCE code challenge** so that **authorization codes cannot be intercepted**
- **US-07**: As an **authorization server**, I want to **validate PKCE code verifier** so that **only the original client can exchange the authorization code**

### P3: DPoP Token Binding
- **US-08**: As a **security engineer**, I want to **implement DPoP proof-of-possession** so that **stolen access tokens cannot be used by attackers**
- **US-09**: As a **mobile app**, I want to **generate DPoP proofs** so that **my tokens are cryptographically bound to my key pair**
- **US-10**: As a **resource server**, I want to **validate DPoP proofs** so that **only legitimate token holders can access protected resources**

### P4: Testing & Documentation
- **US-11**: As a **QA engineer**, I want to **test authorization code flow with PKCE** so that **I can verify the implementation is secure**
- **US-12**: As a **mobile app developer**, I want to **see integration examples** so that **I can implement the client-side flow correctly**

---

## 3. Functional Requirements (Testable)

### FR-01: Remove Password Grant Flow
**MUST**: Remove `options.AllowPasswordFlow()` from OpenIddict configuration
- **Rationale**: Password grant is deprecated for native apps (OAuth 2.0 Security BCP)
- **Impact**: Breaking change - existing clients must migrate to authorization code flow
- **Migration Path**: Provide 30-day deprecation notice with migration guide

### FR-02: Implement Authorization Code Flow
**MUST**: Add authorization endpoint at `/api/auth/authorize`
- **Endpoint**: `GET/POST /api/auth/authorize`
- **Parameters**: `response_type=code`, `client_id`, `redirect_uri`, `scope`, `state`, `code_challenge`, `code_challenge_method`
- **Response**: Redirect to `redirect_uri` with `code` and `state` parameters
- **Authentication**: User must be authenticated (via ASP.NET Core Identity)
- **Consent**: Implicit consent (no consent screen for first-party apps)

### FR-03: Implement PKCE (RFC 7636)
**MUST**: Require PKCE for all authorization code requests
- **Code Challenge Method**: Support `S256` (SHA-256) - REQUIRED
- **Code Challenge Method**: Support `plain` - OPTIONAL (for legacy clients)
- **Validation**: Verify `code_verifier` matches `code_challenge` on token exchange
- **Storage**: Store `code_challenge` with authorization code (in-memory or database)
- **Expiration**: Authorization codes expire after 10 minutes

### FR-04: Configure Public Client Support
**MUST**: Allow anonymous clients (public clients without client_secret)
- **Add**: `options.AcceptAnonymousClients()` to OpenIddict configuration
- **Remove**: `options.RequireProofKeyForCodeExchange()` (incorrect usage)
- **Add**: Per-client PKCE requirement in `OpenIddictSeeder.cs`
- **Client Type**: Set `ClientType = OpenIddictConstants.ClientTypes.Public` for mobile/desktop apps

### FR-05: Implement DPoP (RFC 9449) - Phase 1
**MUST**: Add DPoP proof validation for access tokens
- **Header**: Accept `DPoP` HTTP header with JWT proof
- **Validation**: Verify DPoP proof signature, `htm`, `htu`, `ath` claims
- **Binding**: Bind access tokens to DPoP public key (`jkt` claim in token)
- **Token Type**: Return `token_type=DPoP` in token response
- **Nonce**: Support optional server-provided nonce for replay protection

### FR-06: Update Token Endpoint
**MUST**: Modify `/api/auth/token` to support authorization code grant
- **Grant Type**: `authorization_code` (new)
- **Grant Type**: `refresh_token` (existing, keep)
- **Grant Type**: `password` (remove after deprecation period)
- **Parameters**: `code`, `redirect_uri`, `code_verifier`, `client_id`
- **DPoP**: Accept optional `DPoP` header for DPoP-bound tokens

### FR-07: Update OpenIddictSeeder
**MUST**: Seed public client application with correct configuration
- **Client ID**: `quater-mobile-client`
- **Client Type**: `Public` (no client_secret)
- **Redirect URIs**: Configure for mobile/desktop deep links
- **Permissions**: `Endpoints.Authorization`, `Endpoints.Token`, `GrantTypes.AuthorizationCode`, `GrantTypes.RefreshToken`
- **Requirements**: `Requirements.Features.ProofKeyForCodeExchange`

### FR-08: Implement Authorization Endpoint Controller
**MUST**: Create `AuthorizationController` action for `/api/auth/authorize`
- **Authentication**: Require authenticated user (redirect to login if not)
- **Validation**: Validate `client_id`, `redirect_uri`, `scope`, `code_challenge`
- **Authorization Code**: Generate and store authorization code
- **Redirect**: Redirect to `redirect_uri` with `code` and `state`
- **Error Handling**: Return OAuth 2.0 error responses per RFC 6749

### FR-09: DPoP Proof Generation (Client-Side Guidance)
**SHOULD**: Provide client-side implementation guidance
- **Key Generation**: Use ES256 (ECDSA P-256) or RS256 (RSA 2048-bit)
- **Proof Structure**: JWT with `typ=dpop+jwt`, `alg`, `jwk` header
- **Claims**: `jti`, `htm`, `htu`, `iat`, `ath` (for resource access)
- **Storage**: Store private key in secure storage (Keychain/Keystore)

### FR-10: Resource Server DPoP Validation
**MUST**: Validate DPoP proofs on protected resource access
- **Header**: Require `DPoP` header for DPoP-bound tokens
- **Validation**: Verify proof signature, claims, and token binding
- **Authorization**: Use `DPoP` authentication scheme (not `Bearer`)
- **Error Response**: Return `401 Unauthorized` with `WWW-Autticate: DPoP` header

---

## 4. Success Criteria (Measurable)

### SC-01: Security Compliance
- ✅ **PASS**: OAuth 2.0 Security BCP compliance (RFC 8252, RFC 8628)
- ✅ **PASS**: PKCE implementation passes RFC 7636 test vectors
- ✅ **PASS**: DPoP implementation passes RFC 9449 test vectors
- ✅ **PASS**: No password grant flow in production

### SC-02: Performance
- ✅ **PASS**: Authorization endpoint response time < 200ms (p95)
- ✅ **PASS**: Token endpoint response time < 300ms (p95)
- ✅ **PASS**: DPoP proof validation overhead < 50ms (p95)

### SC-03: Test Coverage
- ✅ **PASS**: 100% code coverage for authorization endpoint
- ✅ **PASS**: 100% code coverage for PKCE validation
- ✅ **PASS**: 100% code coverage for DPoP proof validation
- ✅ **PASS**: Integration tests for full authorization code flow
- ✅ **PASS**: Integration tests for DPoP-bound token usage

### SC-04: Documentation
- ✅ **PASS**: Mobile app integration guide published
- ✅ **PASS**: Desktop app integration guide published
- ✅ **PASS**: API documentation updated with new endpoints
- ✅ **PASS**: Migration guide for existing password grant clients

### SC-05: Backward Compatibility
- ✅ **PASS**: Existing refresh tokens continue to work
- ✅ **PASS**: 30-day deprecation period for password grant
- ✅ **PASS**: Clear error messages for deprecated flows

---

## 5. Edge Cases

### EC-01: PKCE Code Challenge Validation Failure
**Scenario**: Client sends invalid `code_verifier` that doesn't match `code_challenge`
**Expected**: Return `400 Bad Request` with `error=invalid_grant`, `error_description="Invalid code verifier"`
**Test**: Unit test with mismatched code verifier

### EC-02: Authorization Code Replay Attack
**Scenario**: Attacker intercepts authorization code and tries to use it
**Expected**: Authorization code is single-use; second attempt returns `400 Bad Request` with `error=invalid_grant`
**Test**: Integration test attempting to reuse authorization code

### EC-03: DPoP Proof Replay Attack
**Scenario**: Attacker captures DPoP proof and replays it
**Expected**: Server validates `jti` (unique ID) and `iat` (timestamp); replay rejected with `401 Unauthorized`
**Test**: Integration test with replayed DPoP proof

### EC-04: DPoP Key Rotation
**Scenario**: Client rotates DPoP key pair
**Expected**: New tokens bound to new key; remain valid until expiration
**Test**: Integration test with key rotation

### EC-05: Missing DPoP Proof for DPoP-Bound Token
**Scenario**: Client presents DPoP-bound access token without DPoP proof
**Expected**: Return `401 Unauthorized` with `WWW-Authenticate: DPoP error="use_dpop_nonce"`
**Test**: Integration test with missing DPoP header

### EC-06: Redirect URI Mismatch
**Scenario**: Client sends different `redirect_uri` in token request than authorization request
**Expected**: Return `400 Bad Request` with `error=invalid_grant`
**Test**: Unit test with mismatched redirect URIs

### EC-07: Expired Authorization Code
**Scenario**: Client attempts to exchange authorization code after 10-minute expiration
**Expected**: Return `400 Bad Request` with `error=invalid_grant`, `error_description="Authorization code expired"`
**Test**: Integration test with expired code

### EC-08: Public Client with Client Secret
**Scenario**: Public client mistakenly sends `client_secret` in token request
**Expected**: Ignore `client_secret` (public clients don't authenticate)
**Test**: Integration test with public client sending secret

### EC-09: Network Failure During Authorization
**Scenario**: Network fails after authorization code issued but before redirect
**Expected**: Authorization code remains valid for 10 minutes; client can retry
**Test**: Manual test with network interruption

### EC-10: Concurrent Token Requests with Same Refresh Token
**Scenario**: Client sends multiple token requests with same refresh token simultaneously
**Expected**: Refresh token reuse leeway (30 seconds) allows concurrent requests
**Test**: Integration test with concurrent requests

---

## 6. Technical Design

### 6.1 Architecture Changes

```
┌─────────────────────────────────────────────────────────────┐
│                     Mobile/Desktop App                       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  1. Generate PKCE code_verifier & code_challenge       │ │
│  │  2. Open system browser with /authorize URL            │ │
│  │  3. User authenticates in browser                      │ │
│  │  4. Receive authorization code via deep link           │ │
│  │  5. Exchange code for tokens (with code_verifier)      │ │
│  │  6. Generate DPoP proof for each API request           │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                 │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              Quater Backend API (Authorization Server)       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  GET /api/auth/authorize                               │ │
│  │    - Validate client_id, redirect_uri, scope           │ │
│  │    - Authenticate user (ASP.NET Core Identity)         │ │
│  │    - Store code_challenge with authorization code      │ │
│  │    - Redirect to redirect_uri with code & state        │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  POST /api/auth/token                                  │ │
│  │    - Validate code_verifier matches code_challenge     │ │
│  │    - Validate DPoP proof (if present)                  │ │
│  │    - Issue DPoP-bound access token (token_type=DPoP)   │ │
│  │    - Bind refresh token to DPoP key (public clients)   │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Protected Resources (e.g., /api/samples)              │ │
│  │    - Validate DPoP proof (signature, htm, htu, a │
│  │    - Verify token binding (jkt claim matches proof)    │ │
│  │    - Authorize request based on token claims           │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 6.2 OpenIddict Configuration Changes

**File**: `backend/src/Quater.Backend.Api/Extensions/ServiceCollectionExtensions.cs`

**Changes**:
```csharp
// REMOVE (line 204)
options.AllowPasswordFlow();

// REMOVE (line 210) - Incorrect usage
options.RequireProofKeyForCodeExchange();

// ADD - Authorization code flow
options.AllowAuthorizationCodeFlow();

// ADD - Public client support
options.AcceptAnonymousClients();

// ADD - Authorization endpoint
options.SetAuthorizationEndpointUris("/api/auth/authorize");

// ADD - DPoP support (if OpenIddict supports it, otherwise custom middleware)
// Note: OpenIddict 5.8.0 may not have native DPoP support - check documentation
```

### 6.3 New Files Required

1. **`AuthorizationController.cs`** - Authorization endpoint implementation
2. **`DPoPProofValidator.cs`** - DPoP proof validation logic
3. **`DPoPMiddleware.cs`** - Middleware for DPoP proof extraction and validation
4. **`PKCEValidator.cs`** - PKCE code challenge/verifier validation
5. **`AuthorizationCodeStore.cs`** - In-memory or database storage for authorization codes
6. **`DPoPProofValidationTests.cs`** - Unit tests for DPoP validation
7. **`AuthorizationCodeFlowTests.cs`** - Integration tests for authorization code flow

### 6.4 Database Schema Changes

**Table**: `OpenIddictAuthorizations` (existing, no changes)
**Table**: `OpenIddictTokens` (existing, add `DPoPKeyThumbprint` column)

```sql
ALTER TABLE OpenIddictTokens
ADD COLUMN DPoPKeyThumbprint VARCHAR(256) NULL;

CREATE INDEX IX_OpenIddictTokens_DPoPKeyThumbprint
ON OpenIddictTokens(DPoPKeyThumbprint);
```

### 6.5 Configuration Changes

**File**: `backend/src/Quater.Backend.Api/appsettings.json`

```json
{
  "OpenIddict": {
    "Issuer": "",
    "Audience": "",
    "AccessTokenLifetime": 3600,
    "RefreshTokenLifetime": 604800,
    "RefreshTokenReuseLeewaySeconds": 30,
    "AuthorizationCodeLifetime": 600,
    "EncryptionCertificatePath": "",
    "EncryptionCertificatePassword": "",
    "SigningCertificatePath": "",
    "SigningCertificatePassword": "",
    "DPoP": {
      "Enabled": true,
      "RequireNonce": false,
      "NonceLifetime": 300,
      "AllowedClockSkew": 60
    }
  }
}
```

---

## 7. Implementation Phases

### Phase 1: Authorization Code Flow + PKCE (Week 1-2)
**Goal**: Replace password grant with authorization code flow

**Tasks**:
1. Remove password grant configuration
2. Add authorization code flow configuration
3. Implement authorization endpoint (`/api/auth/authorize`)
4. Implement PKCE validation in token endpoint
5. Update `OpenIddictSeeder` for public client
6. Write integration tests for authorization code flow
7. Update API documentation

**Deliverables**:
- ✅ Authorization endpoint functional
- ✅ PKCE van working
- ✅ Integration tests passing
- ✅ API documentation updated

### Phase 2: DPoP Token Binding (Week 3-4)
**Goal**: Add DPoP proof-of-possession for sender-constrained tokens

**Tasks**:
1. Implement DPoP proof validation logic
2. Add DPoP middleware for proof extraction
3. Bind access tokens to DPoP public key
4. Update token endpoint to accept DPoP proofs
5. Update resource servers to validate DPoP proofs
6. Write unit tests for DPoP validation
7. Write integration tests for DPoP-bound tokens

**Deliverables**:
- ✅ DPoP proof validation functional
- ✅ DPoP-bound tokens issued
- ✅ Resource servers validate DPoP proofs
- ✅ Unit and integration tests passing

### Phase 3: Client Integration & Documentation (Week 5)
**Goal**: Provide client-side implementation guidance

**Tasks**:
1. Write mobile app integration guide (iOS/Android)
2. Write desktop app integration guide (.NET MAUI/Electron)
3. Create code examples for PKCE generation
4. Create code examples for DPoP proof generation
5. Update AGENTS.md with OAuth 2.0 best practices
6. Create migration guide for existing clients

**Deliverables**:
- ✅ Mobile app integration guide
- ✅ Desktop app integration guide
- ✅ Code examples published
- ✅ Migration guide published

### Phase 4: Testing & Validation (Week 6)
**Goal**: Comprehensive testing and security validation

**Tasks**:
1. Security audit of OAuth 2.0 implementation
2. Penetration testing (authorization code interception, token replay)
3. Performance testing (authorization endpoint, token endpoint)
4. Load testing (concurrent authorization requests)
5. Compliance validation (RFC 7636, RFC 9449)
6. Fix any issues found

**Deliverables**:
- ✅ Security audit report
- ✅ Penetration test results
- ✅ Performance benchmarks
- ✅ Compliance validation report
\n
## 8. Security Considerations

### 8.1 Authorization Code Security
- **Single-Use**: Authorization codes MUST be single-use
- **Expiration**: Authorization codes MUST expire after 10 minutes
- **Binding**: Authorization codes MUST be bound to `client_id` and `redirect_uri`
- **PKCE**: Authorization codes MUST be bound to `code_challenge`

### 8.2 PKCE Security
- **Code Challenge Method**: MUST support `S256` (SHA-256)
- **Code Challenge Method**: SHOULD NOT support `plain` (only for legacy clients)
- **Code Verifier**: MUST be 43-128 characters (base64url-encoded random bytes)
- **Validation**: MUST validate `code_verifier` matches `code_challenge`

### 8.3 DPoP Security
- **Key Storage**: Private keys MUST be stored in secure storage (Keychain/Keystore)
- **Proof Uniqueness**: Each DPoP proof MUST have unique `jti` (prevents replay)
- **Timestamp Validation**: DPoP proofs MUST have recent `iat` (within 60 seconds)
- **Token Binding**: DPoP proofs MUST include `ath` claim (hash of access token)
- **Nonce**: SHOULD support server-provided nonce for additional replay protection

### 8.4 Redirect URI Security
- **Validation**: MUST validate `redirect_uri` matches registered URI
- **Deep Links**: Mobile apps MUST use custom URL schemes or universal links
- **HTTPS**: Web apps MUST use HTTPS redirect URIs (no HTTP in production)

### 8.5 Token Security
- **Encryption**: Access tokens MUST be encrypted (already implemented)
- **Signing**: Access tokens MUST be signed (already implemented)
- **Binding**: Access tokens MUST be bound to DPoP public key (new)
- **Expiration**: Access tokens MUST expire after 1 hour (already configured)

---

## 9. Testing Strategy

### 9.1 Unit Tests
- ✅ PKCE code challenge generation (S256, plain)
- ✅ PKCE code verifier validation
- ✅ DPoP proof JWT parsing
- ✅ DPoP proof signature validation
- ✅ DPoP proof claim validation (`htm`, `htu`, `ath`, `jti`, `iat`)
- ✅ Authoion code generation and validation
- ✅ Redirect URI validation

### 9.2 Integration Tests
- ✅ Full authorization code flow (authorize → token)
- ✅ PKCE validation in token endpoint
- ✅ DPoP-bound token issuance
- ✅ DPoP-bound token usage on protected resources
- ✅ Refresh token flow with DPoP
- ✅ Authorization code expiration
- ✅ Authorization code replay prevention
- ✅ DPoP proof replay prevention

### 9.3 Security Tests
- ✅ Authorization code interception attack
- ✅ PKCE downgrade attack (force `plain` method)
- ✅ DPoP proof replay attack
- ✅ Token theft without DPoP proof
- ✅ Redirect URI manipulation
- ✅ State parameter CSRF protection

### 9.4 Performance Tests
- ✅ Authorization endpoint throughput (requests/second)
- ✅ Token endpoint throughput (requests/second)
- ✅ DPoP proof validation latency
- ✅ Concurrent authorization requests
- ✅ Database query performance (authorization code lookup)

---

## 10. Migration Plan

### 10.1 Deprecation Timeline

**Week 1-2**: Announcement
- Publish deprecation notice for password grant
- Update API documentation with migration guide
- Send email to registered developers

**Week 3-4**: Dual Support
- Both password grant and authorization code flow supported
- Log warnings for password grant usage
- Provide migration assistance

**Week 5-6**: Enforcement
- Disable password grant in production
- Return `400 Bad Request` with migration instructions
- Monitor error rates and provide support

### 10.2 Migration Guide for Clients

**Step 1**: Update OAuth 2.0 library
- iOS: Use `AppAuth-iOS` library
- Android: Use `AppAuth-Android` library
- .NET: Use `IdentityModel.OidcClient` library

**Step 2**: Implement authorization code flow
- Generate PKCE code verifier and challenge
- Open system browser with authorization URL
- Handle redirect wzation code
- Exchange code for tokens

**Step 3**: Implement DPoP (optional but recommended)
- Generate ES256 key pair
- Store private key in secure storage
- Generate DPoP proof for each request
- Include DPoP header in API requests

**Step 4**: Test migration
- Test authorization flow in development
- Test token refresh
- Test DPoP-bound token usage
- Deploy to production

---

## 11. Open Questions (Max 3)

### Q1: DPoP Implementation Approach
**Question**: Should we implement DPoP using custom middleware or wait for native OpenIddict support?

**Context**: OpenIddict 5.8.0 may not have native DPoP support. We can either:
- **Option A**: Implemeustom DPoP middleware (more control, more work)
- **Option B**: Upgrade to OpenIddict 6.x if it has DPoP support (less work, dependency upgrade)
- **Option C**: Implement Phase 1 (Authorization Code + PKCE) first, defer DPoP to Phase 2

**Recommendation**: Option C - Implement authorization code flow + PKCE first (critical security fix), then evaluate DPoP implementation approach based on OpenIddict roadmap.

### Q2: Mobile App Redirect URI Strategy
**Question**: What redirect URI scheme should mobile apps use?

**Context**: Mobile apps need to receive authorization codes via deep links. Options:
- **Option A**: Custom URL scheme (`quater://oauth/callback`) - Simple but can be hijacked
- **Option B**: Universal Links (iOS) / App Links (Android) - Secure but requires domain verification
- **Option C**: Loopback redirect (`http://127.0.0.1:PORT/callback`) - Desktop apps only

**Recommendation**: Option B for mobile apps (Universal Links/App Links), Option C for desktop apps (loopback). Require PKCE for all clients to mitigate redirect URI hijacking.

### Q3: Backward Compatibility for Existing Tokens
**Question**: How should we handle existing access tokens and refresh tokens issued via password grant?

**Context**: Existing tokens were issuedP binding. Options:
- **Option A**: Invalidate all existing tokens (force re-authentication)
- **Option B**: Allow existing tokens until expiration (grace period)
- **Option C**: Support both Bearer and DPoP tokens during migration

**Recommendation**: Option C - Support both Bearer and DPoP tokens for 30 days, then require DPoP for new tokens. Existing refresh tokens remain valid but new access tokens are DPoP-bound.

---

## 12. References

### OAuth 2.0 Specifications
- **RFC 6749**: The OAuth 2.0 Authorization Framework
- **RFC 7636**: Proof Key for Code Exchange (PKCE)
- **RFC 8252**: OAuth 2.0 for Native Apps
- **RFC 9449**: OAuth 2.0 Demonstrating Proof of Possession (DPoP)
- **OAuth 2.0 Security Best Current Practice**: https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics

### OpenIddict Documentation
- **OpenIddict Documentation**: https://documentation.openiddict.com/
- **Authorization Code Flow**: https://documentation.openiddict.com/guides/choosing-the-right-flow.html
- **PKCE Configuration**: https://documentation.openiddict.com/configuration/proof-key-for-code-exchange.html

### Client Libraries
- **AppAuth-iOS**: https://github.com/uth-iOS
- **AppAuth-Android**: https://github.com/openid/AppAuth-Android
- **IdentityModel.OidcClient**: https://github.com/IdentityModel/IdentityModel.OidcClient

---

## 13. Approval & Next Steps

### Approval Checklist
- [ ] Security team review
- [ ] Architecture team review
- [ ] Mobile team review (client-side impact)
- [ ] Desktop team review (client-side impact)
- [ ] Product owner approval

### Next Steps After Approval
1. Create implementation tasks in project management system
2. Assign tasks to development team
3. Schedule kickoff meeting
4. Begin Phase 1 implementation (Authorization Code Flow + PKCE)
5. Weekly progress reviews

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-XX  
**Author**: AI Assistant  
**Status**: Draft - Awaiting Approval

# Feature Specification: Email System & Security

**Feature Branch**: `002-email-system`  
**Created**: 2026-02-01  
**Status**: Draft  
**Input**: User description: "Comprehensive email system specifications including verification, password reset, and infrastructure."

## Clarifications

### Session 2026-02-01
- Q: What are the specific lifetimes for SecurityTokens (Password Reset vs. Email Verification)? → A: Reset: 1 hour, Verification: 24 hours.
- Q: How should the system handle repeated failed login attempts (Brute Force)? → A: Block account (30m) & Notify user via email.
- Q: Should the email queue persist across application restarts? → A: In-Memory (Lossy on restart) - sufficient for MVP.
- Q: How are email templates managed? → A: File-based (embedded resources or disk) - simpler for MVP than DB.
- Q: Should the system reveal if an email exists during Password Reset request? → A: No (Silent Fail) - prevent email enumeration.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Email Verification (Priority: P1)

As a new user, I need to verify my email address so that the system knows my identity is valid and I can access secured features.

**Why this priority**: Core security requirement. Prevents fake accounts and ensures communication channel reliability.

**Independent Test**: Can be tested by creating a new account, checking the mock/real inbox, clicking the link, and verifying the `EmailConfirmed` status updates in the database.

**Acceptance Scenarios**:

1. **Given** a newly registered user, **When** the registration completes, **Then** an email with a verification link is sent to their address.
2. **Given** a received verification email, **When** I click the link, **Then** my account status updates to "Verified" and I am redirected to a success page.
3. **Given** an invalid or expired verification token, **When** I click the link, **Then** I see an error message and am prompted to request a new one.

---

### User Story 2 - Password Reset (Priority: P1)

As a user who forgot their password, I need to reset it via an email link so that I can regain access to my account.

**Why this priority**: Critical for account recovery. Users locked out without this feature are permanently lost.

**Independent Test**: Can be tested by initiating a "Forgot Password" request, receiving the email, and successfully logging in with the new password.

**Acceptance Scenarios**:

1. **Given** a valid user email, **When** I request a password reset, **Then** an email with a time-limited reset link is sent.
2. **Given** a valid reset link, **When** I click it and enter a new password, **Then** my password is updated, and all existing sessions are invalidated.
3. **Given** an invalid/expired token, **When** I attempt to reset, **Then** the system rejects the request.
4. **Given** an invalid email address (not in system), **When** I request a password reset, **Then** the system returns a generic success message (no error) to prevent enumeration.

---

### User Story 3 - Security Alerts (Priority: P2)

As a user, I want to be notified of suspicious account activity so that I can take action if my account is compromised.

**Why this priority**: Enhances security posture and user trust.

**Independent Test**: Trigger a login from a "new" device (or mock IP) and verify an alert email is sent.

**Acceptance Scenarios**:

1. **Given** a user account, **When** a login occurs from a previously unseen IP address or device, **Then** a "New Sign-in Alert" email is sent to the user.
2. **Given** multiple failed login attempts, **When** the account is locked out, **Then** a "Account Locked" email is sent with instructions.

---

### User Story 4 - Email Change Verification (Priority: P2)

As a user, I want to change my email address securely so that I can maintain access if my email provider changes.

**Why this priority**: Standard account management feature.

**Independent Test**: Update email in profile -> verify email is sent to NEW address -> link click updates the record.

**Acceptance Scenarios**:

1. **Given** a logged-in user, **When** I request to change my email, **Then** a verification link is sent to the *new* email address.
2. **Given** the verification link, **When** I click it, **Then** the user's email is updated in the system.

---

### Edge Cases

- What happens when the email provider is down? (Queue should retry with backoff).
- What happens if a user requests multiple tokens rapidly? (Rate limiting should block abuse).
- What happens if a token is used twice? (Second attempt must fail).
- What happens if the application restarts while emails are queued? (In-memory queue items are lost - acceptable for MVP).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an abstraction for sending emails (SMTP, SendGrid, etc.) configurable via `appsettings.json`.
- **FR-002**: System MUST support HTML and Plain Text email formats with a templating engine (e.g., Scriban) using file-based templates.
- **FR-003**: System MUST send emails asynchronously using an in-memory background queue to prevent blocking API responses.
- **FR-004**: System MUST generate cryptographically secure, time-limited tokens for Verification (24 hours) and Reset (1 hour) flows.
- **FR-005**: System MUST invalidate tokens immediately after successful use (One-Time Use).
- **FR-006**: System MUST enforce rate limits on email-triggering endpoints (e.g., max 3 reset requests per hour).
- **FR-007**: System MUST support localization of email templates based on user preference.
- **FR-008**: System MUST lock accounts for 30 minutes after 5 failed login attempts and send an "Account Locked" notification.
- **FR-009**: System MUST return generic success responses for Password Reset requests to non-existent emails (prevent enumeration).

### Key Entities *(include if feature involves data)*

- **EmailTemplate**: Represents a specific email type (Welcome, Reset, Alert) with subject and body templates.
- **EmailQueueItem**: A serialized email request waiting for processing.
- **SecurityToken**: Ephemeral token linked to a user and an action (Verify/Reset).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 99.9% of transactional emails are queued for delivery within 500ms of the API request.
- **SC-002**: Token validation failures (expired/invalid) are logged with security severity.
- **SC-003**: Background email processor recovers from SMTP failures by retrying at least 3 times before failing.
- **SC-004**: Users receive "Forgot Password" emails within 2 minutes of request under normal load.

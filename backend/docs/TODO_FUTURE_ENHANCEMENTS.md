# TODO Future Enhancements

## Resend invitation email
- TODO: Add the ability for admins to resend pending invitations from the user management UI.
- TODO: Record resend events in the audit log with actor, target, and timestamp.

## Invitation templates
- TODO: Support multiple invitation email templates with localized variants.
- TODO: Allow admins to select a template when sending or resending invitations.

## Bulk user import
- TODO: Add CSV import for bulk invitations with validation and preview.
- TODO: Provide per-row status reporting and downloadable error summaries.

## Self-service password reset for invited users
- TODO: Allow invited users to request a password reset before first login.
- TODO: Ensure reset tokens respect invitation expiry and tenant isolation.

## Two-factor authentication (2FA)
- TODO: Add optional 2FA enrollment using TOTP for tenant accounts.
- TODO: Provide recovery codes and enforcement policies per tenant.

## SSO/SAML integration
- TODO: Integrate SSO with SAML providers for enterprise tenants.
- TODO: Support IdP-initiated and SP-initiated login flows.

## User profile completion wizard
- TODO: Guide new users to complete required profile fields after first login.
- TODO: Block access to sensitive areas until required fields are complete.

## Invitation analytics
- TODO: Track invitation send, open, accept, and expire rates by tenant.
- TODO: Expose analytics dashboards and exportable reports.

# Implementation Plan - Email System & Security

## Technical Context

**Feature**: Email System & Security
**Branch**: `002-email-system`
**Specification**: [specs/002-email-system/spec.md](specs/002-email-system/spec.md)

This feature introduces a comprehensive email infrastructure and security-critical flows (Verification, Reset, Alerts) to the backend.

**Key Technical Decisions:**

1.  **Architecture**: The user requested splitting this into a separate project. We will create a new class library `Quater.Backend.Infrastructure.Email` to isolate email logic, dependencies (MailKit/Scriban), and templates. This keeps the core clean and follows Clean Architecture principles.
2.  **Queueing**: In-memory `Channel<T>` based queue for MVP (as clarified in spec).
3.  **Templating**: `Scriban` for rendering HTML/Text email bodies. Templates stored as embedded resources in the new project.
4.  **Security**: 
    - ASP.NET Core Identity for token generation.
    - OpenIddict for Auth integration.
    - Rate limiting via `System.Threading.RateLimiting` or existing middleware.
    - Brute force protection via Identity's `Lockout` feature.

**Unknowns & Risks:**

- **Integration**: Wiring up `IEmailSender` into the existing `AuthController` without breaking current flows.
- **Project Structure**: Ensuring the new `Infrastructure.Email` project is correctly referenced by `Api` and `Services` (or vice-versa depending on abstractions).
    - *Plan*: Core defines `IEmailSender`. `Infrastructure.Email` implements it. `Api` references both.

## Constitution Check

| Principle | Status | Notes |
| :--- | :--- | :--- |
| **I. Conventions** | ✅ | Will follow C# 13 / .NET 10 standards (file-scoped namespaces, primary constructors). |
| **II. Offline-First** | ⚪ | N/A - Backend infrastructure feature. |
| **III. Platform Integrity** | ✅ | Using standard ASP.NET Core Identity & MailKit patterns. |
| **IV. Verification** | ⚠️ | Must ensure tests cover the new project and integration. |
| **V. Strategic Workflow** | ✅ | Following Spec -> Plan -> Task flow. |

## Phased Execution

### Phase 0: Research & Design

- [x] **Research**: Confirm best structure for modular email project in Clean Architecture (Core vs Infrastructure).
- [x] **Design**: Define `IEmailSender` interface in Core.
- [x] **Design**: Define Email Template contract and storage mechanism (Embedded Resource vs Physical File).

### Phase 1: Infrastructure & Core

- [ ] **Scaffold**: Create `Quater.Backend.Infrastructure.Email` project.
- [ ] **Dependencies**: Install `MailKit`, `Scriban`, `Microsoft.Extensions.Options.ConfigurationExtensions`.
- [ ] **Core**: Add `IEmailSender` interface and `EmailMessage` DTOs to `Quater.Backend.Core`.
- [ ] **Implementation**: Implement `SmtpEmailSender` and `ScribanTemplateRenderer`.
- [ ] **Queue**: Implement `BackgroundEmailQueue` service (BackgroundService).

### Phase 2: Functional Implementation

- [ ] **Templates**: Create HTML/Text templates for Welcome, Verify, Reset, Alert.
- [ ] **Auth Integration**: Update `AuthController` to trigger emails on events (Register, ForgotPassword).
- [ ] **Security**: Implement Rate Limiting and Brute Force Lockout logic.
- [ ] **API**: Add endpoints for `verify-email`, `resend-verification`, `forgot-password`, `reset-password`.

### Phase 3: Verification & Polish

- [ ] **Testing**: Unit tests for Template Renderer and Queue logic.
- [ ] **Integration Test**: Verify email flow with Mock SMTP server (e.g., Smtp4Dev or similar).
- [ ] **Docs**: Update `AGENTS.md` with email config details.

## Q&A / Clarifications

*   *User Input*: "Split this email features into a seperate dedicated project for modularity" -> **Adopted**. New project `Quater.Backend.Infrastructure.Email`.

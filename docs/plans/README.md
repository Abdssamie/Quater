# Implementation Plans

This directory contains detailed, step-by-step implementation plans for the Quater water quality management system.

## Plans Overview

### Phase 1: Critical Security Fixes (Plans 1-4)

These plans fix critical security vulnerabilities and must be completed before multi-tenancy implementation.

**Execution Order:** Sequential (1 → 2 → 3 → 4)

1. **[Login Security Fixes](2026-02-06-login-security-fixes.md)** (~3 hours)
   - Fix timing attack vulnerability in login page
   - Add rate limiting to prevent brute-force attacks
   - **Status:** Ready to execute
   - **Dependencies:** None

2. **[Registration Security Fix](2026-02-06-registration-security-fix.md)** (~2 hours)
   - Fix privilege escalation vulnerability
   - Hard-code self-registered users to Viewer role
   - **Status:** Ready to execute
   - **Dependencies:** None (can run in parallel with Plan 1)

3. **[Claim Type Fixes](2026-02-06-claim-type-fixes.md)** (~3 hours)
   - Fix claim type usage to follow OAuth2/OIDC standards
   - Add LabId extraction methods (prerequisite for multi-tenancy)
   - **Status:** Ready to execute
   - **Dependencies:** None (can run in parallel with Plans 1-2)

4. **[Authorization Infrastructure Fixes](2026-02-06-authorization-infrastructure-fixes.md)** (~2 hours)
   - Add fallback authorization policy
   - Fix exception handling in AuthorizationController
   - **Status:** Ready to execute
   - **Dependencies:** None (can run in parallel with Plans 1-3)

**Total Time for Phase 1:** ~10 hours (or ~3-4 hours if executed in parallel)

---

### Phase 2: Multi-Tenancy Implementation (Plans 5-6)

These plans implement SaaS multi-tenant architecture with row-level security.

**Execution Order:** Sequential (5 → 6)

5. **Multi-Tenancy Foundation** (~8 hours) - **NOT YET CREATED**
   - Create UserLab many-to-many relationship
   - Migrate existing data
   - Add helper methods
   - **Status:** Plan needs to be written
   - **Dependencies:** Plans 1-4 completed

6. **Multi-Tenancy Row-Level Security** (~15 hours) - **NOT YET CREATED**
   - Implement service-level filtering by LabId
   - Add authorization handlers for resource-based checks
   - Update all controllers
   - Comprehensive testing
   - **Status:** Plan needs to be written
   - **Dependencies:** Plan 5 completed

**Total Time for Phase 2:** ~23 hours

---

## Total Implementation Time

- **Phase 1 (Security Fixes):** ~10 hours (or 3-4 hours parallel)
- **Phase 2 (Multi-Tenancy):** ~23 hours
- **Grand Total:** ~33 hours (~1 week full-time)

---

## Execution Options

### Option A: Subagent-Driven Development (Recommended for Phase 1)
- Execute plans in current session
- Dispatch fresh subagent per task
- Review between tasks
- Fast iteration with human oversight

**Best for:** Plans 1-4 (critical security fixes)

### Option B: Parallel Session Execution
- Open new session with `executing-plans` skill
- Batch execution with checkpoints
- Less human oversight, faster execution

**Best for:** Plans 5-6 (multi-tenancy implementation)

---

## Current Status

✅ **Plans 1-4 completed and ready to execute**
❌ **Plans 5-6 need to be written**

---

## Next Steps

1. **Execute Plans 1-4** (critical security fixes)
   - Can be done in parallel or sequentially
   - Recommended: Execute all 4 in parallel using subagent-driven development

2. **Write Plans 5-6** (multi-tenancy)
   - After Plans 1-4 are complete
   - More complex, requires careful planning

3. **Execute Plans 5-6** (multi-tenancy)
   - Sequential execution required (5 must complete before 6)
   - Recommended: Use parallel session execution for each plan

---

## Plan Format

Each plan follows the `writing-plans` skill format:
- Bite-sized tasks (2-5 minutes per step)
- Test-Driven Development (TDD)
- Exact file paths and commands
- Expected outputs
- Frequent commits

---

## Questions?

See individual plan files for detailed step-by-step instructions.

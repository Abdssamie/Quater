# Implementation Plans

This directory contains detailed, step-by-step implementation plans for the Quater water quality management system.

## Completed Plans

All Phase 1 security fix plans (Plans 1-4) have been **completed and merged** into the main branch:

1. ✅ **Login Security Fixes** - Timing attack mitigation + rate limiting (completed)
2. ✅ **Registration Security Fix** - Privilege escalation fix (completed)
3. ✅ **Claim Type Fixes** - OAuth2/OIDC standards compliance (completed)
4. ✅ **Authorization Infrastructure Fixes** - Fallback policy + error handling (completed)

**Archived plans:** See `archive/` directory for completed implementation plans.

---

## Future Plans

### Phase 2: Multi-Tenancy Implementation (Not Yet Created)

These plans will implement SaaS multi-tenant architecture with row-level security.

5. **Multi-Tenancy Foundation** (~8 hours) - **NOT YET CREATED**
   - Create UserLab many-to-many relationship
   - Migrate existing data
   - Add helper methods
   - **Dependencies:** Plans 1-4 completed ✅

6. **Multi-Tenancy Row-Level Security** (~15 hours) - **NOT YET CREATED**
   - Implement service-level filtering by LabId
   - Add authorization handlers for resource-based checks
   - Update all controllers
   - Comprehensive testing
   - **Dependencies:** Plan 5 completed

**Total Time for Phase 2:** ~23 hours

---

## Plan Format

Each plan follows the `writing-plans` skill format:
- Bite-sized tasks (2-5 minutes per step)
- Test-Driven Development (TDD)
- Exact file paths and commands
- Expected outputs
- Frequent commits

# Agent Instructions - Quater Water Quality Lab Management System

## Project Overview

**Quater** is an open-source, cross-platform water quality lab management system with three integrated applications:
- **Backend API**: ASP.NET Core 10.0 + PostgreSQL (C# 13 / .NET 10)
- **Desktop App**: Avalonia UI 11.x (Windows/Linux/macOS) (C# 13 / .NET 10)
- **Mobile App**: React Native 0.73+ (Android, field sample collection only)

**Architecture**: Offline-first with bidirectional sync, Last-Write-Wins conflict resolution with automatic backup.

---

## Monorepo Structure

- `backend/` - ASP.NET Core 10.0 Web API & Core Logic
- `desktop/` - Avalonia UI 11.x Desktop Application
- `mobile/` - React Native 0.73+ Mobile Application
- `specs/` - Feature specifications and planning documents (Speckit)

**Note**: Coding guidelines and build commands for each platform are located in their respective directories (`backend/AGENTS.md`, `desktop/AGENTS.md`, `mobile/AGENTS.md`) and are automatically loaded via `opencode.json`.

---

## Working with Beads (Issue Tracking)

Beads is used for task tracking and dependencies. **Use beads for strategic work** (multi-session, dependencies, discovered work).

### Quick Commands

```bash
bd ready                                  # Find available work
bd show quater-1                          # View issue details
bd update quater-1 --status=in_progress   # Claim work
bd close quater-1                         # Complete work
bd create --title="..." --type=task --priority=2  # Create issue
bd dep add quater-2 quater-1              # quater-2 depends on quater-1
bd sync --from-main                       # Sync beads from main branch
```

**Priority Levels**: 0 (critical) → 1 (high) → 2 (medium) → 3 (low) → 4 (backlog)

### Session Close Protocol (MANDATORY)

Before ending a session, you MUST:
1. Close completed beads issues: `bd close quater-1 quater-2 ...`
2. Run `bd sync --from-main` to pull latest beads updates
3. Commit code changes: `git add . && git commit -m "..."`
4. **DO NOT push to remote** - This is an ephemeral branch, merge to main locally

---

## Working with Speckit (Specifications)

Speckit manages feature specifications. All specs are in `specs/001-water-quality-platform/`.

### Key Files

- **spec.md**: User stories and requirements (v1.2)
- **plan.md**: Implementation plan, tech stack, project structure
- **data-model.md**: Complete data model for all components
- **research.md**: Technology decisions and rationale
- **ARCHITECTURE_DECISIONS.md**: 10 validated architecture decisions
- **contracts/sync.schema.json**: Bidirectional sync protocol

---

## Key Architecture Decisions

1. **Authentication**: ASP.NET Core Identity + OpenIddict (OAuth2/OIDC)
2. **Mobile Framework**: React Native (rejected .NET MAUI for reliability)
3. **Mobile Scope**: Field sample collection ONLY (no test entry/reporting)
4. **Conflict Resolution**: Last-Write-Wins with automatic backup
5. **TypeScript Generation**: NSwag auto-generates from OpenAPI (eliminates contract drift)
6. **API Versioning**: `/api/v1/` prefix for all endpoints
7. **Test Methods**: Enumeration (7 standard methods + Other)
8. **Audit Archival**: 90-day hot/cold split with nightly background job

See `specs/001-water-quality-platform/ARCHITECTURE_DECISIONS.md` for full details.

---

## Project Status

- **Specifications**: ✅ Complete (v1.2)
- **Architecture**: ✅ Validated
- **Implementation**: ⏳ Not started (ready to begin)
- **Branch**: `001-water-quality-platform` (monolithic feature approach)
- **Tech Stack**: C# 13 / .NET 10 (Backend + Desktop), React Native (Mobile)
Use 'bd' for task tracking

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds

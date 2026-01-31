<!--
Sync Impact Report:
- Version: 0.0.0 -> 1.0.0 (Initial Ratification)
- Added Principles: I. Conventions & Style, II. Offline-First Architecture, III. Platform Integrity, IV. Verification Gates, V. Strategic Workflow
- Added Sections: Architecture Standards, Development Workflow
- Templates Status:
  - .specify/templates/plan-template.md: ✅ Aligned (Generic Constitution Check)
  - .specify/templates/spec-template.md: ✅ Aligned
  - .specify/templates/tasks-template.md: ✅ Aligned
- Follow-up:
  - Referenced directory .specify/templates/commands/ is missing in filesystem (cited in plan-template.md).
-->
# Quater Constitution

## Core Principles

### I. Conventions & Style
Rigorously adhere to existing project conventions. Analyze surrounding code, tests, and configuration before modifying. Mimic the style, structure, and architectural patterns of the specific platform (C#/.NET vs TypeScript/React).

### II. Offline-First Architecture
Design all mobile field features for offline capability with bidirectional sync. Use Last-Write-Wins for conflict resolution. Ensure data integrity across sync boundaries. Mobile app is strictly for field sample collection.

### III. Platform Integrity
Respect platform idioms and constraints.
- **Backend**: ASP.NET Core 10 (Clean Architecture, C# 13)
- **Desktop**: Avalonia UI 11.x (MVVM, C# 13)
- **Mobile**: React Native 0.73+ (TypeScript, Functional Components)
Do not cross-contaminate patterns (e.g., no React hooks concepts in C# ViewModels unless adapted to MVVM).

### IV. Verification Gates
**Non-Negotiable**: Work is not complete until `git push` succeeds. Before pushing, you MUST:
1. Run platform-specific builds (no errors).
2. Run linters/formatters (no violations).
3. Run tests (no regressions).
4. Update issue tracking (Beads).

### V. Strategic Workflow
Use **Beads** for all task tracking. Follow the **Speckit** flow for features: Plan (`/speckit.plan`) → Spec → Tasks (`/speckit.tasks`). Do not bypass the planning phase for non-trivial features.

## Architecture Standards

- **Authentication**: ASP.NET Core Identity + OpenIddict (OAuth2/OIDC).
- **API Versioning**: All endpoints must be prefixed with `/api/v1/`.
- **Client Generation**: Use NSwag to auto-generate TypeScript clients from OpenAPI specs to prevent contract drift.
- **Audit Logs**: 90-day hot/cold archival split.

## Development Workflow

1. **Start**: `bd ready` to find work, `bd update ...` to claim.
2. **Plan**: Generate implementation plan and specifications for new features.
3. **Implement**: Follow platform-specific guidelines in `backend/`, `desktop/`, or `mobile/`.
4. **Finish**: Close beads (`bd close`), sync (`bd sync`), commit, and push.

## Governance

This constitution supersedes all other practices. Amendments require documentation and team approval.
- **Compliance**: All PRs must verify compliance with these principles.
- **Versioning**: Follow Semantic Versioning (MAJOR.MINOR.PATCH) for this document.
- **Guidance**: See `AGENTS.md` in root and subdirectories for specific technical commands.

**Version**: 1.0.0 | **Ratified**: 2026-01-31 | **Last Amended**: 2026-01-31

---
description: Create Beads issues directly from specifications for the feature based on available design artifacts.
handoffs:
  - label: Analyze For Consistency
    agent: speckit.analyze
    prompt: Run a project analysis for consistency
    send: true
  - label: Start Working on Issues
    agent: beads-task-agent
    prompt: Show me what's ready to work on
    send: true
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. **Setup**: Run `.specify/scripts/bash/check-prerequisites.sh --json` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS list. All paths must be absolute. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Load design documents**: Read from FEATURE_DIR:
   - **Required**: plan.md (tech stack, libraries, structure), spec.md (user stories with priorities)
   - **Optional**: data-model.md (entities), contracts/ (API endpoints), research.md (decisions), quickstart.md (test scenarios)
   - Note: Not all projects have all documents. Generate tasks based on what's available.

3. **Analyze specifications and plan task structure**:
   - Load plan.md and extract tech stack, libraries, project structure
   - Load spec.md and extract user stories with their priorities (P1, P2, P3, etc.)
   - If data-model.md exists: Extract entities and map to user stories
   - If contracts/ exists: Map endpoints to user stories
   - If research.md exists: Extract decisions for setup tasks
   - Organize tasks by phase: Setup → Foundational → User Stories (P1, P2, P3) → Polish
   - Identify dependencies between tasks
   - Identify parallel execution opportunities

4. **Create Beads issues using beads-task-agent**: Use the Task tool with subagent_type="beads-task-agent" to create all issues:
   - Create issues for Phase 1 (Setup tasks - project initialization)
   - Create issues for Phase 2 (Foundational tasks - blocking prerequisites)
   - Create issues for each user story phase (in priority order from spec.md)
   - Create issues for final phase (Polish & cross-cutting concerns)
   - Set appropriate priorities:
     - Priority 0 (critical): Setup tasks that block everything
     - Priority 1 (high): Foundational tasks and P1 user stories
     - Priority 2 (medium): P2 user stories
     - Priority 3 (low): P3 user stories
     - Priority 4 (backlog): Polish and nice-to-have features
   - Set up dependencies using `bd dep add <issue> <depends-on>`
   - Include full task details in issue descriptions with file paths

5. **Report**: Output summary of created Beads issues:
   - Total issues created
   - Breakdown by priority (0-4)
   - Number of dependencies configured
   - Number of ready tasks (no blockers)
   - Number of blocked tasks
   - Next steps: How to see ready tasks (`bd ready`)
   - Suggested MVP scope (typically just User Story 1 issues)

Context for task generation: $ARGUMENTS

**IMPORTANT**: This command creates Beads issues directly, NOT a tasks.md file. Issues are tracked in `.beads/beads.db` and persist across sessions.

## Beads Issue Creation Guidelines

**CRITICAL**: Issues MUST be organized by phase and user story to enable independent implementation and testing.

**Tests are OPTIONAL**: Only create test-related issues if explicitly requested in the feature specification or if user requests TDD approach.

### Issue Title Format

Every Beads issue title should follow this format:

```text
[TaskID]: [Story?] Description
```

**Format Components**:

1. **Task ID**: Sequential identifier (T001, T002, T003...) for reference
2. **[Story] label**: Include for user story tasks only
   - Format: US1, US2, US3, etc. (maps to user stories from spec.md)
   - Setup phase: NO story label
   - Foundational phase: NO story label
   - User Story phases: MUST have story label
   - Polish phase: NO story label
3. **Description**: Clear, concise action

**Examples**:

- ✅ CORRECT: `T001: Create project structure`
- ✅ CORRECT: `T005: Install backend dependencies`
- ✅ CORRECT: `T012: US1 Create Sample entity`
- ✅ CORRECT: `T014: US1 Implement mobile sample collection screen`

### Issue Description Format

Each issue description should include:

1. **Full task details**: What needs to be done
2. **File paths**: Exact locations where code should be written
3. **Acceptance criteria**: How to verify the task is complete
4. **Related entities/components**: What this task depends on or affects
5. **Technical notes**: Any specific implementation guidance from specs

### Issue Organization by Phase

1. **Phase 1 - Setup** (Priority 0):
   - Project structure creation
   - Solution/project initialization
   - Basic configuration files

2. **Phase 2 - Foundational** (Priority 1):
   - Package/dependency installation
   - Domain model entities
   - Database context setup
   - Core infrastructure that blocks all user stories

3. **Phase 3+ - User Stories** (Priority based on story priority):
   - Priority 1 (high): P1 user stories from spec.md
   - Priority 2 (medium): P2 user stories from spec.md
   - Priority 3 (low): P3 user stories from spec.md
   - Each story should be independently implementable and testable

4. **Final Phase - Polish** (Priority 2-4):
   - Documentation
   - Performance optimization
   - Additional testing
   - Code cleanup

### Dependency Setup

Use `bd dep add <issue> <depends-on>` to establish dependencies:

1. **Setup dependencies**: Package installation depends on project initialization
2. **Entity dependencies**: Database context depends on entity models
3. **Migration dependencies**: Migrations depend on database context
4. **Feature dependencies**: User story implementations depend on foundational tasks
5. **Cross-story dependencies**: Mark if one user story depends on another (rare)

### Parallel Execution

Mark issues that can run in parallel by noting in the description:
- Different files
- No shared dependencies
- Independent components

This helps developers/agents identify work that can be done simultaneously.

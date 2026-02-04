---
description: Execute the implementation plan using Beads task management.
scripts:
  sh: scripts/bash/check-prerequisites.sh --json
  ps: scripts/powershell/check-prerequisites.ps1 -Json
---

## User Input

```text
$ARGUMENTS
```

## Workflow

1. **Prerequisites**:
   - Run `{SCRIPT}`.
   - Verify `plan.md` exists.
   - **Ignore Files**: Create `.gitignore` relevant to the project technologies (e.g., `bin/ obj/` for .NET, `node_modules/` for TS, `__pycache__/` for Python).

2. **Load Tasks from Beads**:
   - Run `bd ready` to see available tasks.
   - Run `bd list --status in_progress` to resume work.

3. **Execution Loop**:
   - **Pick Task**: Select from `bd ready` or `in_progress`.
   - **Start**: `bd update <ISSUE_ID> --status in_progress`.
   - **Context**: `bd show <ISSUE_ID>` to read instructions.
   - **Implement**: 
     - Write code/tests. 
     - **TDD Requirement**: If testing is needed, use the stack's framework (xUnit/pytest/Vitest). Write failing test -> Pass -> Refactor.
   - **Verify**: Run the project's test suite.
   - **Close**: `bd close <ISSUE_ID>` (Unblocks dependent tasks).
   - **Repeat**: Go back to `bd ready`.

4. **Error Handling**:
   - If blocked/failed: `bd comments add <ISSUE_ID> --body "Failure reason..."`.
   - Do NOT close failed tasks. Stop and report.

5. **Completion**:
   - When `bd ready` is empty and `bd list --status open` is empty:
   - Run `bd sync`.
   - Report success.


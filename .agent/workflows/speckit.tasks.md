---
description: Generate actionable, dependency-ordered tasks as Beads issues, tailored for .NET, Python, or TypeScript/Next.js projects.
handoffs: 
  - label: Analyze For Consistency
    agent: speckit.analyze
    prompt: Run a project analysis
    send: true
  - label: Implement Project
    agent: speckit.implement
    prompt: Start implementation
    send: true
scripts:
  sh: scripts/bash/check-prerequisites.sh --json
  ps: scripts/powershell/check-prerequisites.ps1 -Json
---

## User Input

```text
$ARGUMENTS
```

## Workflow

1. **Analyze Design**:
   - Read `plan.md` and `spec.md`.
   - Identify which of the 3 supported stacks is being used.
   - Extract user stories and dependencies.

2. **Delegate to beads agent**:
   - if the tasks to create are large and require many steps, it is better you delegate the creation beads issues to beads agent if it exists or if you have a tool to so. otherwise, you can create the tasks yourself. make sure the beads agent know the context of the specs and plan in details and has reference to them, ask hime to include detailed description for the tasks.

3. **Generate Import File (`beads_import.md`)**:
   - You **MUST** first create a markdown file named `beads_import.md` to bulk-create issues.
   - **Format Rules**:
     - Each task starts with `## Task Title`.
     - The text immediately following is the description.
     - Include tags like `[US1]` or `[Setup]` in the title or description.
     - Include exact file paths (e.g., `src/components/Button.tsx`).
     - **Critial**: Use the correct testing framework for the stack (xUnit for .NET, pytest for Python, Vitest for TS).

   - **Example Content**:
     ```markdown
     ## Setup Project Structure
     Initialize the repo structure. Ensure .gitignore is set for [Stack].

     ## [US1] Create Auth Service
     File: src/services/auth.ts
     Implement Clerk integration.
     
     ## [US1] P1 Test Auth Service
     File: tests/auth.test.ts
     Write Vitest tests for the auth service.
     ```

4. **Execute creation**:
   - Run: `bd create -f beads_import.md`
   - **Capture output**: The output contains the new Issue IDs (e.g., `specify-beads-optimized-x9z`).

5. **Set Dependencies**:
   - Parse the `bd create` output to map your tasks to IDs.
   - Run `bd dep add <BLOCKER_ID> <BLOCKED_ID>` for *every* dependency.
   - Example: If "Setup" blocks "Auth Service", run `bd dep add <setup_id> <auth_id>`.

6. **Report**:
   - Summary of created tasks and dependencies.
   - Suggest running `/speckit.implement`.

The tasks.md should be immediately executable - each task must be specific enough that an LLM can complete it without additional context.

# PollyDemo — Agent Instructions

An ASP.NET Core project with Minimal APIs and Blazor, progressively refactored toward Domain-Driven Design and Clean Architecture.

## Conventions

- Use `dotnet` CLI for all commands
- Don't write inline documentation
- Update `CHANGELOG.md` with every new feature or bugfix
- After every new feature update the `docs/` folder
- For each new feature implement a unit test in the tests folder
- Use xUnit as the test framework
- Minimal API endpoints go in `Program.cs` or separate extension files under `Endpoints/`
- Blazor components go under `Components/` (or `Pages/`/`Shared/`)
- Apply established design patterns when implementing each feature
- Progressively refactor toward Domain-Driven Design and Clean Architecture
- Before writing code, always plan first

## Change Classification & Pipeline

Not every change needs the full industrial pipeline. Before starting, triage the request into one of these categories and follow the corresponding pipeline:

### Tiny Fix
**Criteria:** ≤2 files, mechanical change, no new types/concepts, follows an existing pattern (e.g., null-check wrapper, retry logic, renaming).

**Pipeline:** Orchestrator implements directly. Run build + test only. Skip branching, subagent delegation, review, governance docs, and commit unless the user explicitly asks for them.

### Small Feature
**Criteria:** 3-5 files, extends existing abstractions, may introduce a single new type, no architectural impact.

**Pipeline:** Analyze → Branch (if on master) → Implement (delegate `coding` with a precise spec of what to write and where) → Build → Test → Optionally Review (delegate `review` if logic is non-trivial) → Governance (minimal changelog entry) → Commit → Final verify.

### Large Feature / Refactor
**Criteria:** 5+ files, new abstractions, new endpoints, architectural change, crosses a refactoring phase boundary.

**Pipeline:** Follow the full standard pipeline: Analyze → Branch (always) → Implement (delegate `coding` with spec) → Build → Test → Smoke test → Review (mandatory — delegate `review`) → Governance (update all three docs) → Commit → Final verify.

### Delegation Thresholds

| Category | Implements | Build/Test | Review | Branch/Commit | Governance |
|----------|-----------|------------|--------|---------------|------------|
| Tiny Fix | Orchestrator | Orchestrator | Skip | Skip | Skip |
| Small Feature | coding agent | Orchestrator | Optional | Required | Changelog only |
| Large Feature | coding agent | Orchestrator | review agent | Required | All three docs |

The orchestrator analyzes first, picks the category, then follows the appropriate pipeline. If the change grows in scope during implementation, escalate to the next category.

### Delegation Fallback

If a subagent call fails (timeout, error, unresponsive):
1. Fall back to direct execution via bash / dotnet CLI
2. Briefly report the failure to the user
3. Continue with the pipeline — do not block on a failed subagent

## Governance Docs (Session Handoff)

Three files track the project's state across sessions:

| File | Purpose |
|------|---------|
| `CHANGELOG.md` | Chronological log of all changes |
| `docs/REFACTORING_ROADMAP.md` | Phase-by-phase checklist of the refactoring plan |
| `docs/PROJECT_STRUCTURE.md` | Solution layout, dependencies, folder organization |

### Governance Update Thresholds

- **Tiny fix:** Skip governance updates unless the fix affects documented behavior or the user specifically asks.
- **Small feature:** Update `CHANGELOG.md` with a brief entry. Skip the other docs unless the change alters the dependency graph or folder structure.
- **Large feature / refactor:** Update all three governance docs as described in the Session Handoff Protocol.

### Session Handoff Protocol

1. **Before stopping** (context window limit, phase boundary, or blocking issue):
   - Update all three governance docs with what was done, what's in progress, and next steps
   - Report to user: accomplishments, remaining work, exact next step
   - Include the classification category assigned to each change and whether delegation was used
   
2. **Starting a new session:**
   - Load `update-governance-docs` skill to get the handoff template
   - Read `CHANGELOG.md` (latest [Unreleased] section)
   - Read `docs/REFACTORING_ROADMAP.md` (current phase status)
   - Read `docs/PROJECT_STRUCTURE.md` (current project layout)
   - Read `docs/REFACTORING_ROADMAP.md` (full remaining plan)

3. **Between phases:**
   - Update all three governance docs
   - Tell the user a phase is complete and ask for direction to the next phase

4. **During a session:**
   - When you classify a request, note the category and reasoning in the todo list
   - If you escalate from Tiny Fix to Small Feature mid-work, note why
   - If you fall back from a subagent to direct execution, record the failure reason


## Edit Safety Rules

When modifying an existing source file, follow these guards to prevent structural and encoding errors:

1. **Read before write** — Understand the file's layout before inserting or replacing code. Read the relevant section (at minimum the insertion target and its surroundings) before choosing a modification strategy.

2. **Use specific landmarks** — Target a unique, verifiable landmark for the insertion point (e.g., `class Foo` or a specific line of code). Do not match generic patterns like `}` or `<` that could land in the wrong scope.

3. **Verify by re-reading** — After writing, re-read the affected lines and their immediate context to confirm the edit landed in the correct position and is structurally valid (correct class/namespace nesting, no truncated syntax).

These rules apply to both the orchestrator and any subagent performing file edits.

## Available Skills

- `build-project` — Build the solution via `.opencode/skills/build-project/scripts/build.cmd`
- `update-governance-docs` — Update governance files and capture session handoff state
- `second-opinion` — Get code review from another agent

## Current Dependency Graph (as of Phase 6 complete — 71 tests)

```
PollyDemo.Domain          [no deps]
       ↑
PollyDemo.Application     [Domain, MediatR, FluentValidation]
       ↑
PollyDemo.Infrastructure  [Domain, Application, ClosedXML]
       ↑
PollyDemo.Bootstrapper    [Application, Infrastructure]
       ↑
├── UI.Web     [Bootstrapper]
├── UI.Console [Bootstrapper]
└── Auth.Api   [Bootstrapper]

Shared:
  PollyDemo.ServiceDefaults  [Aspire]
  PollyDemo.AppHost          [Aspire orchestration]
```

## Build & Test

```bash
dotnet build     # 0 errors expected
dotnet test      # 71/71 pass expected
```

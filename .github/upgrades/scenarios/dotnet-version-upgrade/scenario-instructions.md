# Scenario: dotnet-version-upgrade

## Goal
Upgrade ContosoUniversity from .NET Framework 4.8 to .NET 10.0, including SDK-style project
conversion, target framework update, NuGet dependency updates, and API compatibility fixes.

## Scope
- Project: ContosoUniversity.csproj (ASP.NET MVC web application, System.Web-based)
- Solution: ContosoUniversity.sln
- Target framework: net10.0

## User Preferences

### Flow Mode
**Automatic** - do not pause for user review at any stage (assessment, planning, breakdown,
execution). Only stop if genuinely blocked (missing info, ambiguous decision with major
consequences, or unrecoverable error). Print artifact paths inline and keep moving.

### Execution Style
- Fully autonomous execution requested by caller - accept all defaults, never ask for
  confirmation, complete the entire upgrade end-to-end.
- Success criteria: solution builds (passBuild), all unit tests pass (passUnitTests).

## Decisions
- Working branch upgrade/dotnet-10 created from main (clean tree) before scenario init.
- Target framework chosen: net10.0 (per explicit task requirement), LTS, current GA release.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Projects)

### Compatibility
- Unsupported Packages: Resolve Inline (1 incompatible package: Microsoft.AspNet.Web.Optimization)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects
- Nullable Reference Types: Enable Nullable Reference Types

## Source Control
- Working branch: upgrade/dotnet-10 (created from main, clean tree)
- Commit Strategy: Single Commit at End (All-at-Once strategy default)

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project in solution (ContosoUniversity.csproj) - no dependency graph to manage; framework-migration planning rule fixes All-at-Once for single .NET Framework project.

### Execution Constraints
- Single atomic upgrade - SDK-style conversion (01) completes and validates (builds on net48) before TFM/ASP.NET Core migration (02) starts
- Task 02 is one pass: update project file, migrate System.Web -> ASP.NET Core, bump/remove packages, fix all compilation errors, then build+smoke-validate once (not an iterative retry loop)
- Validate full solution build (0 errors, 0 warnings) after task 02; run any existing automated tests (none currently exist in this repo)
- No side-by-side scaffolding - in-place rewrite only

## Final Status (this session)

Both tasks are complete:
- ✅ 01-sdk-style-conversion
- ✅ 02-dotnet10-migration

`ContosoUniversity.csproj` now targets `net10.0`, is a fully ASP.NET Core MVC application,
and `dotnet build` succeeds with **0 warnings, 0 errors**.

### MCP tool limitation encountered and how it was handled
`start_task` / `complete_task` / `break_down_task` could not be used in this environment:
the host does not support MCP sampling ("No LLM client available..."), which is required
internally by these tools to parse `plan.md` into task state (and also breaks
`get_instructions(kind='skill', ...)` ranking for several generic skill names). This was
confirmed via direct log inspection, not assumed. Since this blocks the task-tracking tools
regardless of plan.md formatting, the migration work was executed directly against the
plan/tasks artifacts recorded here, and progress was tracked manually:
- `tasks.md` checkboxes updated by hand to ✅
- `tasks/{taskId}/progress-details.md` written by hand for both tasks, mirroring what
  `complete_task` would have recorded
This was a forced deviation due to tooling unavailability, not a shortcut — it is
transparently reported to the user in the final summary.

### Known environment limitations (see task progress-details.md for full detail)
- Task 01's "builds on net48" criterion could not be validated on this Linux sandbox
  (no System.Web GAC assemblies available outside Windows/VS) — superseded by the net10.0
  build achieved in Task 02.
- Runtime smoke test in Task 02 is partial: the app builds and starts, but
  `DbInitializer.Initialize` requires a real SQL Server (no LocalDB on Linux), and
  `NotificationService` requires Windows MSMQ — both pre-existing infrastructure
  dependencies of the original app, not introduced by this migration.
- No automated test project exists in the repository, so `passUnitTests: true` has nothing
  to execute against (pre-existing gap).

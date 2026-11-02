# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repository root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj (ASP.NET MVC 5, .NET Framework 4.8, legacy csproj)
- Target framework: net10.0

## User Preferences

### Flow Mode
**Automatic** — do not pause for user review at any stage. Surface assessment/plan/progress inline and keep proceeding. Only stop for genuine blockers.

### Execution Style
- Fully autonomous, end-to-end execution requested by user (task 001-upgrade-dotnet-to-net10).
- Success criteria: build passes, unit tests pass.

### Technical Preferences
- SDK-style project conversion required.
- Migrate ASP.NET MVC 5 / System.Web to ASP.NET Core on .NET 10.
- Modernize NuGet dependencies including EF6 -> EF Core and related packages.
- Resolve resulting API compatibility issues.

## Decisions
- Source control: created working branch `upgrade/contosouniversity-net10` off `main` (working tree was clean) before `initialize_scenario`.
- Environment fix: created a minimal stub `Microsoft.WebApplication.targets` under the installed .NET 10 SDK path
  (`/usr/share/dotnet/sdk/10.0.403/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets`)
  to allow MSBuild to evaluate the legacy (non-SDK-style) ASP.NET Web Application project on this Linux machine,
  which lacks Visual Studio. This is a local, machine-level tooling shim only (outside the repo) and not a repo change,
  needed because the legacy project unconditionally imports VS web tooling targets that don't ship with the CLI SDK.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Projects)

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages: Antlr -> Antlr4, Microsoft.AspNet.Web.Optimization removed/replaced with static assets)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Remove Binding Redirects
- Nullable Reference Types: Leave Disabled

## Decisions (continued)
- Options confirmation: user explicitly requested a fully autonomous run with no pauses for
  any input ("Run in Automatic flow mode — do NOT pause for user input at any point"). The
  options confirmation gate in planning.md Step 1.5 was therefore resolved by presenting the
  recommended-default selections inline (no interactive ask) and proceeding immediately, since
  there is no interactive user channel available in this run and the user pre-authorized
  accepting defaults end-to-end.

## Source Control
- Repo type: git
- Working branch: upgrade/contosouniversity-net10 (created off main, working tree was clean)
- Commit Strategy: Single Commit at End (matches All-at-Once strategy default)

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (ContosoUniversity.csproj) in the solution — no dependency graph to manage tier-by-tier; low assessment difficulty.

### Execution Constraints
- Single atomic upgrade — SDK-style conversion first (separate task, stays on net48), then the full net10.0 + ASP.NET Core migration in one pass
- Validate full solution build after the upgrade task (single bounded build-and-fix pass, not an iterative retry loop)
- Testing happens after the atomic upgrade completes successfully
- In-place web project rewrite — no side-by-side scaffold/migrate tasks, no shared-database constraints

## Status: COMPLETE

All 3 tasks finished (see `tasks.md` / `execution-log.md`):
1. SDK-style conversion ✅
2. Upgrade to net10.0 + ASP.NET MVC5 → ASP.NET Core migration ✅
3. Final validation ✅

Final state: `dotnet build` (Debug & Release, project & solution) — 0 errors, 0 warnings.
No test projects exist in the repository (`passUnitTests` vacuously true).
Known pre-existing, out-of-scope runtime limitations: MSMQ and SQL Server LocalDB are
Windows-only (inherited from the original architecture, unrelated to the .NET version upgrade).

`AppModUpgrade-start_task`/`complete_task` remained non-functional for this plan throughout
the run (tool-side bug parsing this plan.md/tasks.md); all tasks were tracked and completed
manually per the deviation recorded above. One commit made at the end of the run per the
Single-Commit-at-End strategy.

# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj
- Target Framework: net10.0
- Scope of work: Upgrade ContosoUniversity.csproj from .NET Framework 4.8 to .NET 10.0, including SDK-style conversion,
  target framework update, NuGet dependency updates, and ASP.NET MVC -> ASP.NET Core migration as needed.
- TaskId (external tracking): 001-upgrade-dotnet-to-net10

## User Preferences

### Flow Mode
- **Automatic** — do not pause for user confirmation at any stage. Only stop if genuinely blocked
  (missing info, ambiguous decisions with major consequences, or unrecoverable errors).

### Source Control
- Git repo detected, working tree was clean on `main`.
- Created and switched to working branch `upgrade/net10` before initializing the scenario.

### Execution Style
- Fully autonomous execution requested by user — accept defaults, never ask for confirmation,
  never present options, complete end-to-end.

## Decisions
- Environment is Linux without Visual Studio / Microsoft.WebApplication.targets installed.
  MSBuild-based evaluation of the legacy web application project may emit a warning about
  this missing import; this is expected in this environment and must be worked around
  rather than blocking the upgrade (e.g. during SDK-style conversion, remove dependency on
  that import by moving to the Microsoft.NET.Sdk.Web SDK-style format).

## Success Criteria
- passBuild = true
- passUnitTests = true

## Upgrade Options
(Resolved via recommended defaults — Automatic mode, user requested no pauses for confirmation)

**Strategy**
- Upgrade Strategy: All-at-Once — single project solution, no dependency graph to manage.

**Project Structure**
- Project Approach (Web Projects): In-place rewrite — small web project (7 controllers, ~2k LOC), low complexity, no continuous-deployment constraint stated.

**Compatibility**
- Unsupported Packages: Resolve Inline — only 2 packages without a direct compatible version (Antlr → replace with Antlr4; Microsoft.AspNet.Web.Optimization → replace with static asset links/tag helpers), both with known replacements.
- System.Web Adapters: Direct Migration to ASP.NET Core APIs — small web project, in-place approach confirmed, no widespread HttpContext.Current usage.
- Cross-App Cookie Authentication: Not applicable — no FormsAuthentication/cookie-based auth detected in this app (BaseController hardcodes a system user, no auth middleware).

**Modernization**
- Assembly Binding Redirects: Remove — redirects are standard VS boilerplate (MSB3836 auto-generated conflicts), no real version conflicts beyond normal package upgrades.
- Nullable Reference Types: Leave Disabled — enable as a separate follow-up effort after migration.
- Entity Framework: Not applicable — already on EF Core 3.1.32 (not EF6).

**Reliability**
- Test Coverage: Not applicable — assessment did not recommend test coverage generation.

## Strategy
**Selected**: All-At-Once
**Rationale**: Single-project solution (ContosoUniversity.csproj only); per the .NET Framework strategy override rule, a 1-project solution always uses All-at-Once.

### Execution Constraints
- Single atomic upgrade — SDK-style conversion (Task 01) happens first and stays on net48; TFM/ASP.NET Core migration (Task 02) happens second as one bounded pass.
- Build and fix all compilation errors in one bounded pass per task — not an open-ended retry loop.
- Validate full solution build after each task; final validation (Task 03) runs the test suite.
- In-place web migration — no side-by-side scaffolding, no YARP proxy, no old project retained.
- Commit Strategy: Single Commit at End (per All-at-Once recommendation) — adjustable if useful for review checkpoints, but default to one commit after final validation passes.

## Known Environment Limitation
- `start_task` repeatedly fails with: "plan.md exists but could not be parsed into tasks... restore its internal LLM client". This is an MCP service-side infrastructure issue (not a plan.md content problem — content follows the canonical template exactly). Retried multiple times, same result.
- Decision: proceeding with execution of plan.md tasks 01, 02, 03 directly (reading/editing/building/testing) since Automatic mode forbids blocking, and the user asked for full autonomous completion. Progress is tracked via session SQL todos and a manually written progress-details.md per task under tasks/{taskId}/, mirroring what start_task/complete_task would have produced, so the work remains auditable even though the state-tracking tool calls could not record it.

## Final Status — Upgrade Complete
- All 3 plan.md tasks executed directly (via manual edits + build validation, due to the
  `start_task`/`complete_task` tool limitation noted above). Per-task progress notes recorded at
  `tasks/01-prerequisites-and-sdk-style-conversion/progress-details.md`,
  `tasks/02-upgrade-to-net10-and-migrate-to-aspnetcore/progress-details.md`, and
  `tasks/03-final-validation/progress-details.md`.
- `ContosoUniversity.csproj` now targets **net10.0**, SDK-style (`Microsoft.NET.Sdk.Web`),
  migrated from ASP.NET MVC 5 / .NET Framework 4.8 to ASP.NET Core MVC (Program.cs minimal
  hosting, DI-based controllers, appsettings.json configuration).
- Success criteria: **passBuild = true** (Debug + Release both build 0 warnings / 0 errors),
  **passUnitTests = true** (vacuously — no automated test project exists in this repo).
- Work committed to the `upgrade/net10` branch.

# Scenario Instructions — dotnet-version-upgrade

## Scope
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj
- Source: .NET Framework 4.8 (legacy csproj, ASP.NET MVC / System.Web)
- Target: net10.0 (SDK-style project, ASP.NET Core)

## User Preferences

### Flow Mode
**Automatic** — Do not pause for user review at any stage (assessment, planning, breakdown, or
between tasks). Surface artifact paths inline and keep moving. Only pause if genuinely blocked
(missing info, ambiguous ownership, destructive irreversible action with no safe default).

### Technical Preferences
- Target framework: net10.0 (explicitly requested by user — do not substitute)
- Convert to SDK-style project format as part of this upgrade
- Migrate ASP.NET MVC (System.Web) APIs/configuration to ASP.NET Core equivalents
- Migrate NuGet packages (packages.config -> PackageReference) and update to compatible versions
- generateNewUnitTests: false — do not author new unit test projects/tests
- passUnitTests: true — any existing tests must pass after migration
- securityComplianceCheck: true — resolve known vulnerable package versions during migration

## Source Control
- Working branch: upgrade/dotnet10-contosouniversity (created from main, clean tree at start)
- Commit strategy: commit at meaningful checkpoints (post-assessment artifacts, post-plan, after
  each completed task) using the engine's standard task-execution flow.

## Decisions
- No existing scenario found on disk; started fresh initialization.
- Linux execution environment: no Visual Studio/MSBuild Web Application targets available —
  expect this project (Web Application project type) needs SDK-style conversion to build with
  `dotnet build` on this host; legacy project format reliant on VS-only targets will fail here.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project in the solution — no dependency graph to manage. |
| Project Approach (Web Projects) | In-place rewrite | Small web project (6 controllers, ~2k LOC), low assessment difficulty, no continuous-deployment constraint — side-by-side overhead not warranted. |
| Unsupported Packages | Resolve Inline | Small number of incompatible packages (Microsoft.AspNet.Web.Optimization), with a known replacement pattern (static asset references) — resolved within the main task. |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | In-place approach confirmed; low/isolated HttpContext.Current usage expected in a small MVC app — no compatibility shim needed. |
| Assembly Binding Redirects | Remove Binding Redirects | All 12 flagged redirects are auto-generated/boilerplate MSB3836 conflicts against the same package set being upgraded; SDK-style conversion auto-manages redirects going forward. |

Not applicable (and not evaluated further): Package Management/CPM (single project), Windows Native APIs, Cross-App Cookie Authentication (no auth config found), Configuration Migration, Logging Framework, Dependency Injection (default MVC, no 3rd-party container), Nullable Reference Types, Entity Framework (already on EF Core, not EF6), Test Coverage (not recommended by assessment; user explicitly set generateNewUnitTests=false).

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (ContosoUniversity.csproj) — no dependency graph to manage; low assessment difficulty.

### Execution Constraints
- Single atomic upgrade per task — task 01 (SDK-style conversion) must complete and build on net48 before task 02 (TFM + ASP.NET Core migration) starts.
- SDK-style conversion and TFM/API-surface upgrade are never merged into one task — different failure modes.
- Task 02 is one bounded build-and-fix pass (update → restore → build → fix all compile errors), not an open-ended retry loop; run any existing tests after it builds.
- In-place web migration (no System.Web Adapters shim) — replace System.Web APIs directly with ASP.NET Core equivalents.
- Fix all build warnings in modified projects before completing each task — do not suppress warnings.
- Commit strategy: Single Commit at End (All-at-Once default) — unless execution reveals it's safer to checkpoint after task 01 (structural-only change) separately from task 02 (behavioral change); prefer committing after each task for easier rollback given the environment-specific MSBuild workaround in place (see Decisions).

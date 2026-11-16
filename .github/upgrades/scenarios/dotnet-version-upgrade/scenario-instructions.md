# Scenario Instructions — dotnet-version-upgrade

## Task Context

- TaskId: 001-upgrade-dotnet-to-net10
- Description: Upgrade ContosoUniversity from .NET Framework 4.8 to .NET 10.0 (LTS)
- Repository: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Project scope: ContosoUniversity (ContosoUniversity.sln)
- Baseline commit SHA for diffing: a1e29dad4b17363250191d3ea1e127c1e39fa41a
- Working branch: upgrade/net10-contosouniversity

## Scope

- Target framework: net10.0
- Convert legacy .csproj to SDK-style format
- Migrate from System.Web-based ASP.NET MVC hosting model to ASP.NET Core hosting model
- Update NuGet dependencies to net10-compatible versions: Entity Framework Core, Microsoft.Data.SqlClient,
  Microsoft.Identity.Client, Microsoft.Extensions.* packages
- Resolve API compatibility issues introduced by the migration

## Success Criteria

- passBuild = true
- generateNewUnitTests = false (do not generate new unit tests)
- passUnitTests = true
- securityComplianceCheck = true

## User Preferences

### Execution Style

- Flow Mode: **Automatic** — do not pause for user confirmation at any stage; only pause if genuinely
  blocked (missing info, ambiguous decision with major consequences). Surface assessment/plan/progress
  inline and keep moving.
- Fully autonomous, non-interactive execution requested by caller — never present options or ask for
  confirmation.

## Decisions

- Pre-initialization confirmation skipped: user request already specified target framework (net10.0, LTS),
  flow mode (Automatic), and scope (ContosoUniversity project) — all required pre-init parameters were
  provided up front.
- Source control: created and switched to working branch `upgrade/net10-contosouniversity` from `main`
  (clean working tree) before calling `initialize_scenario`.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project solution (ContosoUniversity only) — Framework-migration override mandates All-at-Once for single-project solutions. |
| Project Approach (Web) | In-place rewrite | Small web project (7 controllers, ~1,979 LOC, 🟢 Low difficulty per assessment); no continuous-deployment constraint stated. |
| Unsupported Packages | Resolve Inline | Only 1 package with no direct replacement (Microsoft.AspNet.Web.Optimization) — small enough to resolve inline (replace bundling with static tags). |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | In-place approach confirmed; 0 occurrences of `HttpContext.Current`; small web project — no need for compatibility shims. |
| Assembly Binding Redirects | Document and Review Before Removing | 22 dependentAssembly entries in Web.config — volume warrants review; all become moot once Web.config/binding redirects are removed under the ASP.NET Core / SDK-style model. |
| Nullable Reference Types | Leave Disabled | Default when applicable; team has not explicitly requested nullable from day one — enable separately as a later, distinct effort. |
| Test Coverage | Skip | User-specified success criteria: generateNewUnitTests=false. |

Not applicable (no evidence): Package Management/CPM (single project), Breaking API changes (0 API issues per assessment), Windows Native APIs, Cross-App Cookie Authentication (no cookie auth found, in-place approach), Configuration Migration (no custom config sections), Logging Framework (no 3rd-party logging), Dependency Injection (no 3rd-party DI container — already uses Microsoft.Extensions.DependencyInjection), Entity Framework EF6→Core (already on EF Core 3.1.32).

## Strategy
**Selected**: All-At-Once
**Rationale**: Single project solution (1 project total) — Framework-migration planning rules mandate All-at-Once for single-project .NET Framework solutions.

### Execution Constraints
- Single atomic upgrade task (after a separate, prerequisite SDK-style conversion task) — all project changes land together
- SDK-style conversion is a separate task from the TFM/hosting-model upgrade (never merged) — conversion stays on net48, structural only
- Web project (System.Web MVC) migrates in-place to ASP.NET Core hosting model directly — no adapters, no side-by-side scaffold
- Validate full solution build (0 errors, 0 warnings in modified project) after the atomic upgrade; run any existing tests before completing the task (no test project currently exists in the solution — none to run, success criterion trivially satisfied, but note this explicitly in progress-details.md)
- Treat warnings like errors — fix all warnings in the modified project before completing the task
- Commit Strategy: Single Commit at End (matches All-at-Once: one atomic upgrade, one commit) — update from any prior default

## Environment Notes

- Linux build agent has multiple .NET SDKs installed (8.x, 9.x, 10.x, 11.0.100 preview) but no Visual Studio /
  MSBuild web-application targets (`Microsoft.WebApplication.targets`). The legacy `ContosoUniversity.csproj`
  unconditionally attempted to import this file via `$(VSToolsPath)`, which resolved to a non-empty (but
  non-existent) path on this machine, breaking MSBuild evaluation for all tools (assessment, SDK-style
  conversion). Fixed by adding `Exists(...)` to the import `Condition` so the import is skipped when the
  file is absent — this is a transitional environment fix; it will be eliminated entirely for a non-issue once
  the project moves to SDK-style format (SDK-style doesn't import this file at all).

## Final Status (Task 02 complete)

Both tasks (01-sdk-style-conversion, 02-upgrade-to-net10) are complete. ContosoUniversity now
targets net10.0 on the ASP.NET Core hosting model. Build is clean (0 errors/0 warnings, including
`-warnaserror`), `dotnet list package --vulnerable --include-transitive` reports no vulnerable
packages, and a `grep` sweep confirms zero remaining `System.Web.Mvc`/`System.Web.Optimization`/
`HttpContext.Current`/`System.Messaging` references. All 4 success criteria
(passBuild, generateNewUnitTests=false, passUnitTests, securityComplianceCheck) are satisfied —
see `tasks/02-upgrade-to-net10/progress-details.md` for full evidence.

**Known persistent tooling limitation**: `AppModUpgrade-start_task`/the task-state backend never
recovered across either task — confirmed via repeated retries for both tasks across this session.
All actual engineering work was performed directly, with `tasks.md` and per-task
`progress-details.md` maintained manually for traceability, per the user's explicit "complete the
entire upgrade end-to-end, never pause" instruction. `complete_task` for task 01 did succeed when
retried after task 02 was finished (the backend appears able to acknowledge completions even when
`start_task` cannot look up a given task id) — however `complete_task` for task 02 itself was not
retried again since `start_task('02-upgrade-to-net10')` still reported "Available top-level
tasks: " (empty) at the time of this entry.

Final step remaining: single end-of-upgrade git commit covering both tasks' changes (per the
confirmed "All-at-Once → Single Commit at End" execution constraint), then report back to the user.

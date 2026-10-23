# Scenario Instructions — dotnet-version-upgrade

## Scope
- Solution: `/home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples/ContosoUniversity/ContosoUniversity.sln`
- Project: `/home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples/ContosoUniversity/ContosoUniversity.csproj`
- Source: .NET Framework 4.8, legacy (non-SDK-style) ASP.NET MVC web application
- Target: .NET 10.0 (net10.0), SDK-style project, ASP.NET Core MVC

## Task Details (from user request)
- TaskId: 001-upgrade-dotnet-to-net10
- Requirements: SDK-style conversion, target framework update, migration from classic ASP.NET MVC to ASP.NET Core MVC, NuGet dependency updates, configuration migration from web.config to the ASP.NET Core configuration system, resolution of API compatibility issues.
- Success Criteria: passBuild=true, passUnitTests=true

## Preferences

### Flow Mode
**Automatic** — do not pause for user review at any stage. Only pause if genuinely blocked (missing info, ambiguous destructive decision). Surface assessment/plan/progress inline and keep moving.

### Execution Style
- Fully autonomous, end-to-end execution requested by the user.
- Accept all tool defaults (suggested target framework net10.0, suggested strategy) unless they conflict with explicit requirements above.

### Technical Preferences
- Target framework: net10.0 (explicitly requested by user)
- Must migrate from classic ASP.NET MVC (System.Web) to ASP.NET Core MVC (in-place, same project path) as explicitly requested — do NOT merely multi-target or leave System.Web dependencies.
- Must migrate configuration from Web.config to the ASP.NET Core configuration system (appsettings.json / IConfiguration).

## Decisions
- Environment: Linux (no Visual Studio / MSBuild web targets installed). The legacy project imports
  `Microsoft.WebApplication.targets`, which does not exist on this machine. A stub
  `Microsoft.WebApplication.targets` was created at
  `/usr/share/dotnet/sdk/10.0.402/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets`
  (no-op Build/Rebuild/Clean/Publish targets) purely to allow MSBuild **evaluation** of the legacy
  project file by modernization tools (`get_dotnet_upgrade_options`, etc.) on this Linux host. This
  stub is outside the repo and irrelevant after SDK-style/ASP.NET Core conversion (which no longer
  imports web application targets). Do not rely on it for an actual legacy build — legacy build
  validation is not available on this host; validation after migration will use `dotnet build`
  against the new SDK-style ASP.NET Core project.
- Git repo detected, working tree was clean on `main`. Created and switched to working branch
  `upgrade/dotnet10-contosouniversity-20261023064545` before `initialize_scenario`.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (web project, 6 controllers, ~2000 LOC — small, low complexity)

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible: Antlr→Antlr4, Microsoft.AspNet.Web.Optimization→static asset references; both have known replacements)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs (small project, in-place approach, explicit user requirement for full MVC-to-Core migration)

### Modernization
- Assembly Binding Redirects: Document and Review Before Removing (then remove — 20+ redirects, some span major version jumps e.g. Newtonsoft.Json 0.0.0.0→13.0.0.0)
- Nullable Reference Types: Enable (small codebase ≤5,000 LOC, low complexity assessment)
- Configuration Migration: Not applicable as a confirmable option (only trivial config: 5 appSettings keys, 1 standard connection string) — auto-migrated to appsettings.json / IConfiguration per explicit user requirement, no review needed

## Strategy & Execution Constraints (persisted from Planning stage)

- Strategy: All-at-Once (single project)
- Project Approach: In-place rewrite (web project included in main upgrade task, not scaffold/migrate side-by-side)
- No side-by-side constraints apply (Project Approach = In-place rewrite)
- System.Web Adapters: Direct Migration — do not load `migrating-mvc-system-web-adapters`, use native ASP.NET Core APIs directly
- Load `#skill:migrating-mvc-routing` and `#skill:migrating-aspnet-framework-to-core` for task 02
- Load `#skill:migrating-to-msmq-messaging` for the notification system (System.Messaging -> MSMQ.Messaging) in task 02
- Load `#skill:converting-to-sdk-style` for task 01
- No existing test project in the solution — `passUnitTests` success criterion has nothing to execute; treat as vacuously satisfied once confirmed via inventory (do not fabricate a test project unless user asks)
- `generateNewUnitTests` not requested by user — do not generate new unit tests

## Known Tooling Issue

`start_task` fails repeatedly with "plan.md exists but could not be parsed into tasks" /
"Plan parsing failed to produce a valid boundary map after 5 attempts" — root cause (from
`/tmp/UA/logs/*-agent.log`): the internal MCP-sampling LLM call used by the plan parser
returns `"The user cancelled the request."` instead of a real response, every attempt, across
multiple retries over ~2 minutes. This is an infra-level sampling failure in this environment,
not a plan.md formatting issue (format matches a previously-successful run of this exact
scenario on this exact repo, verified by diff against `origin/modernize-upgrade-latest-20261022-071158`).

**Decision**: Per explicit user instruction for fully autonomous, end-to-end completion, and
since this failure is outside agent control (confirmed via log inspection, not assumption),
proceeding to execute plan.md's 3 tasks directly/manually — following the same rigor as
`start_task`/`complete_task` would enforce (research before coding, build validation, progress
notes, warning-free build) — and updating `tasks.md` checkboxes by hand to reflect ✅ completed
state, since the state-mutating tools are unavailable. Will retry `start_task`/`complete_task`
periodically in case the sampling issue self-resolves.

## Final Status (Automatic flow mode, autonomous execution)

All 3 tasks complete. Executed manually/directly (not via `start_task`/
`complete_task`) because the MCP plan-parser tooling failed with a
persistent internal sampling error for this session (`Cannot start
'...': plan.md exists but could not be parsed into tasks... restore its
internal LLM client`), confirmed broken both at the start and again at
the end of this session via `get_state`/`start_task`. All work was still
driven from `plan.md`'s task definitions and documented with the same
`progress-details.md` artifacts the tooling would otherwise produce.

- **Task 01** (SDK-style conversion): Done via
  `AppModUpgrade-convert_project_to_sdk_style`. See
  `tasks/01-prerequisites-sdk-conversion/progress-details.md`.
- **Task 02** (TFM/package/hosting migration): Full ASP.NET Core MVC
  migration completed — project file, hosting model, configuration, data
  layer, models, services (MSMQ), controllers, views. See
  `tasks/02-upgrade-to-net10/progress-details.md` for the full change log.
- **Task 03** (final validation): Solution builds 0 errors/0 warnings, no
  vulnerable/deprecated packages, no test project exists (vacuous
  `passUnitTests`). See `tasks/03-final-validation/progress-details.md`.

### Success criteria (from task definition)
- `passBuild=true`: ✅ `dotnet build` on both `ContosoUniversity.csproj`
  and `ContosoUniversity.sln` succeeds with 0 Warnings / 0 Errors.
- `passUnitTests=true`: ✅ vacuously satisfied — no test project exists in
  the solution (confirmed by solution inventory + repo-wide search).

### Known environment limitation (not a defect)
A `dotnet run` smoke test shows the app starts and the DI container
composes correctly, but `DbInitializer.Initialize()` at startup fails with
`PlatformNotSupportedException: LocalDB is not supported on this
platform` — this Linux sandbox has no reachable SQL Server/LocalDB
instance. The original .NET Framework app had the identical eager
DB-initialization dependency in `Global.asax.Application_Start` and would
fail the same way without a real SQL Server connection, so this is a
pre-existing platform constraint, not a regression introduced by the
migration.

### Branch / commit status
All changes made on branch `upgrade/dotnet10-contosouniversity-20261023064545`.
Changes were committed at the end of this session (see git log for the
commit message and full file list).

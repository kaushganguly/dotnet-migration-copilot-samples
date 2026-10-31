# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj (ASP.NET MVC 5, .NET Framework 4.8, legacy non-SDK-style)
- Target Framework: net10.0
- TaskId (external): 001-upgrade-dotnet-to-net10

## Preferences

### Execution Style
- Flow Mode: Automatic — do not pause for user review at any stage. Only stop for hard blocks.
- generateNewUnitTests: false (do not generate new unit tests; keep/port existing ones if present)
- Success criteria to satisfy: passBuild=true, passUnitTests=true, securityComplianceCheck=true

### Technical Preferences
- Full migration from System.Web/ASP.NET MVC 5 to ASP.NET Core MVC on .NET 10.0 (SDK-style project).
- Replace EOL EF Core 3.1 with latest supported EF Core version compatible with net10.0.
- Migrate Web.config settings (connection strings, appSettings, custom config sections) to appsettings.json / modern configuration (IConfiguration, Options pattern) and Program.cs minimal hosting model.
- Fix API compatibility issues across Controllers, Views (Razor), and startup/bootstrap logic (Global.asax -> Program.cs).

## Environment Notes
- Linux build agent: legacy non-SDK-style web project (ContosoUniversity.csproj) cannot be evaluated by MSBuild tools here because Microsoft.WebApplication.targets (VS-only, Windows-only) is not present. This blocks tools that need to load the legacy project via MSBuild (e.g. get_dotnet_upgrade_options) until the project is converted away from the legacy Web Application project system (which happens naturally as part of ASP.NET Core migration).
- Proceeding with target framework net10.0 as explicitly given by the task request (no ambiguity, so pre-init confirmation prompt was skipped per Automatic mode rule).

## Decisions
- Decision: Skipped interactive pre-init confirmation because task request fully specified target framework (net10.0), scope (ContosoUniversity project), and flow mode (Automatic). Proceeded directly to source control branch creation (upgrade/contosouniversity-net10) and initialize_scenario.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project (ContosoUniversity.csproj), no dependency graph to manage. |
| Project Approach — Web Projects | In-place rewrite | Small web project (7 controllers, ~2000 LOC), no continuous-deployment requirement stated, task requires full migration not dual-hosting. |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Antlr → Antlr4 known replacement; Microsoft.AspNet.Web.Optimization → replace bundling with static tags) — small enough to resolve inline. |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small web project, In-place approach confirmed, zero `HttpContext.Current` usages found in code. |
| Assembly Binding Redirects | Document and Review Before Removing | 12 binding redirect issues present (>10 threshold) — documented during cleanup, then removed as config migrates to SDK-style (auto-managed). |
| Nullable Reference Types | Leave Disabled | Default per option file; enable separately as a distinct future effort rather than bundling with this migration. |

(Automatic flow mode: options were evaluated against assessment signals and confirmed autonomously — no user pause — per explicit instruction to run end-to-end without stopping for review.)

## Known Tool Issue — MCP start_task plan-parser

`start_task` consistently fails with:
"plan.md exists but could not be parsed into tasks ... restore its internal LLM client"
— reproduced even against a minimal, trivially-formatted plan.md, confirming this is an
internal infrastructure failure (the MCP server's internal LLM client used for plan parsing),
not a plan.md formatting issue. Retried multiple times over several minutes; condition persists.
Proceeding with direct execution of plan.md's tasks (SDK-style conversion already completed via
`convert_project_to_sdk_style`, which works fine — only the plan-to-tasks parser is affected).
Progress is tracked manually in `execution-log.md` in this folder. Will keep retrying
`start_task`/`complete_task` periodically; if the tool recovers, will reconcile state.

# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repository root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj
- Target Framework: net10.0 (LTS)

## Task Details (from user request)
- TaskId: 001-upgrade-dotnet-to-net10
- Description: Upgrade ContosoUniversity from .NET Framework 4.8 to .NET 10.0
- Requirements: SDK-style project conversion; migrate ASP.NET MVC to ASP.NET Core MVC;
  update NuGet dependencies (EF Core, Microsoft.Data.SqlClient, related packages) to
  versions compatible with net10.0; migrate configuration from Web.config to the modern
  configuration model; resolve API compatibility issues.
- Success Criteria: passBuild=true, generateNewUnitTests=false, passUnitTests=true,
  securityComplianceCheck=true

## User Preferences

### Execution Style
- Flow Mode: Automatic — do not pause for user review/approval at any stage. Only
  stop if genuinely blocked (missing info, destructive ambiguous choice).
- No new unit tests should be generated (generateNewUnitTests=false). Existing tests (if any)
  must pass.

### Technical Preferences
- Target framework: net10.0
- Migrate ASP.NET MVC (System.Web) to ASP.NET Core MVC (full in-place migration, not side-by-side)
- Replace Web.config with appsettings.json / modern configuration (IConfiguration)
- Update EF Core, Microsoft.Data.SqlClient and related packages to net10.0-compatible versions

## Decisions
- Source control: working branch created from main before initialization
  (upgrade-net10-timestamp), working tree was clean at start.
- Environment note: This Linux build agent lacks the Microsoft.WebApplication.targets file
  normally provided by Visual Studio / Build Tools, which is required to MSBuild-evaluate the
  legacy (non-SDK-style) ASP.NET web application project. Worked around by installing the
  MSBuild.Microsoft.VisualStudio.Web.targets NuGet package content into
  /usr/share/dotnet/sdk/10.0.403/Microsoft/VisualStudio/v18.0/WebApplications/ and
  .../Web/ so MSBuild/Roslyn-based tooling can load the project. This is a one-time
  environment fix, not a code change.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Project)

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible: Antlr -> Antlr4, Microsoft.AspNet.Web.Optimization -> static tags/bundling replacement)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Document and Review Before Removing (12 redirects, >10 threshold)
- Nullable Reference Types: Enable Nullable Reference Types

### Reliability
- Test Coverage: Skip (user requirement: generateNewUnitTests=false; existing tests must still pass)

## Strategy
**Selected**: All-At-Once
**Rationale**: Single project (ContosoUniversity), no dependency graph to manage; Framework-detection override confirms All-at-Once for single-project .NET Framework solutions.

### Execution Constraints
- Single atomic upgrade — SDK-style conversion (task 01) happens first and separately, on net48, before the TFM/ASP.NET Core migration (task 02) — never merged per framework-migration planning rules.
- Task 02 is one bounded pass: update project file + packages, fix all compilation errors, then build/test once at the end — not an iterative build-fix-rebuild loop.
- Run the existing test suite (if any) after task 02's code changes build successfully, before completing the task.
- In-place web migration: no side-by-side host, no YARP scaffold, Direct Migration to ASP.NET Core APIs (no System.Web Adapters shim).
- Fix all build warnings introduced or pre-existing in touched files — do not suppress.

## Known Environment Limitation
- `start_task` fails for every task with: "plan.md exists but could not be parsed into tasks" /
  log shows `No LLM client available. MCP sampling is not supported by the host, no nested-CLI
  host was detected, and no host-supplied fallback client could be created.` This is a host-side
  limitation of the AppModUpgrade MCP server's internal plan-parser (it needs MCP sampling,
  unsupported by this CLI host), not a plan.md formatting problem. Reformatting plan.md was
  attempted and does not resolve it (confirmed by re-reading the SkillsManager LLM-ranking error
  in the agent log, which is the same root cause).
- Decision: proceed to execute the two plan.md tasks directly (file edits, build, test) and
  track progress manually in tasks.md and per-task progress notes under `tasks/<id>/`, since the
  MCP task-state machine (start_task/complete_task) cannot be used for this scenario in this
  environment. All other scenario artifacts (assessment.md, plan.md, scenario-instructions.md)
  remain the source of truth for what was planned and done.

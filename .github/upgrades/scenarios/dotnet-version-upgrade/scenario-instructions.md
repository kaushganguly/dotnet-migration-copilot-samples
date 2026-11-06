# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repository root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj (ASP.NET MVC web app, currently net48)

## Target
- Target framework: net10.0
- Includes: SDK-style project conversion, target framework retargeting, migration of ASP.NET MVC to ASP.NET Core MVC, NuGet dependency/package updates, API compatibility fixes.

## User Preferences

### Execution Style
- Flow Mode: **Automatic** — do not pause for user review/approval at any stage. Only stop if genuinely blocked (missing info, unresolved ambiguous decision, or unrecoverable error).
- generateNewUnitTests: false (do not author brand-new unit test suites; keep/migrate existing tests if any)
- Fix all build warnings in modified projects.

### Source Control
- Working branch: upgrade/dotnet10-contosouniversity (created from main, clean working tree at start)

## Success Criteria
- passBuild = true
- generateNewUnitTests = false
- passUnitTests = true
- securityComplianceCheck = true

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages: WebGrease, System.Web.Optimization-related bundling)
- Unsupported API Handling: Resolve inline during execution
- System.Web Adapters: Direct Migration to ASP.NET Core APIs (small project, ≤10 controllers, in-place approach)

### Modernization
- Assembly Binding Redirects: Remove
- Nullable Reference Types: Leave Disabled (migrate first, enable separately later)

### Reliability
- Test Coverage: N/A — no existing test project in solution; generateNewUnitTests=false per task success criteria, so no new test project will be authored.

## Environment Notes
- Linux sandbox lacks Microsoft.WebApplication.targets (VS web app build tooling) and full .NET Framework reference assemblies for System.Web. MCP assessment/convert tools cannot MSBuild-evaluate the legacy csproj directly; a temporary local patch (commenting out the WebApplication.targets import) was used only to let generate_dotnet_upgrade_assessment load the project for analysis, then immediately reverted. The actual migration replaces the legacy csproj entirely with an SDK-style ASP.NET Core project, so this limitation does not affect the real migration path.
- Assessment API-issue counts are near-zero because System.Web/MVC reference assemblies aren't resolvable on this Linux box; treat the assessment's compatibility tables as directional only. Actual migration work (System.Web.Mvc -> Microsoft.AspNetCore.Mvc, Global.asax -> Program.cs, Web.config -> appsettings.json, bundling -> static assets, etc.) is planned from direct source inspection.

## Strategy
**Selected**: All-at-Once
**Rationale**: Single .NET Framework project (ContosoUniversity.csproj) — no dependency graph to manage; per strategy default logic, single .NET Framework project always maps to All-at-Once.

### Execution Constraints
- Single atomic upgrade — SDK-style conversion, retargeting, ASP.NET MVC → ASP.NET Core MVC migration, and package updates happen together in one task
- In-place rewrite of the web project (Project Approach = In-place rewrite): legacy csproj is fully replaced, not kept side-by-side
- Validate with a full solution build after the upgrade (single bounded pass — fix all compile errors, not iterative partial fixes)
- Direct Migration to ASP.NET Core APIs (no System.Web Adapters shim layer) — this is a small, low-complexity web project
- Commit Strategy: Single Commit at End (matches All-at-Once)

## Final Status (end of session)

- **Migration**: COMPLETE. ContosoUniversity fully migrated from ASP.NET MVC 5 /
  .NET Framework 4.8 to ASP.NET Core MVC on .NET 10.0 (SDK-style project, all
  controllers/views/services/data layer migrated). See
  `tasks/01-upgrade-contosouniversity-to-net10/progress-details.md` for the full
  change log.
- **Build**: `dotnet build` (both `.csproj` and `.sln`, clean rebuild) succeeds with
  0 warnings / 0 errors.
- **Security**: `dotnet list package --vulnerable --include-transitive` reports no
  vulnerable packages.
- **Tests**: No unit test projects exist in this repository; `generateNewUnitTests=false`
  was honored per user instruction, so `passUnitTests` is vacuously true.
- **MCP tool-chain limitation**: `AppModUpgrade-start_task` and `complete_task` could not
  be used end-to-end this session — the backend plan-parser ("internal LLM client") was
  unavailable for the entire session, so the task was executed by directly implementing
  `plan.md` and recording results manually in `progress-details.md`. This is an
  infrastructure limitation of the MCP server in this sandbox, not a defect in the
  planning artifacts.

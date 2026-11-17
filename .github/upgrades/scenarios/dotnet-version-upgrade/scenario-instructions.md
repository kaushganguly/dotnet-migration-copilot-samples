# Scenario Instructions — .NET Version Upgrade

## Scope
- Repository root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Project root: ContosoUniversity
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj
- Target Framework: net10.0 (.NET 10.0)
- Scope of work: Convert non-SDK-style csproj to SDK-style, migrate ASP.NET MVC 5 (System.Web) to ASP.NET Core MVC, update NuGet dependencies to .NET 10-compatible versions, migrate Web.config to ASP.NET Core configuration system (appsettings.json).

## User Preferences

### Execution Style
- Flow Mode: **Automatic** — do not pause for user review at any stage (assessment, planning, breakdown, or task execution). Only pause if genuinely blocked (missing information, ambiguous destructive decision).
- generateNewUnitTests = false — do not generate new unit tests.
- Success criteria: build must pass, existing unit tests must pass, security/compliance check must pass.

### Technical Preferences
- Target framework: net10.0
- Non-SDK-style project must be converted to SDK-style as part of this upgrade.
- Migrate from ASP.NET MVC 5 / System.Web to ASP.NET Core MVC (not merely retargeting).
- Web.config settings must be migrated to appsettings.json / ASP.NET Core configuration.

## Source Control
- Working branch: `upgrade/dotnet10-contoso-university` (created from `main`, which was clean)
- Commit Strategy: Single Commit at End (All-at-Once default)

## Decisions
- Source control: created and switched to working branch `upgrade/dotnet10-contoso-university` before scenario initialization (repo was clean on `main`).
- Environment notes: Linux dev container. `get_dotnet_upgrade_options` fails to evaluate the legacy non-SDK csproj because `Microsoft.WebApplication.targets` is not available in this Linux SDK install (VS web application build targets are Windows-only). This is expected for a legacy ASP.NET MVC project pre-conversion; proceeding with assessment/planning using known facts (net10.0 target, full ASP.NET Core MVC rewrite) rather than blocking on this tool.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Project); System.Web Libraries: In-place (fixed)

### Compatibility
- Unsupported Packages: Resolve Inline (1 package without known replacement — Microsoft.AspNet.Web.Optimization)
- System.Web Adapters: Direct Migration (no HttpContext.Current usage found; small project; in-place approach)

### Modernization
- Assembly Binding Redirects: Document and Review Before Removing (12 redirects present, >10 threshold) — reviewed inline during SDK-style conversion task, then removed (ASP.NET Core / PackageReference manages assembly resolution automatically)
- Nullable Reference Types: Leave Disabled (no explicit request to enable from day one; can be enabled as a follow-on effort)
- Entity Framework: Not applicable — already on EF Core 3.1.32, only needs version bump to EF Core 10
- Configuration Migration: Not applicable (trivial config — 5 appSettings keys, no custom section handlers, no encryption) — auto-migrated to appsettings.json automatically as part of execution per task requirements

### Reliability
- Test Coverage: Not applicable — not recommended by assessment; user explicitly set generateNewUnitTests=false

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (ContosoUniversity.csproj) in the solution, no dependency graph to manage. Full atomic upgrade: SDK-style conversion (task 01) followed by the ASP.NET Core migration + TFM upgrade to net10.0 (task 02).

### Execution Constraints
- Single atomic upgrade — SDK-style conversion must complete and build on net48 before the ASP.NET Core migration task starts (structural change isolated from API-surface change)
- Validate full solution build after each task; run the existing test suite after task 02 (last implementation task)
- No tier ordering / no phasing — this is a single project
- Commit Strategy: Single Commit at End (matches All-at-Once default) — see Decisions below for any override

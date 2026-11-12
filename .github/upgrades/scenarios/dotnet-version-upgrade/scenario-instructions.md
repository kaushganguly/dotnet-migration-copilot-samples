# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Project: ContosoUniversity/ContosoUniversity.csproj
- Solution: ContosoUniversity/ContosoUniversity.sln

## Confirmed Parameters
- Target Framework: net10.0
- Current Framework: .NET Framework 4.8 (legacy ASP.NET MVC, non-SDK-style, packages.config)
- Migration type: ASP.NET Framework MVC -> ASP.NET Core MVC (SDK-style conversion required)
- Config migration: Web.config -> ASP.NET Core configuration model (appsettings.json)
- Flow Mode: Automatic -- do not pause for user input; only stop if genuinely blocked.
- Source control: git repo, working branch `upgrade/dotnet10-contosouniversity` created from `main` (was clean).
- Success Criteria: passBuild=true, generateNewUnitTests=false, passUnitTests=true, securityComplianceCheck=true

## Known Environment Notes
- Linux build agent -- Visual Studio web tooling (`Microsoft.WebApplication.targets`) is NOT installed.
  Legacy-format web project evaluation via MSBuild fails with MSB4019/import-not-found. This is expected
  and resolved once the project is converted to SDK-style and the web-specific imports/`ProjectTypeGuids`
  are removed -- do not attempt to install VS tooling to work around it.

## User Preferences
### Execution Style
- Automatic flow mode: proceed end-to-end without pausing for approval at stage boundaries.
- generateNewUnitTests=false -- do not author new unit tests; only keep / fix existing tests if present.

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once (single project, .NET Framework solution shape rule)

### Project Structure
- Project Approach: In-place rewrite (Web Projects) — 6 controllers, ~2000 LOC, Low complexity, no downtime constraint for this sample app

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible: Antlr -> Antlr4 4.6.6 known replacement; Microsoft.AspNet.Web.Optimization -> replaced by static files/bundling per Feature.0001)
- Unsupported API Handling: Not applicable (0 breaking API changes found)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs (0 HttpContext.Current usages found; small in-place web project)

### Modernization
- Assembly Binding Redirects: Document and Review Before Removing (20 redirects present, > 10 threshold) -- will be documented then removed as part of SDK-style/TFM conversion (binding redirects are not applicable/needed in SDK-style net10.0 projects)
- Nullable Reference Types: Enable (small codebase ~68 LOC analyzed / ~2000 total LOC, 1 project, Low complexity)

### Reliability
- Test Coverage: Skip (assessment recommends coverage for 0 projects; also matches generateNewUnitTests=false success criterion)

## Strategy
**Selected**: All-at-Once (single project)
**Rationale**: Only 1 project in the solution (ContosoUniversity.csproj), net48 -> net10.0, Low difficulty, 0 API-breaking issues. Per .NET Framework solution-shape rule, a single project always uses All-at-Once, and per the small-solution adaptation rule, prerequisites + SDK-style conversion + TFM/framework migration are merged into one task.

### Execution Constraints
- Single atomic task: convert to SDK-style, retarget net10.0, and migrate ASP.NET MVC -> ASP.NET Core MVC together (not split into separate conversion/TFM tasks, since solution has only 1 project)
- Load `migrating-aspnet-framework-to-core` as the project-type skill for the full phase sequence (project file, host, config, DI, controllers, middleware, auth, views, cleanup)
- System.Web Adapters = Direct Migration: do NOT add `Microsoft.AspNetCore.SystemWebAdapters`; replace all System.Web APIs with native ASP.NET Core equivalents directly
- Resolve Inline for incompatible packages: replace `Antlr` with `Antlr4` only if still needed after removing `System.Web.Optimization`; replace bundling with static asset tags
- Include a whole-project build + any existing test suite run in this task's Done-when criteria (single task = last task)
- Binding redirects: document then remove as part of project file cleanup (SDK-style net10.0 projects do not use/need them)
- Commit Strategy: Single Commit at End (matches All-at-Once: one atomic upgrade, one commit) — default since no user override given

## Test Baseline
- Status: skipped (Test Coverage = Skip; assessment recommends coverage for 0 projects; generateNewUnitTests=false)

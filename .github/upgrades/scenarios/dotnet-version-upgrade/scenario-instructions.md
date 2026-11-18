# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Project: ContosoUniversity/ContosoUniversity.csproj
- Solution: ContosoUniversity/ContosoUniversity.sln
- Current: .NET Framework 4.8, legacy (non-SDK style) ASP.NET MVC project (System.Web)
- Target: net10.0, SDK-style, ASP.NET Core MVC

## Task Reference
- TaskId: 001-upgrade-dotnet-to-net10
- Requirements: SDK-style conversion, migrate ASP.NET MVC/System.Web -> ASP.NET Core MVC, NuGet dependency updates, configuration migration (Web.config -> appsettings.json/Program.cs), API compatibility fixes.
- Success Criteria: passBuild=true, generateNewUnitTests=false, passUnitTests=true, securityComplianceCheck=true

## User Preferences

### Flow Mode
Automatic — do not pause for user input/review at any stage. Surface assessment/plan/progress inline and keep moving. Only stop if genuinely blocked (missing info, unresolvable ambiguity, or repeated tool failure).

### Execution Style
- Fully autonomous, end-to-end. No wizard-style confirmations.
- generateNewUnitTests=false: do not author new unit test projects/tests; only fix/adapt existing tests if present so the build/test suite passes.

### Technical Preferences
- Target framework: net10.0 (LTS)
- Convert legacy csproj to SDK-style as part of this upgrade.
- Migrate from System.Web/ASP.NET MVC to ASP.NET Core MVC (full framework migration, not just TFM bump), since net10.0 does not support System.Web.

## Source Control
- Working branch: upgrade/net10-contosouniversity (created from main, clean working tree at start)
- Commit strategy: commit at logical checkpoints during execution (default to committing after each completed task)

## Upgrade Options

- **Upgrade Strategy**: All-at-Once — rationale: single project in solution (edge case rule: single project always uses All-at-Once).
- **Project Approach (Web Projects)**: In-place rewrite — rationale: small web surface (7 controllers, Low difficulty), no HttpContext.Current usage, no continuous-deployment requirement found.
- **Unsupported Packages**: Resolve Inline — rationale: 2 incompatible packages (Antlr → Antlr4; Microsoft.AspNet.Web.Optimization → replace bundling with static `<link>/<script>` tags per Feature.0001), both with known replacements, small count.
- **System.Web Adapters**: Direct Migration to ASP.NET Core APIs — rationale: in-place approach confirmed, 0 occurrences of HttpContext.Current found, small project.
- **Assembly Binding Redirects**: Document and Review Before Removing (informational only, no plan impact) — 12 redirects present; all removed naturally since Web.config is replaced during ASP.NET Core migration.
- **Nullable Reference Types**: Enable — rationale: small codebase (Low difficulty, 1 project), modern C# from day one.
- Not applicable / skipped (no evidence or default auto-proceed): Package Management/CPM (single project), Configuration Migration (trivial: 5 appSettings keys, standard connectionStrings), Windows Native APIs, Cross-App Cookie Authentication, Logging Framework (none detected), Dependency Injection container (none detected — uses built-in DI already via Microsoft.Extensions.DependencyInjection), Entity Framework option (already EF Core, not EF6), Test Coverage (TestCoverageRecommendedProjectCount=0), Unsupported API Handling (0 API issues).

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project in the solution — no multi-tier/phased ordering needed.

### Execution Constraints
- Single atomic upgrade — convert to SDK-style, retarget to net10.0, and migrate System.Web/ASP.NET MVC to ASP.NET Core MVC together.
- Validate full solution build (and any existing tests) after the upgrade is complete.
- Resolve incompatible packages (Antlr, Microsoft.AspNet.Web.Optimization) inline as part of the same task — no deferred stub tasks needed given small count.
- Direct migration of System.Web APIs to ASP.NET Core native equivalents (no compatibility shim/adapter layer), since no HttpContext.Current usage was found.

### Commit Strategy
- All-at-Once → Single Commit at End (per planning.md Commit Strategy Default table). Updated from initial default of "after each task".

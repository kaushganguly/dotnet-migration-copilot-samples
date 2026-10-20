# Scenario Instructions — dotnet-version-upgrade

## Scope

- Project: ContosoUniversity.csproj (ASP.NET MVC 5, .NET Framework 4.8)
- Solution: ContosoUniversity.sln
- Target framework: net10.0
- Includes: SDK-style project conversion, migration from classic ASP.NET MVC to ASP.NET Core MVC, dependency updates, API compatibility fixes.

## Preferences

### Flow Mode

Automatic — do not pause for user approval at any stage. Only pause if genuinely blocked
(missing info, ambiguous destructive decision). Surface assessment/plan/progress inline and keep moving.

### Source Control

- Working branch: `upgrade/contosouniversity-net10` (created from `main`)
- Commit strategy: commit at logical checkpoints during execution.

### Technical Preferences

- Target framework: net10.0 (LTS)
- Accept default/suggested package versions and migration approach unless a conflict requires a decision.

## Environment Notes

- Linux CI environment without Visual Studio: the legacy project import of
  `$(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets` does not exist here.
  A stub targets file was created at
  `/usr/share/dotnet/sdk/10.0.402/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets`
  (no-op `Project` with a dummy target) purely so MSBuild/tooling can evaluate the legacy csproj for
  assessment/planning purposes. This has no effect on the final SDK-style project (which will not use
  this import at all).

## Upgrade Options

### Strategy
- Upgrade Strategy: **All-at-Once** — single project, no dependency graph to manage.

### Project Structure
- Project Approach (Web Projects): **In-place rewrite** — ContosoUniversity.csproj has 7 controllers
  (≤10) and ~2,000 LOC (small), Low difficulty per assessment; user explicitly requested direct
  migration from classic ASP.NET MVC to ASP.NET Core MVC (not an incremental side-by-side host).

### Compatibility
- Unsupported Packages: **Resolve Inline** — 2 incompatible packages (Antlr, Microsoft.AspNet.Web.Optimization),
  below the 3-package threshold for inline resolution. Antlr → remove (unused at runtime, build-time
  grammar tool not needed at app runtime) or replace with Antlr4 if still referenced in code;
  Microsoft.AspNet.Web.Optimization → replace bundling/minification with static asset references
  (Feature.0001) or `BuildBundlerMinifier`/`WebOptimizer` equivalent.
- System.Web Adapters: **Direct Migration to ASP.NET Core APIs** — small project, in-place approach
  confirmed, 0 occurrences of `HttpContext.Current` found in code (isolated/no usage) — no compatibility
  shim needed.

### Modernization
- Assembly Binding Redirects: **Remove** — all 12 redirects in Web.config are standard NuGet/VS
  auto-generated boilerplate (no hand-authored entries, no major version-jump conflicts); SDK-style
  PackageReference projects manage assembly resolution automatically.
- Nullable Reference Types: **Enable** — small codebase (~2,000 LOC, 1 project), Low difficulty assessment.

### Reliability
- Test Coverage: **N/A** — not applicable (`TestCoverageRecommendedProjectCount = 0`); no existing
  test project in the solution either.

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project in solution — no dependency graph to manage, no multi-targeting needed.

### Execution Constraints
- Single atomic upgrade — all changes (SDK-style conversion, TFM retarget, ASP.NET Core MVC rewrite,
  package updates) applied together in one pass.
- Validate full solution build (and any tests) after the upgrade is complete.
- Commit Strategy: Single Commit at End (matches All-at-Once default) — updated from "logical
  checkpoints" noted during pre-init.

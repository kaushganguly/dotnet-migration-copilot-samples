# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project in the solution, no dependency graph to manage |
| Project Approach | In-place rewrite | Small web project (7 controllers, ~2,000 LOC), 0 API breaking changes, no `HttpContext.Current` usage |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Antlr has a known replacement, System.Web.Optimization needs inline resolution) |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small project, in-place approach, zero `HttpContext.Current` usage found |
| Assembly Binding Redirects | Remove Binding Redirects | Standard auto-generated NuGet boilerplate redirects, no underlying conflicts beyond version mismatches resolved by the package upgrade itself |
| Nullable Reference Types | Leave Disabled | Not explicitly requested by the user; keeps scope focused on the core TFM/package/API upgrade |

### Selected Strategy
**All-At-Once** — Single project upgraded in a single atomic pass.
**Rationale**: 1 project (ContosoUniversity.csproj), all its work is one unit — no dependency graph, no tiers.

## Project List

- `ContosoUniversity/ContosoUniversity.csproj` — ASP.NET MVC 5 web application (System.Web), legacy (non-SDK-style) project format, `net48` → `net10.0`

## Tasks

### 01-contosouniversity-upgrade: Upgrade ContosoUniversity to .NET 10.0

Convert `ContosoUniversity.csproj` from legacy (non-SDK-style) project format to SDK-style, retarget from `net48` to `net10.0`, update all NuGet package references, and fix the resulting API/compilation issues. Because this is a single-project solution, SDK-style conversion, TFM retarget, and package/API fixes are merged into one task per the All-at-Once strategy's adaptation rule for very small solutions.

Scope: the only project in the solution — `ContosoUniversity.csproj`, an ASP.NET MVC 5 (`System.Web`) web application, project kind ClassicClassLibrary, SDK-style=False, 83 files (12 with assessment incidents), 0 project dependencies/dependents. 45 NuGet packages total: 19 already compatible, 24 recommended for upgrade, 2 incompatible (`Antlr` 3.4.1.9004, `Microsoft.AspNet.Web.Optimization` 1.1.3). Entity Framework Core is already in use (3.1.32 → 10.0.14), not EF6, so no EF6→EFCore migration is needed — only a version bump and API compat pass.

Known risks and research starting points:
- **System.Web.Optimization bundling** (Feature.0001) — `BundleConfig`/`BundleTable` usage must be replaced with direct `<script>`/`<link>` tags pointing at static content files; inspect `App_Start/BundleConfig.cs` and any `Bundles.RenderScripts/RenderStyles` calls in views.
- **Global.asax.cs application init** (Feature.1000) — convert `Application_Start` and related lifecycle hooks from `Global.asax.cs` into ASP.NET Core startup/hosting model; review `Global.asax.cs` for routing, area, filter, and bundle registrations that need an ASP.NET Core equivalent.
- **Antlr → Antlr4** — replace `Antlr` 3.4.1.9004 with `Antlr4` 4.6.6 per assessment recommendation; check for any direct Antlr API usage in code that needs updating for the new major version.
- **Microsoft.Data.SqlClient security vulnerability** — upgrade 2.1.4 → 7.1.1 (also deprecated at current version).
- **Binding redirects** — 12 issues in `Web.config` (6 mandatory MSB3836 conflicts where manual redirects disagree with auto-generated versions, 6 potential version-downgrade issues); these are removed entirely as part of SDK-style/PackageReference conversion (no hand-authored/non-standard redirects found — all are routine NuGet-generated bindingRedirect/dependentAssembly entries).
- **Packages included via framework reference** — `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.Web.Infrastructure`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `NETStandard.Library`, `System.Buffers`, `System.ComponentModel.Annotations`, `System.Memory`, `System.Numerics.Vectors`, `System.Threading.Tasks.Extensions` — remove these references; functionality ships with the `net10.0`/ASP.NET Core framework reference.
- **Deprecated packages** — `Microsoft.Identity.Client` 4.21.1 is deprecated; check if still needed and update to a supported version if so.
- **Client-side libraries** (`bootstrap`, `jQuery`, `jQuery.Validation`, `Modernizr`, `WebGrease`, `Microsoft.jQuery.Unobtrusive.Validation`) are already compatible — no action needed beyond ensuring static content paths still resolve after the Optimization bundling replacement.
- Operation sequence (per All-at-Once Execution Guidelines): (1) convert project file to SDK-style on `net48` first if needed as an intermediate structural step, then (2) retarget to `net10.0` and update all package references, (3) restore dependencies, (4) build and fix all compilation errors in a single bounded pass, (5) verify 0 build errors.

**Done when**: `ContosoUniversity.csproj` is SDK-style, targets `net10.0`, all package references are updated (including the Microsoft.Data.SqlClient security fix and Antlr→Antlr4 replacement), binding redirects are removed from `Web.config`, bundling and Global.asax application-start logic are converted to ASP.NET Core equivalents, and the project builds with 0 errors and 0 warnings.

### 02-final-validation: Final validation and test pass

Build the full solution and run any existing automated tests. Document any deferred recommendations (e.g., further modernization opportunities not in scope of this upgrade, such as enabling nullable reference types later).

**Done when**: Solution builds with 0 errors/0 warnings; all existing automated tests pass (none currently exist in this solution — if still true at execution time, this criterion is trivially satisfied, but note it in progress-details.md); deferred recommendations documented.

# Upgrade Plan — ContosoUniversity to .NET 10.0

## Upgrade Options

**Strategy**
- Upgrade Strategy: All-at-Once — single project solution, no dependency graph to manage.

**Project Structure**
- Project Approach (Web Projects): In-place rewrite — small web project (7 controllers, ~2k LOC), low complexity.

**Compatibility**
- Unsupported Packages: Resolve Inline — Antlr → Antlr4; Microsoft.AspNet.Web.Optimization → static asset links.
- System.Web Adapters: Direct Migration to ASP.NET Core APIs — small web project, in-place approach, no widespread HttpContext.Current usage.
- Cross-App Cookie Authentication: Not applicable — no cookie-based auth detected.

**Modernization**
- Assembly Binding Redirects: Remove — standard auto-generated VS boilerplate redirects.
- Nullable Reference Types: Leave Disabled — enable as a separate follow-up effort.
- Entity Framework: Not applicable — already on EF Core 3.1.32 (not EF6).

**Reliability**
- Test Coverage: Not applicable — not recommended by assessment.

### Selected Strategy
**All-At-Once** — Single project (ContosoUniversity.csproj), upgraded in one atomic pass from .NET Framework 4.8 to .NET 10.0.
**Rationale**: Solution contains exactly 1 project, currently net48, non-SDK-style, ASP.NET MVC 5 web application. Per the .NET Framework strategy override rule, a single-project solution always uses All-at-Once, regardless of project type.

## Projects

| Project | Current TFM | Target TFM | SDK-style | Type |
|---|---|---|---|---|
| ContosoUniversity.csproj | net48 | net10.0 | No → Yes | ASP.NET MVC 5 web application (System.Web) |

## Tasks

### 01-prerequisites-and-sdk-style-conversion: Prepare toolchain and convert ContosoUniversity.csproj to SDK-style format

Verify the .NET 10 SDK is installed and available, and confirm no `global.json` pins an incompatible SDK version. Convert `ContosoUniversity.csproj` from the legacy (non-SDK-style, `ToolsVersion="15.0"`) project format to SDK-style, while remaining on `net48` for this task — this is a structural change only, not a TFM change. The project currently uses `packages.config` for 45 NuGet packages; conversion must migrate these to `PackageReference` as part of the SDK-style conversion tooling. The project also carries manual assembly binding redirects in `Web.config` (6 packages: Newtonsoft.Json, System.Threading.Tasks.Extensions, System.ComponentModel.Annotations, System.Runtime.CompilerServices.Unsafe, System.Memory, Microsoft.Data.SqlClient) that conflict with auto-generated redirects (MSB3836) — these should be removed since SDK-style/.NET Core does not require hand-authored binding redirects.

**Done when**: `ContosoUniversity.csproj` is SDK-style, still targets `net48`, builds successfully with legacy MSBuild-equivalent tooling, and `packages.config` has been removed in favor of `PackageReference` entries.

### 02-upgrade-to-net10-and-migrate-to-aspnetcore: Upgrade ContosoUniversity to net10.0 and migrate ASP.NET MVC 5 to ASP.NET Core MVC

Retarget `ContosoUniversity.csproj` to `net10.0` using the `Microsoft.NET.Sdk.Web` SDK, and migrate the ASP.NET MVC 5 (System.Web-based) application to ASP.NET Core MVC in place (single pass, no side-by-side scaffolding — Project Approach = In-place rewrite). This is a System.Web / ASP.NET Framework web project with 7 controllers (~2k LOC) and no authentication middleware in use.

Key areas identified by the assessment to address during this task:
- **Project/host model**: Replace `Global.asax.cs` application startup (Application_Start, RouteConfig, FilterConfig, BundleConfig) with `Program.cs` minimal hosting model and `Microsoft.AspNetCore.Builder` middleware pipeline.
- **Routing**: Convert `RouteConfig.cs` (`RouteCollection`/`routes.MapRoute`) to ASP.NET Core endpoint routing (`MapControllerRoute` in `Program.cs`). Use the `migrating-mvc-routing` skill.
- **Bundling/minification**: `System.Web.Optimization` (`Microsoft.AspNet.Web.Optimization` package, flagged incompatible) is not supported in .NET Core — `BundleConfig.cs` bundles must be replaced with direct `<script>`/`<link>` tags pointing to static files served from `wwwroot`, or a modern bundler.
- **Packages**:
  - Remove: `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.Web.Infrastructure`, `NETStandard.Library`, and other packages whose functionality is now included with the framework reference.
  - Replace: `Antlr` (3.4.1.9004) → `Antlr4` (4.6.6) if still needed after migration (check actual usage first — it may be a transitive/unused leftover).
  - Replace: `Microsoft.AspNet.Web.Optimization` → remove, replace with static asset references (see Bundling above).
  - Upgrade: `Microsoft.EntityFrameworkCore*` 3.1.32 → 10.0.14 (EF Core, not EDMX/EF6 — straightforward version bump, review for EF Core 3→10 breaking changes), `Microsoft.Extensions.*` 3.1.32 → 10.0.14, `Microsoft.Data.SqlClient` 2.1.4 → 7.1.1 (resolves a known security vulnerability and a deprecated-package flag), `Newtonsoft.Json` 13.0.3 → 13.0.4, `System.Collections.Immutable`, `System.Diagnostics.DiagnosticSource`, `System.Runtime.CompilerServices.Unsafe`, `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.HashCode`.
  - Flag for review: `Microsoft.Identity.Client` 4.21.1 is deprecated — only upgrade/replace if the code actually uses it (check for Azure AD/MSAL usage); otherwise consider removing if unused.
- **HttpContext / System.Web surface**: Direct migration (no `Microsoft.AspNetCore.SystemWebAdapters` shim) — replace any `HttpContext.Current`, `Server.MapPath`, `Session`, or `Request`/`Response` static-context usage in controllers and views with injected ASP.NET Core equivalents (`HttpContext` via controller property, `IWebHostEnvironment` for paths).
- **Views**: Razor views (`.cshtml`) under `Views/` should carry over with minimal changes, but `@using` references to `System.Web.Mvc` and HTML helper differences need reconciling with `Microsoft.AspNetCore.Mvc.Razor`.
- **Data access**: `Data/` folder — review `DbContext` setup and connection string handling (move from `Web.config` `<connectionStrings>` to `appsettings.json` / `IConfiguration`).
- **Controllers**: `BaseController.cs`, `CoursesController.cs`, `DepartmentsController.cs`, `HomeController.cs`, `InstructorsController.cs`, `NotificationsController.cs`, `StudentsController.cs` — review each for `System.Web.Mvc` base-class differences (`Controller` base class moves namespace), `ActionResult` types, and any Web API-style attribute usage.
- **Services/**: review for any `System.Web`-dependent code (e.g., email/notification services using `System.Net.Mail` — compatible — or any HttpContext-dependent logic).
- **File uploads**: `Uploads/` folder suggests file upload handling (`TEACHING_MATERIAL_UPLOAD.md` describes this) — review for `HttpPostedFileBase` usage, which needs to migrate to `IFormFile`.
- **Configuration**: Simple `Web.config` (5 app settings, standard connection strings) — migrate to `appsettings.json` with the Generic Host configuration system.
- **Nullable reference types**: leave disabled for this upgrade (tracked as a follow-up item).

Research starting points: read `Global.asax.cs`, `App_Start/RouteConfig.cs`, `App_Start/BundleConfig.cs`, `App_Start/FilterConfig.cs`, `Web.config`, and each controller before making changes. Use `get_code_dependencies` on controllers if their dependency graph needs clarifying.

**Done when**: `ContosoUniversity.csproj` targets `net10.0` using `Microsoft.NET.Sdk.Web`, the project builds with 0 errors and 0 warnings using the .NET 10 SDK, all controllers and views compile against ASP.NET Core MVC, bundling/minification no longer depends on `System.Web.Optimization`, and the app starts under the ASP.NET Core hosting model.

### 03-final-validation: Build, test, and document deferred recommendations

Run a full build of the upgraded solution and execute the test suite (discover test projects first — none may currently exist; if none exist, note that as a gap rather than a failure). Fix any remaining compilation or runtime issues surfaced during this pass. Document any deferred recommendations (e.g., enabling nullable reference types as a follow-up, reviewing `Microsoft.Identity.Client` usage, confirming `Antlr` is actually needed) in the final progress notes.

**Done when**: Solution builds with 0 errors and 0 warnings; all discovered tests pass (or absence of tests is explicitly documented); deferred recommendations are recorded.

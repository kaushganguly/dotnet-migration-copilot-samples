# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Selected Strategy
**All-At-Once** — 1 project in the solution, no dependency graph to manage tier-by-tier.
**Rationale**: Single project (ContosoUniversity.csproj), low assessment difficulty, no
dependents/dependencies — the entire upgrade happens as one atomic pass.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph to manage |
| Project Approach (Web Projects) | In-place rewrite | Small MVC app (6 controllers), low complexity, no auth/DI/logging complications |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Antlr→Antlr4 known replacement; Microsoft.AspNet.Web.Optimization removed for static assets) |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | In-place approach, small project, minimal HttpContext.Current usage expected |
| Assembly Binding Redirects | Remove Binding Redirects | VS auto-generated boilerplate conflicts (MSB3836), no real version conflicts; SDK-style auto-manages resolution |
| Nullable Reference Types | Leave Disabled | Not explicitly requested; keep scope focused on the framework migration |

(Entity Framework, Test Coverage, Configuration Migration, Logging, DI, Windows Native APIs,
Cross-App Cookie Authentication, and Package Management/CPM options are not applicable —
already on EF Core, 0 projects flagged for Test Coverage, no complex config/logging/DI/native-API/
cookie-auth signals, and only 1 project in the solution.)

## Project List

| Project | Type | Current TFM | Target TFM | SDK-style |
|---------|------|-------------|------------|-----------|
| ContosoUniversity.csproj | ASP.NET MVC 5 Web App (System.Web) | net48 | net10.0 | No → Yes |

## Tasks

### 1. `01-sdk-style-conversion` — Convert project to SDK-style
Convert `ContosoUniversity.csproj` from legacy (non-SDK-style, `packages.config`) format
to SDK-style format, staying on the current target framework (net48) in this task.
Use the `convert_project_to_sdk_style` tool. Migrates `packages.config` to
`PackageReference`, removes legacy imports (including the VS `Microsoft.WebApplication.targets`
import), and preserves custom MSBuild targets (e.g., `CopySQLClientNativeBinaries`).

**Done when**: project is SDK-style, builds on net48 (or best achievable on this Linux
toolchain), no `packages.config` remains, solution file references intact.

### 2. `02-upgrade-to-net10` — Upgrade to .NET 10 and migrate ASP.NET MVC 5 to ASP.NET Core
Core upgrade task, covering the full scope of the migration in one pass (all-at-once —
single project, no phasing):
- Retarget `ContosoUniversity.csproj` to `net10.0`.
- Migrate the ASP.NET MVC 5 / System.Web application to ASP.NET Core on .NET 10:
  Global.asax/Global.asax.cs → `Program.cs` minimal hosting, `RouteConfig` →
  endpoint routing, `FilterConfig` → MVC filter registration, `BundleConfig`
  (`System.Web.Optimization`) → static files / bundling alternative, controllers/views
  carried forward onto `Microsoft.AspNetCore.Mvc`.
- NuGet dependency modernization: bump `Microsoft.EntityFrameworkCore*` and
  `Microsoft.Extensions.*` packages from 3.1.32 → 10.0.14 (already EF Core, no EF6
  migration needed), bump `Microsoft.Data.SqlClient` 2.1.4 → 7.1.1 (security vulnerability
  + deprecated), bump `Newtonsoft.Json` → 13.0.4, replace `Antlr` with `Antlr4` (4.6.6),
  remove `Microsoft.AspNet.Web.Optimization` (incompatible/deprecated — replaced by static
  asset references), drop packages whose functionality is now part of the framework
  reference (`Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`,
  `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.Web.Infrastructure`,
  `NETStandard.Library`, `System.Buffers`, `System.ComponentModel.Annotations`,
  `System.Memory`, `System.Numerics.Vectors`, `System.Threading.Tasks.Extensions`), remove
  `Microsoft.Identity.Client` if unused post-migration (deprecated).
- Remove all manual assembly binding redirects — SDK-style projects manage this automatically.
- Resolve all resulting API compatibility and build issues (one bounded build-and-fix pass).
- `#skill:migrating-aspnet-framework-to-core` — orchestrates the MVC-to-Core phase sequence
  (project file, host, config, DI, controllers, middleware, views, cleanup).

**Done when**: project targets net10.0, builds with 0 errors/warnings, application
starts via ASP.NET Core hosting, EF Core DbContext initializes against SQL Server,
no remaining `System.Web` references, no manual binding redirects remain.

### 3. `03-final-validation` — Final validation
Full solution build (0 errors, 0 warnings) and full test suite run. Document any
deferred recommendations (e.g., further modernization opportunities not in scope,
such as enabling nullable reference types as a follow-on effort).

**Done when**: solution builds cleanly, all tests pass, `progress-details.md` written
for tasks 1–2, deferred recommendations documented.

# Upgrade Plan: ContosoUniversity → .NET 10.0

## Scope

- Solution: `ContosoUniversity/ContosoUniversity.sln`
- Project: `ContosoUniversity/ContosoUniversity.csproj` — ASP.NET MVC 5 web application on .NET Framework 4.8, legacy (non-SDK-style) project format, `packages.config` package management.
- Target: `net10.0`, ASP.NET Core MVC, SDK-style project format, `PackageReference`.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project — no dependency graph to manage |
| Project Approach | In-place rewrite | Small web project (7 controllers, ~2k LOC), low complexity assessment |
| Unsupported Packages | Resolve Inline | 1 package without known alternative (Microsoft.AspNet.Web.Optimization) — small enough to research inline |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | In-place approach confirmed, small project, low `HttpContext.Current` usage |
| Configuration Migration | Auto-migrate to .NET Core Configuration | Standard `appSettings` (5 keys) and `connectionStrings` only |
| Assembly Binding Redirects | Remove Binding Redirects | All redirects are auto-generated boilerplate for BCL/framework assemblies, no real version conflicts |
| Nullable Reference Types | Leave Disabled | Enable separately after migration as a distinct effort |

## Strategy

**Selected**: All-At-Once
**Rationale**: Single project in solution, targeting .NET Framework 4.8 → net10.0. No dependency graph to manage; the whole project is converted in one atomic pass.

### Execution Constraints
- Single atomic upgrade — SDK-style conversion, TFM retarget, and ASP.NET MVC 5 → ASP.NET Core MVC migration happen together for this one project
- Validate with a full solution build (and test run) after the upgrade completes
- Load `migrating-aspnet-framework-to-core` skill for the ordered phase sequence (project file → host → config → DI → controllers → middleware → auth → views → cleanup)
- Breakdown into subtasks is expected at execution time given the scope (framework rewrite), per the project-type skill's migration unit breakdown

## Tasks

### 01-prereqs-and-sdk-conversion: Prerequisites and SDK-style conversion

Verify the .NET 10 SDK is installed and compatible. Convert `ContosoUniversity.csproj` from the legacy (non-SDK-style, `ToolsVersion="15.0"`) project format to SDK-style, migrating `packages.config` to `PackageReference` in the same step. This is a structural change only — the project remains on `net48` after this task; no TFM or API changes happen here.

**Done when**: `ContosoUniversity.csproj` is SDK-style, `packages.config` is removed, all package references are `PackageReference` entries, and the project still builds on `net48`.

### 02-migrate-to-aspnetcore: Migrate ASP.NET MVC 5 to ASP.NET Core MVC on net10.0

Retarget `ContosoUniversity.csproj` to `net10.0` and migrate the full ASP.NET MVC 5 (`System.Web`) application to ASP.NET Core MVC, in place (no side-by-side host). This covers: hosting model (`Global.asax`/`Application_Start` → `Program.cs` minimal hosting), routing (`App_Start/RouteConfig.cs` → endpoint routing), controllers (7 controllers under `Controllers/`), Razor views (`Views/`), configuration (`Web.config` → `appsettings.json`/`IConfiguration`, 5 `appSettings` keys + connection strings), dependency injection (built-in `IServiceCollection`), bundling/optimization (`App_Start/BundleConfig.cs`, `Microsoft.AspNet.Web.Optimization` — incompatible package, no direct replacement, replace with direct `<link>`/`<script>` tags or a modern bundler per assessment Feature.0001), and data access (`Microsoft.EntityFrameworkCore` 3.1.32 → 10.0.14, `Microsoft.Data.SqlClient` 2.1.4 → 7.1.1, addressing its known security vulnerability).

Assessment context: 45 NuGet packages, 24 recommended for upgrade, 2 incompatible (`Microsoft.AspNet.Web.Optimization` has no replacement package — direct migration to static tags; several others are framework-included and should be removed rather than upgraded). 12 mandatory/potential binding-redirect issues, all auto-generated boilerplate — remove during conversion. No custom DI container, no third-party logging framework, no cookie/Forms authentication detected. `NotificationsController`, background notification system (see `NOTIFICATION_SYSTEM_README.md`) and `Services/` folder should be reviewed for `System.Web`-specific APIs (e.g. `HttpContext.Current`, caching) that need direct ASP.NET Core equivalents.

Research starting points: inventory `HttpContext.Current` usage across `Controllers/` and `Services/`; review `App_Start/FilterConfig.cs` and `Global.asax.cs` for startup/pipeline code to port to `Program.cs`; review `Models/` and `Data/` for `DbContext` setup and connection-string usage; check `Views/Web.config` and view engine configuration for Razor-view-specific settings to carry over.

#skill:migrating-aspnet-framework-to-core

**Done when**: Solution targets `net10.0`, project is ASP.NET Core MVC with no `System.Web` references, `dotnet build` succeeds with 0 errors/warnings, and all existing unit tests pass.

### 03-final-validation: Final validation

Build the full solution, run the complete test suite, and confirm the application is a working ASP.NET Core MVC app on `net10.0`. Document any deferred recommendations (e.g., enabling nullable reference types as a follow-up effort).

**Done when**: Solution builds with 0 errors and 0 warnings; all unit tests pass; `passBuild=true` and `passUnitTests=true` success criteria are met.

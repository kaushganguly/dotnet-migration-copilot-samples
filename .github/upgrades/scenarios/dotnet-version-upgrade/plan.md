# .NET Version Upgrade Plan

## Overview

**Target**: ContosoUniversity.csproj — ASP.NET MVC 5 (System.Web) web application on .NET Framework 4.8
**Scope**: Single project, small (6 controllers, 27 views, ~2k LOC), low complexity assessment

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph to manage |
| Project Approach (Web Projects) | In-place rewrite | Small project (≤10 controllers, minimal middleware), low complexity |
| Unsupported Packages | Resolve Inline | Only 1 package without a known replacement (Microsoft.AspNet.Web.Optimization) |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small project, zero HttpContext.Current usage found |
| Assembly Binding Redirects | Remove Binding Redirects | No major version-jump conflicts; SDK-style + .NET Core auto-manage resolution |
| Nullable Reference Types | Enable Nullable Reference Types | Small codebase (<5k LOC), single project, low complexity |

## Tasks

### 01-sdk-style-conversion-and-tfm-retarget: Convert to SDK-style and retarget to net10.0

Convert `ContosoUniversity.csproj` from legacy (non-SDK-style) format to SDK-style, migrating `packages.config` to `PackageReference`. Then retarget the project from `net48` to `net10.0`. Remove the manual `<assemblyBinding>` redirects from `Web.config` (not needed with SDK-style/.NET Core dependency resolution). Update NuGet packages that are superseded by framework references (e.g. `System.Buffers`, `System.Memory`, `NETStandard.Library`) by removing them, and bump packages that have newer compatible versions (EF Core 3.1.32 → latest 10.x-compatible version, Newtonsoft.Json, Microsoft.Data.SqlClient, etc. per assessment). Address the known security vulnerability in `Microsoft.Data.SqlClient` by upgrading to a non-vulnerable version.

This task only changes project structure and package references — it does not yet replace `System.Web` APIs with ASP.NET Core equivalents. The project is expected to NOT build cleanly against `net10.0` at the end of this task (System.Web APIs are not available in .NET Core); that is resolved in the next task.

**Done when**: `ContosoUniversity.csproj` is SDK-style, targets `net10.0`, has no `packages.config`, has no manual assembly binding redirects, and all retained NuGet packages resolve to compatible, non-vulnerable versions for `net10.0`.

---

### 02-migrate-aspnet-mvc-to-aspnet-core: Migrate ASP.NET MVC 5 (System.Web) to ASP.NET Core MVC

Rewrite the application in place from ASP.NET MVC 5 / System.Web to ASP.NET Core MVC, replacing every System.Web-dependent piece with its ASP.NET Core equivalent using direct migration (no compatibility shim package):

- Hosting model: replace `Global.asax.cs` / `Global.asax` with `Program.cs` (minimal hosting, `WebApplicationBuilder`), moving `Application_Start` initialization logic and cleaning up anything no longer needed.
- Routing: convert `RouteConfig.cs` / `RouteCollection` registration to ASP.NET Core endpoint routing (`MapControllerRoute` in `Program.cs`).
- Configuration: migrate `Web.config` `<appSettings>` / `<connectionStrings>` to `appsettings.json`, replacing `ConfigurationManager` usage with the ASP.NET Core configuration/options system.
- Controllers, filters, and base controller: update `System.Web.Mvc` namespaces/base classes/attributes to `Microsoft.AspNetCore.Mvc` equivalents across `BaseController`, `CoursesController`, `DepartmentsController`, `HomeController`, `InstructorsController`, `NotificationsController`, `StudentsController`.
- Views: update all 27 Razor views/layouts for ASP.NET Core Razor syntax and tag helpers (replacing `@Html.*`/`@Url.*` patterns and bundling references as needed).
- Bundling/minification: `System.Web.Optimization` (Feature.0001) has no ASP.NET Core equivalent package — replace `BundleConfig` usage with static asset references (`<link>`/`<script>` tags) or `wwwroot`-based static file serving, moving `Content`/`Scripts` assets under `wwwroot`.
- Data access: upgrade the EF Core `DbContext`/configuration to work under the ASP.NET Core DI/configuration model (connection string now sourced from `appsettings.json`), keeping EF Core (already in use, not EF6) up to date with the new TFM-compatible version from Task 01.
- Messaging: review `NotificationsController`/notification services for any `System.Messaging` (MSMQ) usage and adapt for .NET Core compatibility if present.
- Dependency injection: register existing services (no third-party DI container detected) with the built-in ASP.NET Core `IServiceCollection`.
- Enable `<Nullable>enable</Nullable>` on the project and resolve resulting nullability warnings.

This task supersedes the package/project groundwork from Task 01 only where ASP.NET Core requires different packages (e.g., removing `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Web.Optimization`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.Web.Infrastructure`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` and adding the ASP.NET Core SDK/shared framework references in their place).

**Done when**: The solution builds with zero errors and zero warnings targeting `net10.0` using the ASP.NET Core MVC stack (no remaining `System.Web` references), the application's routes/controllers/views are wired through ASP.NET Core endpoint routing, configuration is read from `appsettings.json`, and any available tests pass.

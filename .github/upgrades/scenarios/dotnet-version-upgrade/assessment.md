# Assessment — ContosoUniversity: .NET Framework 4.8 (ASP.NET MVC 5) → .NET 10.0 (ASP.NET Core MVC)

> **Note on tooling**: `generate_dotnet_upgrade_assessment` could not fully evaluate this project via
> MSBuild because it is a legacy ASP.NET Web Application project type
> (`ProjectTypeGuids` includes `{349c5851-65df-11da-9384-00065b846f21}`) that imports
> `Microsoft.WebApplication.targets`, which is only available with a full Visual Studio / Build Tools
> install and is not present in this Linux SDK-only environment. The automated assessment output
> (`assessment/`) is therefore empty (0 projects/packages/APIs analyzed). This document was produced
> by direct manual inspection of the repository to compensate.

## Executive Summary

| Metric | Value |
|---|---|
| Projects | 1 (`ContosoUniversity.csproj`, legacy non-SDK style) |
| Current TFM | `net48` (`v4.8`) |
| Target TFM | `net10.0` |
| Project type | ASP.NET MVC 5 Web Application (System.Web), legacy `packages.config` |
| Controllers | 7 (BaseController, Courses, Departments, Home, Instructors, Notifications, Students) — 1092 LOC |
| Views | 26 `.cshtml` (Razor — largely portable to ASP.NET Core Razor views) |
| Data layer | Already on **EF Core 3.1.32** (`Microsoft.EntityFrameworkCore.SqlServer`) — not EF6/EF Database First, so no EF6→EF Core data-layer rewrite needed, only a package version bump + API changes between 3.1 and the .NET-10-compatible EF Core version |
| Test projects | None found in the repo |

## Technologies / Features Requiring Migration

| Technology | Where | Issue | Migration Path |
|---|---|---|---|
| `System.Web.Mvc`, `System.Web.Routing`, `System.Web.Optimization`, `System.Web` | Controllers, `Global.asax(.cs)`, `App_Start/*` | Not available on modern .NET | Replace with `Microsoft.AspNetCore.Mvc`, ASP.NET Core routing/endpoints, `Program.cs` minimal hosting, static files for bundling |
| `System.Configuration.ConfigurationManager` + `Web.config` `connectionStrings`/`appSettings` | `Global.asax.cs`, `Data/SchoolContextFactory.cs`, `Services/NotificationService.cs` | Config system replaced in ASP.NET Core | Move to `appsettings.json` + `IConfiguration` / DI-injected `DbContextOptions` |
| `System.Messaging` (MSMQ) | `Services/NotificationService.cs` | **Windows-only, no cross-platform .NET replacement, no NuGet package targets modern .NET on Linux** | Replace MSMQ-backed queue with an in-process thread-safe queue (`System.Collections.Concurrent.ConcurrentQueue<T>`) behind the same `NotificationService` public surface (`SendNotification`/`ReceiveNotification`/`MarkAsRead`), registered as a singleton via DI — preserves behavior/API for controllers, removes Windows dependency |
| `packages.config` | project root | Legacy package management | Convert to `PackageReference` in SDK-style csproj |
| Non-SDK-style `.csproj` (`ToolsVersion="15.0"`, explicit `<Reference>`/`<Compile>` items) | `ContosoUniversity.csproj` | Legacy MSBuild format, requires `Microsoft.WebApplication.targets` | Rewrite as SDK-style `Microsoft.NET.Sdk.Web` project with implicit file globbing |
| `JsonRequestBehavior.AllowGet`, `ActionResult`/`JsonResult` from `System.Web.Mvc` | Controllers | API moved | Use `Microsoft.AspNetCore.Mvc` equivalents (`IActionResult`/`JsonResult`, no `JsonRequestBehavior` needed — ASP.NET Core always allows GET JSON) |
| `HandleErrorAttribute`, `GlobalFilters`, `RouteConfig`, `FilterConfig`, `BundleConfig`, `AreaRegistration` | `App_Start/*`, `Global.asax.cs` | ASP.NET MVC 5 pipeline concepts | Replace with ASP.NET Core middleware pipeline (`app.UseExceptionHandler`, `app.MapControllerRoute`, static files, no bundling framework needed — serve bootstrap/jquery statically from `wwwroot`) |
| `Newtonsoft.Json` | `NotificationService.cs` | Works on .NET 10 but not default | Keep as explicit `PackageReference` (upgraded) OR migrate to `System.Text.Json` — keeping Newtonsoft is lower risk given existing serialize/deserialize calls; will upgrade package version |
| EF Core 3.1.32 | `Data/*`, packages.config | Outdated EF Core | Upgrade `Microsoft.EntityFrameworkCore*` packages to latest version compatible with net10.0 (EF Core 9/10 line) |
| `Microsoft.Data.SqlClient` 2.1.4 | packages.config | Outdated, has had security advisories in old versions | Upgrade to latest compatible version |

## Security Vulnerabilities

Known-vulnerable legacy package versions present in `packages.config` that will be remediated as part
of the package upgrade (exact advisories reconfirmed via `dotnet list package --vulnerable` after SDK
conversion, since the legacy project can't be restored/evaluated for vulnerability scanning pre-conversion):
- `Microsoft.Data.SqlClient` 2.1.4 → latest stable
- `Newtonsoft.Json` 13.0.3 → latest stable (already relatively current, re-verify)
- `Microsoft.EntityFrameworkCore*` 3.1.32 → latest stable compatible with net10.0 (3.1.x is out of support)

These will be addressed as part of the upgrade by default.

## Risk Assessment

| Risk | Severity | Mitigation |
|---|---|---|
| MSMQ (`System.Messaging`) has no cross-platform equivalent | High | Replace with in-process `ConcurrentQueue`-backed implementation behind same service API |
| Legacy project can't be MSBuild-evaluated on this (Linux) box for tooling-assisted assessment | Medium | Manual inspection performed; rely on `dotnet build` feedback loop during execution for exhaustive API-break discovery |
| No existing automated tests | Medium | Cannot run regression tests; validate via successful build + manual route/data-flow review. `generateNewUnitTests` is explicitly false per task success criteria, so none will be authored |
| View bundling (`System.Web.Optimization`) removed in ASP.NET Core | Low | Serve static JS/CSS directly from `wwwroot`, no bundling/minification framework required for functional parity |
| TPH inheritance / composite keys in EF Core model | Low | `OnModelCreating` is already EF Core idiomatic — should port with minimal/no change |

## Project Details

### ContosoUniversity.csproj
- Current: `net48`, legacy format, `OutputType=Library` (web app), `packages.config`
- Target: `net10.0`, SDK-style, `Microsoft.NET.Sdk.Web`, `PackageReference`
- Estimated LOC impact: Controllers (~1092 LOC, moderate namespace/API changes), Views (~26 files, low impact — Razor syntax mostly compatible), `App_Start`/`Global.asax` (full rewrite to `Program.cs`), `Services/NotificationService.cs` (MSMQ replacement), `Data/*` (low impact — already EF Core idiomatic)

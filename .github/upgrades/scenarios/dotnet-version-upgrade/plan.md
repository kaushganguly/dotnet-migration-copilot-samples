# .NET Version Upgrade Plan

## Overview

**Target**: ContosoUniversity — ASP.NET MVC 5 (System.Web, .NET Framework 4.8) web application upgraded to ASP.NET Core MVC on .NET 10.0, with SDK-style project conversion and PackageReference migration.
**Scope**: 1 project, small web application (6 controllers, EF Core 3.1 data layer, MSMQ-based notification service, no authentication).

### Selected Strategy
**All-At-Once** — Single project in the solution; no dependency graph to manage. Upgraded in one pass per the .NET Framework single-project rule.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project — no tiers to validate independently |
| Project Approach (Web) | In-place rewrite | Small web surface (6 controllers), low complexity, full conversion requested |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Antlr, Microsoft.AspNet.Web.Optimization), both removed as part of the MVC5→Core rewrite |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small project, in-place approach, no `HttpContext.Current` usage found |
| Assembly Binding Redirects | Document and Review Before Removing | 22 redirects present (>10 threshold); reviewed during package cleanup, then removed via SDK-style/PackageReference conversion |
| Nullable Reference Types | Leave Disabled | No explicit request to enable; keep scope focused on framework/stack migration |
| Test Coverage | Skip | Assessment recommends 0 projects for coverage; user set generateNewUnitTests=false |

## Tasks

### 01-sdk-style-conversion-and-packages: Convert to SDK-style project and update NuGet dependencies

Convert `ContosoUniversity.csproj` from the legacy (`ToolsVersion`-based) project format to SDK-style, targeting `net10.0` with the `Microsoft.NET.Sdk.Web` SDK. Remove `packages.config` and replace it with `PackageReference` items.

Update package versions for the target framework:
- Upgrade `Microsoft.EntityFrameworkCore`, `.Abstractions`, `.Relational`, `.SqlServer`, `.Tools` from 3.1.32 to the net10.0-compatible 10.x line.
- Upgrade `Microsoft.Extensions.*` (Caching, Configuration, DependencyInjection, Logging, Options, Primitives) from 3.1.32 to the net10.0-compatible 10.x line.
- Upgrade `Microsoft.Data.SqlClient` (security vulnerability in 2.1.4) and `Newtonsoft.Json` (13.0.3 → 13.0.4).
- Remove packages whose functionality is now part of the framework/SDK (e.g. `NETStandard.Library`, `Microsoft.Bcl.*` where superseded, `System.ComponentModel.Annotations`, `System.Buffers`, `System.Memory`, `System.Threading.Tasks.Extensions`, `System.Runtime.CompilerServices.Unsafe`, `System.Numerics.Vectors`, `System.Diagnostics.DiagnosticSource` if superseded by framework references).
- Remove MVC5/System.Web-only packages entirely, since ASP.NET Core ships this functionality in-box: `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.AspNet.Web.Optimization` (incompatible), `WebGrease`, `Antlr` (incompatible, transitive to Razor generation), `Microsoft.Web.Infrastructure`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.jQuery.Unobtrusive.Validation` (client assets handled as static files/CDN instead).
- Replace `System.Messaging` (MSMQ) framework reference with an equivalent supported package for .NET so `Services/NotificationService.cs` continues to compile and work on net10.0.
- Review and remove hand-authored/auto-generated assembly binding redirects from `Web.config` — PackageReference + SDK-style projects manage binding at restore/publish time and redirects are not hand-authored going forward.
- Move static client assets (`Content`, `Scripts`, images) under `wwwroot/` per ASP.NET Core convention, keeping bootstrap/jQuery/jquery-validate assets.

**Done when**: `ContosoUniversity.csproj` is SDK-style, targets `net10.0`, has no `packages.config`, has no incompatible or removed packages referenced, and the project restores cleanly.

---

### 02-migrate-mvc-to-aspnetcore: Migrate ASP.NET MVC 5 application to ASP.NET Core MVC

Rewrite the application hosting and request pipeline for ASP.NET Core, replacing `Global.asax`/`App_Start` with `Program.cs`-based minimal hosting:
- Replace `Global.asax.cs` (`Application_Start`, EF Core database initialization) with `Program.cs` host building, `WebApplicationBuilder`, and startup-time `DbInitializer.Initialize` call via DI-resolved `SchoolContext`.
- Convert `App_Start/RouteConfig.cs` MVC route table registration to ASP.NET Core endpoint routing (`MapControllerRoute`) in `Program.cs`.
- Convert `App_Start/FilterConfig.cs` global filter registration to ASP.NET Core MVC options-based filter registration.
- Remove `App_Start/BundleConfig.cs` and `System.Web.Optimization` bundling; replace `@Styles.Render` / `@Scripts.Render` calls in `Views/Shared/_Layout.cshtml` with direct `<link>`/`<script>` tags pointing at `wwwroot` assets (or a lightweight bundling alternative).
- Migrate `Controllers/BaseController.cs` and all controllers (`Home`, `Students`, `Courses`, `Instructors`, `Departments`, `Notifications`) from `System.Web.Mvc.Controller` to `Microsoft.AspNetCore.Mvc.Controller`, updating action result types, model binding attributes (`[Bind(Include=...)]`), `[ValidateAntiForgeryToken]`, and `ViewBag`/`SelectList` usage to their ASP.NET Core equivalents.
- Migrate `Controllers/CoursesController.cs` file upload handling from `HttpPostedFileBase` + `Server.MapPath` + `SaveAs` to `IFormFile` + `IWebHostEnvironment` (`WebRootPath`) + stream-based save, preserving the existing validation rules (file extension allow-list, 5MB size limit) and the `Uploads/TeachingMaterials` storage location.
- Migrate `Services/NotificationService.cs` off `System.Messaging.MessageQueue` to the replacement MSMQ package selected in the previous task, preserving send/receive/message-format behavior, and register it for DI instead of being instantiated directly in `BaseController`.
- Register `SchoolContext` via `AddDbContext` in DI instead of a static factory, and update `Data/SchoolContextFactory.cs` / consumers accordingly.
- Update all Razor views for ASP.NET Core Razor conventions (`_ViewStart.cshtml`, `_ViewImports.cshtml`, tag helpers in place of some `@Html.*` helpers where it reduces breakage, otherwise keep `@Html.ActionLink`/`@Html.ValidationSummary` equivalents that exist in ASP.NET Core).
- Update `Web.config` content that still applies (app settings, connection string) into `appsettings.json`, and remove content that no longer applies (`system.web`, `system.webServer` IIS-classic sections superseded by Kestrel/hosting configuration), keeping `NotificationQueuePath` and `DefaultConnection` available via `IConfiguration`.

**Done when**: the application runs on the ASP.NET Core hosting model (`Program.cs`, Kestrel/`IWebHostEnvironment`), no `System.Web` namespace references remain, all controllers/views compile against `Microsoft.AspNetCore.Mvc`, file upload and notification functionality behave the same as before, and the solution builds with 0 errors.

---

### 03-final-validation: Build, test, and finalize the upgrade

Restore and build the full solution targeting `net10.0`, fix any remaining compilation errors from the TFM/package changes, run the existing automated tests (if any) and confirm they pass, and confirm no remaining security-vulnerable or deprecated package references are present (`Microsoft.Data.SqlClient`, `Microsoft.Identity.Client`).

**Done when**: the solution builds with 0 errors and 0 warnings for the modified project, all existing automated tests pass, and no incompatible/vulnerable/deprecated packages remain referenced.

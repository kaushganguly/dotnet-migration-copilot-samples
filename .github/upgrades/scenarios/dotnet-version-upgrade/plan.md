# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 (ASP.NET MVC 5) → .NET 10.0 (ASP.NET Core)

## Summary

ContosoUniversity is a single legacy, non-SDK-style ASP.NET MVC 5 web application project
(`ContosoUniversity.csproj`, `TargetFrameworkVersion=v4.8`) with no separate class library or test
project. It already uses **EF Core 3.1** for data access (not EF6), which substantially reduces data
layer migration risk. There is no existing test project in the solution, so `passUnitTests=true` has
no pre-existing tests to satisfy — build success is the primary gate.

This is a full in-place rewrite from System.Web/ASP.NET MVC 5 to ASP.NET Core on net10.0, not a pure
TFM bump, because:
- Legacy `Global.asax` / `HttpApplication` / `System.Web.Mvc.Controller` have no net10.0 equivalent.
- `System.Web.Optimization` bundling, `System.Web.Routing`, `JsonRequestBehavior`, `ViewBag`-based
  patterns need ASP.NET Core equivalents.
- **`System.Messaging` (MSMQ)** is used by `Services/NotificationService.cs` — MSMQ is Windows-only
  and has no implementation on .NET Core/.NET 5+ (nor on Linux, where this repo is being built/tested).
  Must be replaced with a cross-platform, in-process substitute that preserves the
  `SendNotification` / `ReceiveNotification` / `MarkAsRead` public surface used by
  `BaseController` and `NotificationsController`.

## Target State

- SDK-style `ContosoUniversity.csproj` using `Microsoft.NET.Sdk.Web`, `TargetFramework=net10.0`.
- `Program.cs` (minimal hosting model) replacing `Global.asax.cs` + `App_Start/*`.
- `appsettings.json` replacing `Web.config` `<appSettings>` / `<connectionStrings>`.
- Controllers converted to `Microsoft.AspNetCore.Mvc.Controller` / `IActionResult`.
- Static assets (`Content`, `Scripts`) moved under `wwwroot`; bundling/minification replaced by direct
  `<link>` / `<script>` tags (no `System.Web.Optimization` equivalent is supported in ASP.NET Core;
  ok for this app's scale — no build-time bundler is required to meet success criteria).
- Views updated: `@Styles.Render` / `@Scripts.Render` → plain tags; `JsonRequestBehavior.AllowGet`
  removed (`Json()` in Core always allows GET); `Html.ActionLink` calls kept (supported across
  ASP.NET Core via Microsoft.AspNetCore.Mvc.TagHelpers/HtmlHelper extensions) — verified/fixed during
  execution if signatures differ.
- EF Core upgraded 3.1.32 → 10.0.x; `Microsoft.Data.SqlClient` upgraded to latest non-vulnerable
  version; `Microsoft.EntityFrameworkCore.Tools` kept as a dev dependency.
- `Services/NotificationService.cs` MSMQ usage replaced with a cross-platform
  `System.Threading.Channels`-backed in-process queue (same public method signatures), eliminating
  the Windows-only dependency while preserving behavior for the admin notification dashboard.
- `packages.config` removed; packages become `<PackageReference>` items in the SDK-style csproj.
- Binding redirects removed entirely (not applicable/needed under .NET Core's assembly loading model).

## Risk Areas

1. **MSMQ removal** — behavioral change from a persistent cross-process queue to an in-process queue.
   Acceptable for this sample app (single-process web app, no external producers/consumers of the
   queue), and necessary since MSMQ cannot run on Linux/.NET Core at all.
2. **Static asset bundling removal** — `System.Web.Optimization` has no direct ASP.NET Core
   equivalent; replaced with direct references. Low risk, purely presentational.
3. **EF Core 3.1 → 10.0 jump** — large version jump; watch for API changes in `OnModelCreating`
   (TPH discriminator API, composite keys) — these APIs are stable across this range but will be
   verified by build.
4. **No existing tests** — `passUnitTests=true` is trivially satisfied (no tests exist to fail);
   `generateNewUnitTests=false` means we do not add a test project.

## Tasks

- ⬜ 01-sdk-style-conversion: Convert project to SDK-style, target net10.0, migrate packages.config
- ⬜ 02-hosting-and-startup-migration: Replace Global.asax/App_Start with Program.cs + appsettings.json
- ⬜ 03-controllers-and-views-migration: Migrate controllers/views from System.Web.Mvc to ASP.NET Core
- ⬜ 04-notification-service-msmq-replacement: Replace MSMQ NotificationService with cross-platform queue
- ⬜ 05-data-layer-and-package-upgrades: Upgrade EF Core/NuGet packages, fix config access
- ⬜ 06-build-validation-and-cleanup: Final build, warning cleanup, remove dead files, security check

### Task Details

1. **01-sdk-style-conversion** — Convert `ContosoUniversity.csproj` to SDK-style
   (`Microsoft.NET.Sdk.Web`), target `net10.0`, migrate `packages.config` → `PackageReference`,
   remove binding redirects, remove `AssemblyInfo.cs` duplicate attributes conflict.
2. **02-hosting-and-startup-migration** — Replace `Global.asax(.cs)` + `App_Start/*` with
   `Program.cs` (minimal hosting, middleware pipeline, MVC routing, static files), migrate
   `Web.config` settings to `appsettings.json`.
3. **03-controllers-and-views-migration** — Migrate all controllers from `System.Web.Mvc` to
   `Microsoft.AspNetCore.Mvc`; fix `JsonResult`/`ActionResult` usage; update Razor views
   (`_ViewStart.cshtml`, `_Layout.cshtml`, bundling tags) and move static content to `wwwroot`.
4. **04-notification-service-msmq-replacement** — Replace `System.Messaging`-based
   `NotificationService` with a cross-platform in-process queue implementation; register via DI.
5. **05-data-layer-and-package-upgrades** — Upgrade EF Core and other NuGet packages to net10.0-
   compatible/secure versions; verify `SchoolContext` / `SchoolContextFactory` work under the new
   configuration-based connection string retrieval (`IConfiguration` instead of
   `ConfigurationManager`).
6. **06-build-validation-and-cleanup** — Full solution build, warning cleanup, remove dead
   System.Web-era files (`Global.asax`, `App_Start`, old `Web.config` sections no longer needed),
   final security/vulnerability pass over package versions.

## Success Criteria Mapping

- `passBuild=true` → Task 06 runs `dotnet build` clean (0 errors, 0 warnings).
- `generateNewUnitTests=false` → no test project added.
- `passUnitTests=true` → trivially true (no test project in solution); re-checked at Task 06.
- `securityComplianceCheck=true` → Task 05 ensures no `NuGet.0004` (vulnerable) packages remain.

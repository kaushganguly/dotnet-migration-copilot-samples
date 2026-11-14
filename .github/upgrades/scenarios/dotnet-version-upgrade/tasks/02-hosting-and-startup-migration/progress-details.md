# 02 — Hosting and startup migration

**Status:** Completed

## Changes
- Deleted `Global.asax`, `Global.asax.cs`, `App_Start/{BundleConfig,FilterConfig,RouteConfig}.cs`,
  `Properties/AssemblyInfo.cs`, `Web.config`, `Views/Web.config` (all obsolete under the ASP.NET
  Core hosting model / SDK-style project).
- Added `Program.cs` (minimal hosting model):
  - `AddControllersWithViews().AddJsonOptions(...)` with `PropertyNamingPolicy = null` and
    `DictionaryKeyPolicy = null` — preserves the exact PascalCase JSON field names
    (`Operation`, `EntityType`, `Message`, `CreatedAt`, `CreatedBy`) that the in-repo
    `wwwroot/Scripts/notifications.js` consumer expects (originally produced by MVC5's
    `JavaScriptSerializer`, which does not camelCase).
  - `AddDbContext<SchoolContext>` wired to `IConfiguration` (`ConnectionStrings:DefaultConnection`).
  - `AddSingleton<INotificationService, NotificationService>()`.
  - DB init/seed via `DbInitializer.Initialize` invoked in a startup DI scope (replaces
    `Global.asax.Application_Start`).
  - Standard middleware pipeline: exception handler + HSTS (non-dev), HTTPS redirect, static
    files, routing, authorization, default MVC route (`{controller=Home}/{action=Index}/{id?}`).
- Added `appsettings.json` / `appsettings.Development.json` replacing `Web.config`
  (`connectionStrings`, relevant `appSettings` keys such as `ClientValidationEnabled`,
  `UnobtrusiveJavaScriptEnabled`, `NotificationQueuePath`).
- Added `Views/_ViewImports.cshtml` with the model namespaces the views use (`ContosoUniversity`,
  `ContosoUniversity.Models`, `ContosoUniversity.Models.SchoolViewModels`).
- Rewrote `Views/Shared/_Layout.cshtml` and all `@Scripts.Render("~/bundles/jqueryval")` usages
  (Departments/Courses/Students/Instructors Edit & Create views) to reference the actual static
  files directly under `wwwroot/Scripts`/`wwwroot/Content` instead of the removed
  `System.Web.Optimization` bundling APIs. Only files that actually exist on disk are referenced
  (the repo never had `bootstrap.css` physically present — a pre-existing condition, not a
  migration regression).
- Rewrote `Views/Shared/Error.cshtml` + `HomeController.Error()` to use the standard ASP.NET Core
  `ErrorViewModel` (already present in `Models/ErrorViewModel.cs`) instead of the MVC5-only
  `System.Web.Mvc.HandleErrorInfo` / `HttpContext.Current.IsDebuggingEnabled`.

## Verified at runtime
Ran the app against a real SQL Server (Docker `mssql/server:2022-latest`) — EF Core
`EnsureCreated()` + seed data executed successfully, all main pages (`/`, `/Students`, `/Courses`,
`/Instructors`, `/Departments`, `/Home/About`, `/Notifications`) returned HTTP 200, and
`/Notifications/GetNotifications` returned the expected JSON shape
(`{"success":true,"notifications":[],"count":0}`).

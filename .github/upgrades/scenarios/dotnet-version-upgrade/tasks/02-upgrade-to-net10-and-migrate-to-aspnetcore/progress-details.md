# Task 02: Upgrade to net10.0 and migrate to ASP.NET Core — Progress

Status: Completed

## Project file (`ContosoUniversity.csproj`)
- Retargeted `TargetFramework` from `net48` to `net10.0`.
- Removed all legacy `<Reference Include="...">` items (`Microsoft.CSharp`,
  `System.Data.DataSetExtensions`, `System.ComponentModel.DataAnnotations`, `System.Configuration`,
  `System.Messaging`, `netstandard`, `System.Net.Http`) — all are either built into .NET 10 or
  replaced by a modern package (`System.Messaging` -> `MSMQ.Messaging`).
- Removed the obsolete `CopySQLClientNativeBinaries` MSBuild target (referenced a stale
  `packages\Microsoft.Data.SqlClient.SNI.runtime...` path; modern `Microsoft.Data.SqlClient`
  versions don't require manual SNI binary copying).
- Removed explicit `<Content Include="Content\...">` / `<Content Include="Scripts\...">` items —
  SDK-style Web projects auto-glob `wwwroot/**`, and the underlying `Content/`/`Scripts/`
  directories were removed (static assets moved to `wwwroot/`, see below).
- Removed stale `<Content Include="favicon.ico">`, `bootstrap.css(.map)`, and the sample upload
  image item — none of these files physically existed in the repository (stale references from
  before this migration).
- Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` so the existing, manually maintained
  `Properties/AssemblyInfo.cs` doesn't conflict with SDK auto-generated assembly attributes.
- Package reference changes:
  - Upgraded: `Microsoft.EntityFrameworkCore(.SqlServer/.Tools)` 3.1.32 -> 10.0.14,
    `Newtonsoft.Json` 13.0.3 -> 13.0.4.
  - Added: `MSMQ.Messaging` 1.0.4 (replaces `System.Messaging`), `System.Drawing.Common` 10.0.14
    and `System.Security.Cryptography.Pkcs` 10.0.14 (pinned explicitly to patched versions —
    `MSMQ.Messaging` pulls older, vulnerable transitive versions of both; pinning resolves the
    NU1902/NU1903/NU1904 vulnerability warnings while keeping `MSMQ.Messaging` functional).
  - Removed entirely (now built into the .NET 10 / ASP.NET Core shared framework, or no longer
    used): `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`,
    `Microsoft.Web.Infrastructure`, `NETStandard.Library`, `System.Buffers`,
    `System.Collections.Immutable`, `System.ComponentModel.Annotations`,
    `System.Diagnostics.DiagnosticSource`, `System.Memory`, `System.Numerics.Vectors`,
    `System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`,
    `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.HashCode`,
    `Microsoft.Data.SqlClient` (2.1.4; EFCore.SqlServer 10.0.14 brings a current, patched
    version transitively — no direct SqlClient API usage was found in the codebase),
    `Microsoft.Data.SqlClient.SNI.runtime` (SNI copy target removed, see above),
    `bootstrap`, `jQuery`, `jQuery.Validation`, `Microsoft.jQuery.Unobtrusive.Validation`,
    `Modernizr`, `WebGrease` (all replaced by CDN-hosted static assets, see below),
    `Antlr` and `Microsoft.Identity.Client` (confirmed via repo-wide grep — neither is
    referenced anywhere in source; both were unused leftover dependencies).

## Hosting model
- Deleted `Global.asax`, `Global.asax.cs`, `App_Start/` (`RouteConfig.cs`, `FilterConfig.cs`,
  `BundleConfig.cs`), `Views/Web.config`, `Web.config`.
- Added `Program.cs` (minimal hosting model): registers `AddControllersWithViews()`,
  `AddDbContext<SchoolContext>` (SQL Server, connection string from configuration),
  `Configure<MessageQueueOptions>` + `AddScoped<NotificationService>()`, standard middleware
  pipeline (`UseExceptionHandler`/`UseHsts` in non-dev, `UseHttpsRedirection`, `UseStaticFiles`,
  `UseRouting`, `UseAuthorization`, default `{controller=Home}/{action=Index}/{id?}` route
  matching the original `RouteConfig`), and calls `DbInitializer.Initialize` against a DI scope
  at startup (mirrors the original `Global.asax` `Application_Start` seeding behavior).
- Added `appsettings.json` (`ConnectionStrings:DefaultConnection`, `MessageQueue:QueuePath`,
  default logging config) and `appsettings.Development.json`, replacing the connection string
  and `NotificationQueuePath` app setting that used to live in `Web.config`.
- Repurposed `Data/SchoolContextFactory.cs` from a manual "new SchoolContext(...)" helper into an
  `IDesignTimeDbContextFactory<SchoolContext>` implementation, so `dotnet ef` tooling still works
  for migrations/scaffolding; the app itself now gets `SchoolContext` via DI, not this factory.

## Controllers
All 7 controllers updated: `using System.Web.Mvc` -> `using Microsoft.AspNetCore.Mvc` (+
`Microsoft.AspNetCore.Mvc.Rendering` for `SelectList`), `HttpNotFound()` -> `NotFound()`,
`new HttpStatusCodeResult(HttpStatusCode.BadRequest)` -> `BadRequest()`,
`[Bind(Include = "...")]` -> `[Bind("...")]`, manual `protected override void Dispose(bool)`
removed (DB context / notification service are now DI-scoped and disposed by the container).
- `BaseController`: now takes `SchoolContext` and `NotificationService` via constructor
  injection instead of `SchoolContextFactory.Create()` / `new NotificationService()`.
- `HomeController`, `DepartmentsController`, `StudentsController`, `NotificationsController`:
  added pass-through constructors calling `base(context, notificationService)` (C# doesn't
  forward base constructors automatically).
- `InstructorsController`: same, plus its `Edit` POST action was changed from
  `TryUpdateModel(model, prefix, string[] propertyNames)` (ASP.NET MVC only) to
  `await TryUpdateModelAsync(model, prefix, expr1, expr2, ...)` (the ASP.NET Core equivalent),
  so the action is now `async Task<ActionResult>`.
- `CoursesController`: added `IWebHostEnvironment` constructor parameter;
  `HttpPostedFileBase` -> `IFormFile` (`.ContentLength` -> `.Length`, `.SaveAs(path)` ->
  `using (var fs = new FileStream(path, FileMode.Create)) { file.CopyTo(fs); }`);
  `Server.MapPath("~/Uploads/TeachingMaterials/")` -> `Path.Combine(_environment.WebRootPath,
  "Uploads", "TeachingMaterials")` in all three places (create, edit-replace-old-file, delete).
- `NotificationsController`: removed all `JsonRequestBehavior.AllowGet` arguments (not present /
  not needed in ASP.NET Core's `Json()` helper — GET JSON responses aren't blocked by default).

## Services
- `NotificationService`: `using System.Messaging` -> `using MSMQ.Messaging` (drop-in API-compatible
  replacement package); `ConfigurationManager.AppSettings["NotificationQueuePath"]` -> constructor
  injection of `IOptions<MessageQueueOptions>` (new `Services/MessageQueueOptions.cs` class bound
  to the `MessageQueue` section of `appsettings.json`). MSMQ itself remains a Windows-only runtime
  dependency regardless of .NET version — this is an inherent, documented limitation of the
  underlying technology, not something fixable by this migration.

## Static assets
- Moved into `wwwroot/` (auto-served by `UseStaticFiles()` / auto-globbed by the Web SDK):
  `Content/Site.css` -> `wwwroot/css/site.css`, `Content/notifications.css` ->
  `wwwroot/css/notifications.css`, `Scripts/notifications.js` -> `wwwroot/js/notifications.js`,
  `Uploads/TeachingMaterials/` -> `wwwroot/Uploads/TeachingMaterials/`.
- Removed vendored `Scripts/*` (bootstrap, jQuery, jQuery Validation, Modernizr, Respond) — none
  of the corresponding Bootstrap/jQuery CSS/JS files physically existed in the repo (stale
  `<Content Include>` references from before this migration), so rather than reconstructing
  vendored copies, switched to CDN-hosted Bootstrap 5.3.3, jQuery 3.7.1, jquery-validation 1.21.0,
  and jquery-validation-unobtrusive 4.0.0 in `_Layout.cshtml` and the four Create/Edit view pairs
  that referenced `@Scripts.Render("~/bundles/jqueryval")`. `Modernizr`/`Respond.js` were dropped
  entirely (obsolete IE-era polyfills, not meaningful in a modern-browser target).

## Views
- Added `Views/_ViewImports.cshtml` (`@using ContosoUniversity`, `@using ContosoUniversity.Models`,
  `@using ContosoUniversity.Models.SchoolViewModels`, `@addTagHelper *,
  Microsoft.AspNetCore.Mvc.TagHelpers`), replacing the Razor namespace imports that used to live
  in the now-deleted `Views/Web.config`.
- Rewrote `Views/Shared/Error.cshtml` to use `@model ContosoUniversity.Models.ErrorViewModel`
  (already present in `Models/`) instead of `System.Web.Mvc.HandleErrorInfo` /
  `HttpContext.Current.IsDebuggingEnabled`; `HomeController.Error()` now populates
  `RequestId = HttpContext.TraceIdentifier`.
- Rewrote `Views/Shared/_Layout.cshtml`: `@Styles.Render`/`@Scripts.Render` replaced by direct
  `<link>`/`<script>` tags (CDN Bootstrap, local `~/css/site.css` and `~/css/notifications.css`,
  CDN jQuery, local `~/js/notifications.js`); dropped the modernizr bundle reference.
- Updated the 8 Create/Edit views (Students, Courses, Departments, Instructors) that referenced
  `@Scripts.Render("~/bundles/jqueryval")`, replacing it with explicit CDN `<script>` tags for
  jquery-validation + jquery-validation-unobtrusive.
- All other views (index/details/delete pages, HTML helpers like `Html.ActionLink`,
  `Html.EditorFor`, `Html.ValidationMessageFor`, `Html.DisplayFor`, `Url.Content`, etc.) needed no
  changes — these APIs have direct, source-compatible equivalents in
  `Microsoft.AspNetCore.Mvc.Rendering` / `Microsoft.AspNetCore.Mvc.ViewFeatures` and were already
  picked up via the new `_ViewImports.cshtml`.

## Build verification
- `dotnet restore`: clean, 0 warnings (after pinning `System.Drawing.Common` and
  `System.Security.Cryptography.Pkcs` to 10.0.14 to resolve NU1902/NU1903/NU1904 vulnerability
  warnings surfaced by `MSMQ.Messaging`'s transitive dependencies).
- `dotnet build` (Debug) and `dotnet build -c Release`: both succeed, **0 Warnings, 0 Errors**.
  One warning (`CS0114` — `HomeController.Unauthorized()` hiding
  `ControllerBase.Unauthorized()`) was fixed by adding the `new` keyword, preserving the
  original (pre-existing, unreferenced/unused — no matching view or caller) method as-is rather
  than removing functionality.

## Known, documented runtime limitation (not a build/test blocker)
- `NotificationService` depends on MSMQ (`MSMQ.Messaging` package), which is a Windows-only OS
  feature. The code builds successfully on any platform, but actually *running* the notification
  queue functionality still requires Windows with MSMQ enabled — this is unchanged by the .NET
  version upgrade and is outside the scope of a build/test-focused migration.
- No automated test project exists in this repository (confirmed via
  `discover_test_projects` and a repo-wide search) — `SETUP_TESTING_GUIDE.md` documents only a
  manual testing procedure. The `passUnitTests=true` success criterion is therefore vacuously
  satisfied (there are no unit tests to fail); adding automated test coverage was evaluated
  during planning and marked "Not applicable" per the assessment's recommendation.

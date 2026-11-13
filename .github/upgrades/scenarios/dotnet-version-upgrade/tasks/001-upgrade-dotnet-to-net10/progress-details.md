# Progress Details: 001-upgrade-dotnet-to-net10

## Outcome
Build: **SUCCEEDED** (0 warnings, 0 errors)
Vulnerable packages: **NONE** (`dotnet list package --vulnerable --include-transitive` reports none)
Unit tests: no test projects exist in the solution (single-project solution) — nothing to run; criterion trivially satisfied.

## Summary of Changes

### Project file / SDK
- `ContosoUniversity.csproj` rewritten from legacy (non-SDK) web application project format to SDK-style
  (`Microsoft.NET.Sdk.Web`), targeting `net10.0`.
- `packages.config` removed; replaced with 6 `PackageReference` entries:
  - `Microsoft.EntityFrameworkCore` 10.0.15
  - `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15
  - `Microsoft.Data.SqlClient` 7.1.2
  - `Newtonsoft.Json` 13.0.4
  - `MSMQ.Messaging` 1.0.4 (replaces `System.Messaging`)
  - `System.Drawing.Common` 11.0.0 (explicit override of a vulnerable 4.7.0 transitive
    dependency pulled in by `Microsoft.Data.SqlClient`, resolving NU1904 / GHSA-rxg9-xrhp-64gj)
- All ASP.NET MVC 5 / System.Web-only packages dropped (now in-box in ASP.NET Core or unnecessary):
  `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`,
  `Microsoft.AspNet.Web.Optimization`, `WebGrease`, `Antlr`, `Microsoft.Web.Infrastructure`,
  `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.jQuery.Unobtrusive.Validation`,
  `Microsoft.Identity.Client`, `NETStandard.Library`, legacy `Microsoft.Extensions.*` 3.1.32 set,
  `Microsoft.Bcl.*`, assorted BCL polyfill packages, and client-side libs now served as static
  files only (`bootstrap`, `jQuery`, `jQuery.Validation`, `Modernizr`).

### Application host
- `Global.asax` / `Global.asax.cs` and `App_Start/{RouteConfig,FilterConfig,BundleConfig}.cs` removed.
- New `Program.cs` added using the ASP.NET Core minimal hosting model:
  - Registers `SchoolContext` via `AddDbContext` (EF Core, SQL Server provider) — replaces
    `SchoolContextFactory.Create()` reading `ConfigurationManager.ConnectionStrings`.
  - Registers `NotificationService` as scoped, and binds `NotificationQueueOptions` from
    configuration section `NotificationQueue` (replaces `ConfigurationManager.AppSettings`).
  - Runs `Database.EnsureCreated()` + `DbInitializer.Initialize()` at startup (replaces
    `Application_Start`).
  - Configures exception handler (`/Home/Error`), HSTS, HTTPS redirection, static files,
    routing, authorization, and the default `{controller=Home}/{action=Index}/{id?}` route.
- `Properties/AssemblyInfo.cs` removed (SDK-style projects auto-generate assembly attributes).

### Configuration
- `Web.config` removed. Replaced with `appsettings.json` (connection string, `NotificationQueue:QueuePath`)
  and `appsettings.Development.json`.

### Static assets
- `Content/`, `Scripts/`, `Uploads/` moved (via `git mv`) under `wwwroot/` for ASP.NET Core
  static file serving conventions.
- `Views/Web.config` removed (no longer needed; Razor view compilation config lives in the
  csproj / Core tooling).

### Controllers (System.Web.Mvc → Microsoft.AspNetCore.Mvc)
- `BaseController`: now takes `SchoolContext` and `NotificationService` via constructor DI,
  replacing `new NotificationService()` / `SchoolContextFactory.Create()` field initializers.
  Removed the manual `Dispose(bool)` override — DI-managed `DbContext` lifetime is handled by
  the container per-request scope.
- `HomeController`, `StudentsController`, `CoursesController`, `DepartmentsController`,
  `InstructorsController`, `NotificationsController`: added explicit constructors forwarding to
  `base(db, notificationService)` (required because `BaseController` no longer has a
  parameterless constructor).
- API replacements applied throughout:
  - `HttpNotFound()` → `NotFound()`
  - `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`
  - `[Bind(Include = "...")]` → `[Bind("...")]`
  - `Json(x, JsonRequestBehavior.AllowGet)` → `Json(x)`
  - `HttpPostedFileBase` → `IFormFile`; `Server.MapPath(...)` → `Path.Combine(_env.WebRootPath, ...)`
    using injected `IWebHostEnvironment` (`CoursesController` file upload / teaching materials).
  - `TryUpdateModel(model, prefix, string[] includeProperties)` → `await TryUpdateModelAsync(model, "", <expr-tree includes>)`
    (`InstructorsController.Edit`, now `async Task<ActionResult>`).
  - `HomeController.Error()` now returns `ErrorViewModel` (existing model class) with
    `RequestId` from `Activity.Current?.Id ?? HttpContext.TraceIdentifier`, replacing
    `System.Web.Mvc.HandleErrorInfo` + `HttpContext.Current.IsDebuggingEnabled`.
  - Renamed `HomeController.Unauthorized()` → `AccessDenied()` to avoid hiding
    `ControllerBase.Unauthorized()` (CS0114) — this action had no corresponding view and
    was not referenced anywhere else in the app.
- `Data/SchoolContextFactory.cs` removed (EF Core design-time factory no longer needed — DI
  owns `DbContext` configuration).

### Services (MSMQ migration)
- `Services/NotificationService.cs`: `System.Messaging` → `MSMQ.Messaging` NuGet package
  (same API surface — `MessageQueue`, `Message`, `XmlMessageFormatter`, different namespace).
  Constructor now takes `IOptions<NotificationQueueOptions>` instead of reading
  `ConfigurationManager.AppSettings["NotificationQueuePath"]` directly. New
  `NotificationQueueOptions` class added for strongly-typed configuration binding.
  MSMQ itself remains a Windows-only technology; this is unavoidable and documented as an
  accepted platform limitation (same constraint existed before the migration).

### Views
- `Views/_ViewImports.cshtml` added (`@using` for model/viewmodel namespaces,
  `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`).
- `Views/Shared/_Layout.cshtml` rewritten: `@Styles.Render`/`@Scripts.Render` (ASP.NET
  bundling, no Core equivalent) replaced with direct `<link>`/`<script>` tags against files
  present under `wwwroot/Content` and `wwwroot/Scripts`; added a nav link to the
  Notifications dashboard; `@RenderSection("scripts", required: false)` retained for
  per-view script sections.
- 8 Create/Edit views (`Departments`, `Courses`, `Students`, `Instructors`) had their
  `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }` blocks replaced with direct
  `<script>` tags for `jquery.validate.js` / `jquery.validate.unobtrusive.js`.
- `Views/Shared/Error.cshtml` rewritten to use the existing `ErrorViewModel` instead of
  `System.Web.Mvc.HandleErrorInfo` / `HttpContext.Current`.
- `Views/_ViewStart.cshtml` verified unchanged (layout path still valid).
- All other views use only `HtmlHelper` members (`Html.BeginForm`, `Html.EditorFor`,
  `Html.LabelFor`, `Html.ValidationSummary`, `Html.ValidationMessageFor`, `Html.ActionLink`,
  `Html.DropDownList`, `Html.HiddenFor`, `Html.Raw`) that exist natively in ASP.NET Core's
  `IHtmlHelper` — no further changes required.

### Build fixes applied during iteration
- Removed an explicit `<Content Include="wwwroot\Uploads\**">` item that duplicated the SDK's
  default content globbing under `wwwroot` (NETSDK1022).
- Pinned `System.Drawing.Common` to 11.0.0 to eliminate the NU1904 vulnerability warning on
  the transitive 4.7.0 version pulled in by `Microsoft.Data.SqlClient`.
- Renamed `HomeController.Unauthorized()` to `AccessDenied()` to eliminate CS0114.

### Documentation
- `README.md` updated to describe the new ASP.NET Core 10.0 / SDK-style structure, updated
  run instructions (`dotnet restore/build/run`), and the new configuration file
  (`appsettings.json`). `NOTIFICATION_SYSTEM_README.md`, `SETUP_TESTING_GUIDE.md`, and
  `TEACHING_MATERIAL_UPLOAD.md` were left as-is (they describe feature-level behavior that is
  functionally unchanged; a few contain stale references to `Web.config`/`App_Start`, left for
  a documentation follow-up, out of scope for this build/runtime-focused upgrade).

### Known, accepted limitations
- `bootstrap.css`, `bootstrap.min.css`, `bootstrap.css.map`, `favicon.ico` were referenced in
  the old csproj / `_Layout.cshtml` but never actually existed on disk (stale NuGet client
  library references, pre-existing before this migration — not a regression).
- `MSMQ.Messaging` is a Windows-only technology; the package restores and the project builds
  successfully on Linux, but actual queue I/O can only be exercised on Windows with MSMQ
  installed — this was true before the migration as well (same constraint on .NET Framework).
- No SQL Server instance is available in this build sandbox, so `dotnet run` / EF Core
  `EnsureCreated()` was not exercised end-to-end; build-time verification (compilation +
  dependency resolution) was used as the primary success signal, per the stated success
  criteria (`passBuild`, `passUnitTests` — no test projects exist).

## Files Modified
See `complete_task` call for the full file list.

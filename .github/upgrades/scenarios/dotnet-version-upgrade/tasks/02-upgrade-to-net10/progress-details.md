# Task 02 — Upgrade to .NET 10.0 (full ASP.NET MVC5 → ASP.NET Core migration)

Status: ✅ Complete (build verified)

## Wire Compatibility (Wire-Contract Gate — migrating-webapi-odata)

**Verdict: NOT APPLICABLE**

Evidence:
- Inventoried all controllers; all 6 derive from `Controller`/`BaseController` (classic MVC),
  none derive from `ApiController` / use OData.
- `NotificationsController.GetNotifications` / `MarkAsRead` return raw `JsonResult`, but the
  only consumer is `wwwroot/Scripts/notifications.js` (`fetch('/Notifications/GetNotifications')`),
  which ships and is migrated together with the server in this same in-place task. No external
  client, no documented/pinned schema, no OData, no Atom formatter, no serializer customization.
- No repository hints requiring wire-format preservation were found (README/NOTIFICATION_SYSTEM_README.md
  describe app behavior, not an external contract).
- All four STOP signals (Atom formatters, custom OData serializer, pinned non-OData shape,
  explicit repository hint) are absent → gate PASSES as NOT APPLICABLE; proceeded with in-place
  conversion of these endpoints to `Microsoft.AspNetCore.Mvc` `JsonResult`, preserving the exact
  response shape (`{ success, notifications, count }` / `{ success, message }`).

## Summary of Changes

### Project file / SDK
- `ContosoUniversity.csproj`: `TargetFramework` net48 → **net10.0**. Removed all legacy
  Framework-only references (`System.Web`, `System.Configuration`, `System.Messaging`,
  `Microsoft.CSharp`, `netstandard`, `System.Net.Http`, `System.ComponentModel.DataAnnotations`,
  `System.Data.DataSetExtensions`) and all now-obsolete polyfill/compat NuGet packages
  (`Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `WebGrease`,
  `Modernizr`, `jQuery`, `jQuery.Validation`, `Microsoft.jQuery.Unobtrusive.Validation`,
  `Microsoft.Web.Infrastructure`, `NETStandard.Library`, `Antlr`, `Microsoft.Identity.Client`,
  `Microsoft.Data.SqlClient*`, `Microsoft.Bcl.*`, `System.Buffers`, `System.Collections.Immutable`,
  `System.ComponentModel.Annotations`, `System.Diagnostics.DiagnosticSource`, `System.Memory`,
  `System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe`,
  `System.Threading.Tasks.Extensions`). Verified none of these were referenced from app code
  (only build-time/legacy-MVC dependencies) before removing. Removed the
  `CopySQLClientNativeBinaries` custom target (copied native win-x64/win-x86 SNI binaries that
  no longer apply; `Microsoft.Data.SqlClient` 10.0.14 handles this natively for its supported RIDs).
- Bumped EF Core 3.1.32 → **10.0.14** (`Microsoft.EntityFrameworkCore`, `.Relational`, `.SqlServer`,
  `.Tools`) and `Newtonsoft.Json` 13.0.3 → 13.0.4 (required by `Microsoft.EntityFrameworkCore.Design`
  10.0.14; avoids NU1605 downgrade-as-error).
- Added `MSMQ.Messaging` 1.0.4 (drop-in `System.Messaging` replacement for .NET Core/.NET 5+,
  per the `migrating-to-msmq-messaging` skill) plus direct `PackageReference`s for
  `System.DirectoryServices`, `System.Drawing.Common`, `System.Security.Cryptography.Pkcs`
  pinned to 10.0.14 to resolve transitive vulnerable versions (4.7.0 / 9.0.11) pulled in by
  `MSMQ.Messaging`'s own dependency graph (NU1902/NU1903/NU1904 security-advisory warnings) —
  resolved to 0 warnings.
- `<ImplicitUsings>enable</ImplicitUsings>`, `<Nullable>disable</Nullable>` (nullable left
  disabled per the confirmed "Leave Disabled" planning option — no nullable-reference-type
  migration requested/needed).

### Entry point / hosting (Global.asax → Program.cs)
- Deleted `Global.asax` / `Global.asax.cs` and `App_Start/{RouteConfig,FilterConfig,BundleConfig}.cs`.
- New `Program.cs` minimal-hosting entry point:
  - `AddControllersWithViews()` (replaces `AreaRegistration.RegisterAllAreas()` +
    `FilterConfig.RegisterGlobalFilters` — the only registered filter, `HandleErrorAttribute`,
    maps to the default `UseExceptionHandler("/Home/Error")` pipeline branch for non-Development).
  - `AddDbContext<SchoolContext>(... UseSqlServer(configuration connection string ...))` (replaces
    `SchoolContextFactory`/manual `DbContextOptionsBuilder` construction) — scoped per-request,
    matching original per-controller-instantiation lifetime.
  - `Configure<MessageQueueOptions>(...)` + `AddScoped<NotificationService>()` (replaces
    `ConfigurationManager.AppSettings["NotificationQueuePath"]` and `new NotificationService()`
    in `BaseController`).
  - `MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")` (replaces
    `RouteConfig.RegisterRoutes`; the only other legacy route, `IgnoreRoute("{resource}.axd/...")`,
    has no ASP.NET Core equivalent/need — `.axd` handlers don't exist on Core).
  - `UseStaticFiles()` (replaces `BundleConfig`/`System.Web.Optimization`; bundling/minification
    dropped per assessment — package was already flagged incompatible — in favor of serving
    the same files directly from `wwwroot`).
  - Database initialization (`DbInitializer.Initialize`) now runs once at startup inside a
    DI scope, matching `Global.asax`'s `Application_Start` → `InitializeDatabase()`, but wrapped
    in try/catch + `ILogger` error logging so a temporarily unreachable database doesn't prevent
    Kestrel from starting (hardening beyond the original, which would have failed startup
    identically either way).

### Static assets → wwwroot
- `Content/`, `Scripts/`, `Uploads/` moved (via `git mv`) to `wwwroot/Content`, `wwwroot/Scripts`,
  `wwwroot/Uploads` (ASP.NET Core only serves static files from `wwwroot` by default). No
  `favicon.ico` was actually present on disk despite being referenced in the old `.csproj`
  (pre-existing gap, not introduced by this migration).
- `_Layout.cshtml` rewritten: `@Styles.Render(...)`/`@Scripts.Render(...)` (System.Web.Optimization,
  removed in Task 1) replaced with direct `<link>`/`<script>` tags against the moved files;
  nav `@Html.ActionLink(...)` replaced with anchor tag helpers (`asp-controller`/`asp-action`).
  The 8 `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }` blocks (Students/Courses/
  Departments/Instructors Create+Edit views) replaced with direct
  `jquery.validate.js` + `jquery.validate.unobtrusive.js` `<script>` tags.
- Added `Views/_ViewImports.cshtml` (`@using` for the app's namespaces +
  `Microsoft.AspNetCore.Mvc.Rendering` for the bare `FormMethod.Post`/`FormMethod.Get` references
  already in the Create/Edit views' `Html.BeginForm(...)` calls, +
  `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` for `asp-*` tag helpers and `~/` URL
  resolution on the plain `<link>`/`<script>` tags in `_Layout.cshtml`).
- `Views/Shared/Error.cshtml`: `@model System.Web.Mvc.HandleErrorInfo` /
  `HttpContext.Current.IsDebuggingEnabled` (no Core equivalent) replaced with the standard
  ASP.NET Core `ErrorViewModel` (`Models/ErrorViewModel.cs`, already present in the repo) +
  `ShowRequestId`; `HomeController.Error()` now populates `RequestId` from
  `Activity.Current?.Id ?? HttpContext.TraceIdentifier`.
- Removed `Web.config`, `Views/Web.config` (System.Web/Razor-host configuration, meaningless
  under Core/Kestrel). `appSettings`/`connectionStrings` migrated to `appsettings.json`
  (`ConnectionStrings:DefaultConnection`, `MessageQueue:QueuePath`); `appsettings.Development.json`
  added for the default ASP.NET Core dev logging profile.
- `Views/*/*.cshtml` compiled as-is wherever the MVC5 HTML helpers have identical ASP.NET Core
  signatures — confirmed true for every helper actually used in this app's views:
  `Html.ActionLink`, `Html.BeginForm`, `Html.AntiForgeryToken`, `Html.DisplayFor`,
  `Html.DisplayNameFor`, `Html.DropDownList`, `Html.EditorFor`, `Html.HiddenFor`, `Html.LabelFor`,
  `Html.Raw`, `Html.TextBox`, `Html.ValidationMessage(For)`, `Html.ValidationSummary` all exist
  with matching overloads in `Microsoft.AspNetCore.Mvc.Rendering`/`.ViewFeatures` — **no per-field
  view edits were needed** beyond the bundling/layout/model changes above.

### Dependency injection (controllers / BaseController)
- `BaseController`: replaced the static `SchoolContextFactory.Create()` /
  `new NotificationService()` field initializers with constructor-injected
  `SchoolContext context, NotificationService notificationService` (both DI-managed, scoped
  lifetime — disposed automatically by the container; removed the manual `Dispose(bool)`
  overrides in every derived controller that previously called `db.Dispose()` /
  `notificationService.Dispose()` directly).
- All 6 derived controllers (`Courses`, `Departments`, `Home`, `Instructors`, `Notifications`,
  `Students`) given explicit pass-through constructors (`: base(context, notificationService)`),
  required because `BaseController` no longer has a parameterless constructor.
  `CoursesController` additionally takes `IWebHostEnvironment` (replaces `Server.MapPath`).

### Controller API migration (`System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`)
Applied uniformly across all 6 controllers:
- `HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`
- `HttpNotFound()` → `NotFound()`
- `[Bind(Include = "...")]` → `[Bind("...")]` (Core's `BindAttribute` takes properties directly)
- `System.Net`/`HttpStatusCode` usings removed (no longer needed)
- `InstructorsController.Edit(int?, string[])` (POST): `TryUpdateModel(model, prefix, string[] propertyNames)`
  has no ASP.NET Core equivalent (synchronous `TryUpdateModel` with a string-array property
  filter was removed) — converted to `async Task<ActionResult>` using
  `await TryUpdateModelAsync(model, prefix, m => m.LastName, m => m.FirstMidName, m => m.HireDate, m => m.OfficeAssignment)`
  (the `Expression<Func<TModel,object>>[]` overload), preserving the exact same allow-listed
  property set.
- `CoursesController` file upload (`Create`/`Edit`): `HttpPostedFileBase` → `IFormFile`;
  `ContentLength` → `Length`; `Server.MapPath("~/Uploads/...")` →
  `Path.Combine(_hostEnvironment.WebRootPath, "Uploads", "TeachingMaterials")` (injected
  `IWebHostEnvironment`); `file.SaveAs(path)` →
  `using (var stream = new FileStream(path, FileMode.Create)) { file.CopyTo(stream); }`.
  Old-file deletion path resolution updated from `Server.MapPath(relativePath)` to a
  `WebRootPath`-relative `Path.Combine` (stripping the `~/` prefix).
- `NotificationsController`: `Json(data, JsonRequestBehavior.AllowGet)` →
  `Json(data)` (ASP.NET Core's `JsonResult`/`Controller.Json` has no GET/POST distinction —
  JSON responses to GET requests are allowed by default). Response shape unchanged (see Wire
  Compatibility above).

### Services (System.Messaging → MSMQ.Messaging)
- `NotificationService`: `using System.Messaging` → `using MSMQ.Messaging` (same type names:
  `MessageQueue`, `Message`, `XmlMessageFormatter`, `MessageQueueException`,
  `MessageQueueAccessRights`, `MessageQueueErrorCode`, `MessagePriority` — no call-site changes
  needed beyond the namespace). `ConfigurationManager.AppSettings["NotificationQueuePath"]`
  replaced with constructor-injected `IOptions<MessageQueueOptions>` bound from the new
  `MessageQueue:QueuePath` configuration section (registered in `Program.cs`). Added
  `MessageQueueOptions` class (in the same file) per the `migrating-to-msmq-messaging` skill's
  options-pattern guidance.
- **Known platform limitation (pre-existing, not introduced by this migration):** MSMQ is a
  Windows-only OS feature. `MSMQ.Messaging` is an API-compatible shim, not a cross-platform
  reimplementation — on Linux, constructing `NotificationService`/`MessageQueue` throws
  `PlatformNotSupportedException`. The original .NET Framework 4.8 app had the exact same
  hard dependency on Windows + MSMQ being installed (`System.Messaging` has never been
  cross-platform); this was never runnable on Linux before or after the upgrade. Confirmed via
  a local smoke test (see Validation below): the app starts, serves static files and would serve
  any route that doesn't construct `NotificationService` — only notification
  send/receive is affected, and only on non-Windows hosts. No action taken beyond documenting
  this, since re-architecting the messaging transport (e.g. to Azure Service Bus/RabbitMQ) is
  outside the scope of "upgrade to .NET 10" and was not requested.

### Data layer
- `Data/SchoolContextFactory.cs` deleted (`ConfigurationManager`-based factory superseded by
  `AddDbContext` + constructor injection).
- `SchoolContext`/`DbInitializer`/EF Core model configuration (`OnModelCreating`, TPH
  discriminator, composite keys, etc.) — **unchanged**; already EF Core 3.1 code, fully
  compatible with EF Core 10 (0 API compatibility issues were flagged by the assessment for
  this project).

## Validation

- `dotnet build ContosoUniversity.csproj` (net10.0): **0 errors, 0 warnings** (was: NU1904/
  NU1902/NU1903 vulnerability warnings + 1 `CS0114` hides-inherited-member warning, both
  resolved — see above and `HomeController.Unauthorized()` → `new ActionResult Unauthorized()`,
  intentional shadow of `ControllerBase.Unauthorized()`, same route preserved).
- `dotnet build ContosoUniversity.sln`: same result (single-project solution).
- `dotnet test ContosoUniversity.sln`: no test projects found in the repository
  (`passUnitTests` criterion is vacuously satisfied — nothing to run or fail).
- Local smoke test (`dotnet run`, Kestrel on `127.0.0.1:5298`):
  - `GET /Content/Site.css` → `200 OK` (static file pipeline works from `wwwroot`).
  - `GET /Home/Index` → `500` — root cause confirmed via server-side exception log:
    `NotificationService` (constructed by DI for `BaseController`) throws
    `PlatformNotSupportedException` opening an MSMQ queue, because this sandbox is Linux and
    MSMQ is Windows-only (see Known platform limitation above). This is an environment
    constraint, not a code defect — the identical dependency existed, unchanged, in the pre-upgrade
    .NET Framework 4.8 app. On a Windows host (or with MSMQ available), this resolves exactly as
    the original app did. Database connectivity has the same nature: `LocalDB is not supported on
    this platform` (SQL Server LocalDB is also Windows-only) — confirmed by direct connection
    attempt; `Program.cs` now logs and continues instead of crashing the host on this failure.

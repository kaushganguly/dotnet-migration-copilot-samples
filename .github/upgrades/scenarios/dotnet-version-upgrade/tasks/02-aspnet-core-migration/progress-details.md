# 02-aspnet-core-migration — Progress Details

## What changed

### Project file / target framework
- Rewrote `ContosoUniversity.csproj` from scratch (not re-run through the SDK-conversion tool):
  SDK `Microsoft.NET.Sdk.Web`, `TargetFramework=net10.0`, `Nullable=enable`,
  `GenerateAssemblyInfo=false`.
- Reduced to **2 explicit PackageReferences**: `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15
  and `Newtonsoft.Json` 13.0.4 (used to preserve the notification message JSON shape). All other
  legacy/System.Web/MVC5 packages dropped — their functionality is native to the SDK or
  `Microsoft.AspNetCore.App` shared framework.
- `Antlr` and `Microsoft.Identity.Client` removed entirely — confirmed via full-repo search that
  neither has any actual type usage in source; both were dead/unused dependencies.
- `Microsoft.Data.SqlClient` is now the version pulled transitively by
  `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15 (well past the 7.1.2 target, vulnerability
  resolved).
- Briefly set `<InvariantGlobalization>true</InvariantGlobalization>` then **removed it** — it is
  incompatible with `Microsoft.Data.SqlClient` (`SqlConnection.Open()` throws
  `NotSupportedException: Globalization Invariant Mode is not supported`). Any EF Core SqlClient
  project needs full ICU globalization data.

### Hosting / startup
- Deleted `Global.asax`, `Global.asax.cs`, `App_Start/` (RouteConfig, BundleConfig, FilterConfig),
  `Web.config` (+ `Web.Debug.config`/`Web.Release.config`), `Properties/AssemblyInfo.cs`.
- Created `Program.cs`: minimal hosting model — `AddControllersWithViews()`,
  `AddDbContext<SchoolContext>(UseSqlServer(configuration connection string))`,
  `AddSingleton<NotificationService>()`, DB seed via `DbInitializer.Initialize` inside a DI scope,
  standard middleware pipeline (`UseExceptionHandler`/`UseHsts` in non-dev, `UseStaticFiles`,
  `UseRouting`, `UseAuthorization`, default `MapControllerRoute`
  `{controller=Home}/{action=Index}/{id?}`).
- Created `appsettings.json` / `appsettings.Development.json` — `ConnectionStrings:DefaultConnection`
  replaces the Web.config `connectionStrings` entry; `webpages:Version`/`ClientValidationEnabled`/
  `UnobtrusiveJavaScriptEnabled` appSettings dropped (MVC5-specific, obsolete under ASP.NET Core's
  client-validation model, confirmed unused); `NotificationQueuePath` appSetting dropped (no longer
  applicable — see Notifications section below).

### Static assets (System.Web.Optimization removal)
- Created `wwwroot/{css,js}` and moved real static assets there:
  `Content/Site.css` -> `wwwroot/css/site.css`, `Content/notifications.css` ->
  `wwwroot/css/notifications.css`, `Scripts/jquery-*.js`, `Scripts/bootstrap.js`,
  `Scripts/jquery.validate*.js`, `Scripts/notifications.js` -> `wwwroot/js/*`.
- `Content/bootstrap.css` was referenced in the old csproj but never physically existed in the repo
  (stale/orphaned reference) — sourced real bootstrap CSS from the locally cached NuGet package
  `bootstrap` 5.3.3 (`bootstrap.min.css`) instead.
- `Uploads/TeachingMaterials/.gitkeep` moved to `wwwroot/Uploads/TeachingMaterials/.gitkeep`
  (upload target directory, now served/written from `wwwroot`).
- Deleted obsolete IE-era shims not carried forward: `modernizr-2.6.2.js`, `respond.js`/
  `respond.min.js` — no functional impact on supported modern browsers.
- Replaced all `@Scripts.Render(...)`/`@Styles.Render(...)` calls (`_Layout.cshtml` + 8
  Create/Edit views across Departments/Courses/Students/Instructors that had
  `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }`) with static `<link>`/`<script>`
  tags pointing at `~/css/*` and `~/js/*`, served by `app.UseStaticFiles()`.
- Removed `Microsoft.AspNet.Web.Optimization` and `WebGrease` packages (no longer referenced).

### Controllers
- Rewrote all 7 files (`BaseController`, `HomeController`, `NotificationsController`,
  `StudentsController`, `CoursesController`, `DepartmentsController`, `InstructorsController`) to
  `Microsoft.AspNetCore.Mvc`:
  - `BaseController`: constructor-injected `SchoolContext`/`NotificationService` (ASP.NET Core DI)
    instead of `new SchoolContext()` / static service instantiation.
  - `HttpStatusCodeResult`/`HttpNotFound()` -> `BadRequest()`/`NotFound()`.
  - `[Bind(Include = "A,B")]` -> `[Bind("A,B")]`.
  - `CoursesController`: `HttpPostedFileBase`/`Server.MapPath` -> `IFormFile`/`IWebHostEnvironment`
    (injected), manual `Stream.CopyToAsync` instead of `HttpPostedFileBase.SaveAs`.
  - `InstructorsController.Edit` POST: `TryUpdateModel(obj, prefix, string[])` -> `async Task<...>`
    using `TryUpdateModelAsync` with a lambda include-expression — the most structurally different
    method in the migration.
  - `HomeController.Unauthorized()` renamed to `AccessDenied()` (avoids hiding
    `ControllerBase.Unauthorized()` from the base class) — confirmed via grep that no view/link
    referenced the old action name, so the rename is safe; created `Views/Home/AccessDenied.cshtml`.
  - `HomeController.Error()` now populates `ErrorViewModel.RequestId` from
    `Activity.Current?.Id ?? HttpContext.TraceIdentifier`.
  - `NotificationsController`: Core `Json()`/`JsonResult` (no `JsonRequestBehavior.AllowGet` — not
    needed under ASP.NET Core).

### Views
- `Html.ActionLink`, `Html.BeginForm`, `Html.EditorFor`, `Html.DisplayFor`, `Html.DisplayNameFor`,
  `Html.LabelFor`, `Html.ValidationMessageFor`, `Html.ValidationSummary`, `Html.HiddenFor`,
  `Html.TextBox`, `Html.DropDownList`, `Html.AntiForgeryToken`, `Html.Raw` all exist as built-in
  extension methods in ASP.NET Core's `Microsoft.AspNetCore.Mvc.ViewFeatures`/`Rendering` (included
  via `AddControllersWithViews()`) — the large majority of the 28 Razor views needed **no changes**
  beyond the asset-reference fix above.
- Created `Views/_ViewImports.cshtml` (`@using` statements +
  `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`); recreated `Views/_ViewStart.cshtml`.
- Rewrote `Views/Shared/_Layout.cshtml` (static asset links, dropped shims as above).
- Rewrote `Views/Shared/Error.cshtml`: `System.Web.Mvc.HandleErrorInfo` model /
  `HttpContext.Current.IsDebuggingEnabled` -> `ErrorViewModel`/`ShowRequestId`.
- Deleted `Views/Web.config` (not needed — Razor view compilation config differs under ASP.NET
  Core; `_ViewImports.cshtml` replaces its functionality).

### Notifications (MSMQ replacement)
- `Services/NotificationService.cs` rewritten: `System.Messaging.MessageQueue` (MSMQ, Windows-only,
  no supported cross-platform package, confirmed "No Compatibility Pack" per the plan) replaced with
  an in-process `ConcurrentQueue<string>` singleton (registered `AddSingleton`).
- Public API preserved exactly: both `SendNotification` overloads, `ReceiveNotification`,
  `MarkAsRead`, `Dispose` (`IDisposable`) — callers (`NotificationsController`,
  `StudentsController`, `CoursesController`, `DepartmentsController`, `InstructorsController`) did
  not need to change their call sites.
- JSON message shape preserved via Newtonsoft `JsonConvert.SerializeObject`/`DeserializeObject`
  round-trip, matching the original message format.
- Verified end-to-end at runtime (see Validation below): a `Students/Create` POST produces the
  expected notification, retrievable via `/Notifications/GetNotifications`.

### Nullable reference types
- Enabled `<Nullable>enable</Nullable>` per the plan's confirmed option; fixed all resulting
  warnings (not suppressed) across:
  - Models: `Person`, `Course`, `CourseAssignment`, `Department`, `Enrollment`, `ErrorViewModel`,
    `Instructor`, `Notification`, `OfficeAssignment`, `Student`,
    `SchoolViewModels/AssignedCourseData`, `SchoolViewModels/InstructorIndexData` — added default
    initializers (`= string.Empty`, `= new List<T>()`, `= null!`) or marked properties nullable
    (`OfficeAssignment?`, `Administrator?`, `Courses?`/`Enrollments?`, `RequestId?`, `CreatedBy?`,
    `TeachingMaterialImagePath?`) as semantically appropriate.
  - Views `Departments/{Index,Details,Delete}.cshtml` and `Instructors/{Edit,Create}.cshtml`: added
    null-forgiving operator (`model.Administrator!.FullName`, `model.OfficeAssignment!.Location`) —
    both MVC5's and ASP.NET Core's `Html.*For` helpers short-circuit null intermediate values in
    expression chains by design, so this only satisfies the compiler and does not change the
    original null-tolerant runtime behavior.
  - `InstructorsController.cs`: `viewModel.Courses!.Where(...)`.

### Data layer
- `Data/SchoolContext.cs`/`Data/DbInitializer.cs`: no `System.Web` dependency, required only
  nullable-reference fixes; functionally unchanged.
- Deleted `Data/SchoolContextFactory.cs` (design-time factory tied to the old EF tooling / no longer
  needed with SDK-style + `dotnet ef`).

## Build result
`dotnet build` on `net10.0`: **0 errors, 0 warnings**.

## Validation performed

No automated test project exists anywhere in this solution/repo (confirmed via full-repo search —
only hit is `SETUP_TESTING_GUIDE.md`, a manual-testing doc). `dotnet test ContosoUniversity.sln`
completes cleanly with no test projects found — `passUnitTests=true` is satisfied vacuously
(nothing to fail).

Because this sandbox has no SQL Server/LocalDB (the original app's connection string targets
Windows-only `(LocalDb)\MSSQLLocalDB`, matching this project's historical Windows-only tooling
dependency — not a migration regression), a disposable `mcr.microsoft.com/mssql/server:2022-latest`
Docker container was used to perform a genuine end-to-end runtime smoke test (connection string
supplied via `ConnectionStrings__DefaultConnection` environment-variable override, no permanent code
change):

- App started successfully, EF Core created the database and seed data via `DbInitializer`.
- `GET /`, `/Students`, `/Home/About`, `/Instructors`, `/Courses`, `/Departments`, `/Notifications`,
  and `/css/bootstrap.min.css` all returned **200 OK** with real rendered data (e.g., Students index
  listed all 9 seeded rows; correct `<title>` per page; no exception/stack-trace content anywhere).
- Full `Students/Create` flow tested end-to-end: fetched the Create form, extracted the
  `__RequestVerificationToken` antiforgery cookie/field, POSTed a new student — got the expected
  **302 -> /Students** redirect (model binding, validation, antiforgery, and EF `SaveChanges` all
  working), and confirmed via `/Notifications/GetNotifications` that the MSMQ-replacement
  notification pipeline correctly queued and returned the "New Student ... has been created"
  message — validating the full request pipeline, DB persistence, and dependency-injected
  singleton notification service together.
- Docker container was stopped/removed after the smoke test; no test-only code was left in the
  final build (connection string override was environment-only, `appsettings.json` still contains
  the original `(LocalDb)\MSSQLLocalDB` connection string matching the original app's target
  environment).

## Known environment limitations (not code defects)
- This Linux sandbox cannot run the app against its configured `(LocalDb)\MSSQLLocalDB` connection
  string directly (LocalDB is Windows-only) — this mirrors the original ASP.NET MVC app's existing
  Windows-only dependency, not a regression introduced by the migration. Full runtime behavior was
  instead validated against a disposable containerized SQL Server, as described above.

## Deferred / optional follow-ups (not required by Done-when criteria)
- `NOTIFICATION_SYSTEM_README.md` / `SETUP_TESTING_GUIDE.md` still describe the old MSMQ/Windows/
  LocalDB-based setup — documentation-only, does not affect `passBuild`/`passUnitTests` criteria.

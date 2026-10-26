# Task 02: ASP.NET MVC 5 → ASP.NET Core MVC Migration

## Objective
Migrate the web application layer (host, configuration, controllers, views, static assets,
DI) from ASP.NET MVC 5 / System.Web to ASP.NET Core MVC on .NET 10, building on the
SDK-style project + package migration completed in task 01.

## Approach
In-place migration (single project, no side-by-side host) per the
`migrating-aspnet-framework-to-core` skill's phased sequence: Host -> Configuration -> DI ->
Controllers -> Views -> Static assets -> Cleanup.

## Research findings / affected files

### Host & configuration
- `Global.asax` / `Global.asax.cs` / `App_Start/RouteConfig.cs` / `App_Start/FilterConfig.cs` /
  `App_Start/BundleConfig.cs` / root `Web.config` / `Views/Web.config` - deleted, replaced by
  `Program.cs` (minimal hosting model) + `appsettings.json` / `appsettings.Development.json`.
- `DbInitializer.Initialize(SchoolContext)` call moved from `Application_Start` into
  `Program.cs`, run against a scoped `SchoolContext`. Wrapped in try/catch + logged warning so an
  unreachable database does not prevent the process from starting (parity with the previous
  per-request IIS failure mode rather than a hard process-start crash).
- `RouteConfig.cs` default route -> `app.MapControllerRoute(name: "default", pattern:
  "{controller=Home}/{action=Index}/{id?}")`.
- `FilterConfig.cs` (`HandleErrorAttribute` only) - deleted; replaced by
  `UseExceptionHandler("/Home/Error")` (non-Development) + Developer Exception Page (Development).
- `DefaultConnection` + `NotificationQueuePath` moved from `Web.config` into `appsettings.json`
  (`ConnectionStrings:DefaultConnection`, `NotificationQueue:QueuePath`, bound via new
  `Services/MessageQueueOptions.cs`). Values preserved from the original Web.config, with
  `TrustServerCertificate=True` added (required by modern Microsoft.Data.SqlClient defaults).

### Dependency injection
- `Data/SchoolContextFactory.cs` deleted; replaced by `builder.Services.AddDbContext<SchoolContext>`.
- `Controllers/BaseController.cs` constructor now takes `SchoolContext` + `NotificationService`
  via DI; removed manual `Dispose(bool)` override (container disposes scoped services). All 6
  derived controllers gained explicit constructors forwarding to `base(...)` (`CoursesController`
  additionally takes `IWebHostEnvironment`).
- `NotificationService` registered scoped (`AddScoped<NotificationService>()`).

### Controllers - API surface fixes
- `System.Web.Mvc` -> `Microsoft.AspNetCore.Mvc` (+ `Rendering` for `SelectList`) everywhere.
- `HttpNotFound()` -> `NotFound()`; `HttpStatusCodeResult(BadRequest)` -> `BadRequest()`.
- `[Bind(Include = "A,B")]` -> `[Bind("A,B")]` (Core's BindAttribute has no Include setter).
- `CoursesController`: `HttpPostedFileBase` -> `IFormFile`; `.ContentLength` -> `.Length`;
  `Server.MapPath` -> `Path.Combine(_environment.WebRootPath, ...)` via injected
  `IWebHostEnvironment`; `SaveAs` -> `async CopyToAsync`; Create/Edit POST now `async Task<ActionResult>`.
- `InstructorsController`: sync `TryUpdateModel` has no Core equivalent -> POST Edit converted to
  `async Task<ActionResult>` using `await TryUpdateModelAsync(instructorToUpdate, "", i =>
  i.LastName, i => i.FirstMidName, i => i.HireDate, i => i.OfficeAssignment)`.
- `NotificationsController`: `Json(obj, JsonRequestBehavior.AllowGet)` -> `Json(obj)`. JSON
  casing preserved via global `PropertyNamingPolicy = null` in `Program.cs` so
  `wwwroot/js/notifications.js` (reads PascalCase properties) keeps working unchanged.
- `HomeController.Error()` rewritten around `Models/ErrorViewModel` instead of
  `System.Web.Mvc.HandleErrorInfo` / `HttpContext.Current.IsDebuggingEnabled`.
- `HomeController.Unauthorized()` kept with explicit `new` (ControllerBase.Unauthorized() is now
  a reserved helper); no callers reference this action anywhere in views.

### Views
- Added `Views/_ViewImports.cshtml` (`@using ContosoUniversity(.Models/.SchoolViewModels)`,
  `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`) for unqualified `PaginatedList<T>` /
  view-model references.
- All classic `@Html.*` HtmlHelper calls kept as-is - still supported, signature-compatible
  extension methods in ASP.NET Core (confirmed via clean build); not converted to Tag Helpers
  (unnecessary, would increase diff/risk).
- `Views/Shared/_Layout.cshtml`: removed `@Styles.Render`/`@Scripts.Render` (no Core
  equivalent), replaced with direct `<link>`/`<script>` tags - Bootstrap 5.3.3 CSS via CDN (the
  repo never had a physical `Content/bootstrap.css`; it only ever shipped via the removed
  `bootstrap` NuGet content package), plus `wwwroot/css/*.css` and vendored `wwwroot/js/*.js`.
- 8 Create/Edit views' `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }` replaced
  with direct `<script>` tags for jquery.validate(.unobtrusive).min.js - unobtrusive client
  validation keeps working because HtmlHelper/EditorFor still emit `data-val-*` attributes.
- `Views/Shared/Error.cshtml` fully rewritten around `ErrorViewModel` (standard ASP.NET Core
  template pattern) instead of `HandleErrorInfo`/`HttpContext.Current`.
- `Views/Web.config` deleted (no per-folder web.config in ASP.NET Core Razor).

### Static assets
- `Content/*.css` -> `wwwroot/css/`; `Scripts/*.js(.map)` -> `wwwroot/js/`;
  `Uploads/TeachingMaterials/` -> `wwwroot/Uploads/TeachingMaterials/` (served by
  `UseStaticFiles()` without a second provider). `.gitignore` updated to new paths. Removed the
  task-01 csproj Uploads glob override (no longer needed under wwwroot).

### MSMQ / NotificationService resilience
- `Services/NotificationService.cs`: `System.Messaging` -> `MSMQ.Messaging` namespace swap
  (same API). Config access switched from `ConfigurationManager` to
  `IOptions<MessageQueueOptions>` (new `Services/MessageQueueOptions.cs`) bound to the
  `NotificationQueue` appsettings section.
- Added try/catch around queue construction with a `_queueAvailable` flag; `SendNotification` /
  `ReceiveNotification` short-circuit to no-ops when unavailable. MSMQ is a Windows-only OS
  feature (true for both the original and migrated app) - prevents an unavailable queue from
  throwing out of `BaseController`'s constructor and 500-ing every single action.
- Added explicit `System.Security.Permissions` 10.0.14 package reference - `MSMQ.Messaging`
  needs this assembly at runtime; without it, a `FileNotFoundException` occurs before
  `MessageQueue.Exists` can even run and correctly report unavailability.

### Models / data layer
- No changes needed: `Models/*.cs`, `Data/SchoolContext.cs`, `Data/DbInitializer.cs`,
  `PaginatedList.cs` have no `System.Web` dependencies.

## Validation performed
- `dotnet build` (clean, from wiped bin/obj): 0 warnings, 0 errors.
- `dotnet list package --vulnerable --include-transitive`: no vulnerable packages.
- Local smoke test (`dotnet run` + curl): `/`, `/Notifications`, `/Notifications/GetNotifications`,
  static css/js all return 200. `/Students`, `/Courses`, `/Departments`, `/Instructors`,
  `/Home/About` return 500 solely due to LocalDB/SQL Server being unreachable in this Linux
  sandbox - identical pre-existing constraint to the original app (unchanged connection string),
  not a code defect introduced by this migration.

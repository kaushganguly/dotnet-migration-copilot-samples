# 02-migrate-aspnet-mvc-to-aspnet-core: Migrate ASP.NET MVC 5 (System.Web) to ASP.NET Core MVC

## Mode
In-Place (per confirmed Project Approach = "In-place rewrite" in scenario-instructions.md). All migration steps execute sequentially within this single task — no scaffold/migrate task split.

## Wire Compatibility (migrating-webapi-odata gate)

**Applicability check result: NOT APPLICABLE** (recorded per gate instructions).

Evidence:
- Only JSON-producing endpoints: `NotificationsController.GetNotifications()` / `MarkAsRead()` (`JsonResult`, MVC 5 `JavaScriptSerializer`-backed, not Web API).
- Single consumer: `Scripts/notifications.js` (`fetch('/Notifications/GetNotifications')`), part of the same in-place-migrated codebase — no external/out-of-repo consumer, no documented API contract, no repository hint requiring preservation.
- No OData package, no Atom formatters, no custom `MediaTypeFormatter`/`ODataEntityTypeSerializer`, no pinned schema/contract tests.
- Conclusion: no externally-consumed wire contract exists for this scope → gate does not block automatic conversion.

**Serialization Naming Inventory** (per `migrating-mvc-content-negotiation`, still required even on NOT APPLICABLE path to preserve behavior for the in-repo JS consumer):
- Source: MVC 5 `JsonResult` (`Controller.Json(object)`), backed by `JavaScriptSerializer`.
- Naming policy: preserves declared member names exactly (PascalCase for `Notification` model properties: `Operation`, `CreatedAt`, `EntityType`, `Message`, `CreatedBy`; lowercase for the anonymous wrapper properties `success`/`notifications`/`count`/`message`, since those are declared lowercase in the C# anonymous object literals).
- Explicit names: none (`Notification` has no `[JsonProperty]`/`DataMember(Name=...)` attributes).
- Nested models: `notifications` array of `Notification` — same naming policy applies recursively.
- Dictionary keys: none.
- Baseline evidence: `Scripts/notifications.js` reads `data.success`, `data.notifications`, `data.count`, and per-item `notification.Operation`, `notification.CreatedAt`, `notification.EntityType`, `notification.Message`, `notification.CreatedBy` — confirms PascalCase expected for model properties, lowercase for wrapper keys, matching the JavaScriptSerializer source behavior described above.

**Decision**: Adopt System.Text.Json (Option A) with `PropertyNamingPolicy = null` and `DictionaryKeyPolicy = null` in `AddJsonOptions`, preserving declared CLR names exactly — matches the recorded source policy with no JS changes required.

## Baseline Capture

**Pipeline inventory**: No `IHttpModule`/`IHttpHandler` registrations found anywhere in the project. `Global.asax.cs` has only `Application_Start` (calls `AreaRegistration.RegisterAllAreas()` — no areas actually defined; `FilterConfig.RegisterGlobalFilters` — registers only `HandleErrorAttribute`; `RouteConfig.RegisterRoutes` — single default convention route; `BundleConfig.RegisterBundles` — 5 script/style bundles; and `InitializeDatabase()` — EF Core seed call via `DbInitializer.Initialize`). No `Application_End`/`Application_Error`/`Session_*` handlers.

**Feature inventory** (signals checked against the satellite loading table):
- `UsesGlobalAsax` → yes (trivial, handled inline below, `migrating-global-asax` mapping applied).
- `UsesHttpModules`/`UsesHttpHandlers` → no.
- `UsesHttpContextCurrent`/`UsesHttpServerUtility` → `Server.MapPath(...)` used in `CoursesController` only (file upload path resolution) → replace with `IWebHostEnvironment.WebRootPath`/`ContentRootPath`, no `HttpContext.Current` static access found anywhere.
- `UsesFormsAuth`/`UsesMembership`/`UsesWindowsAuth`/`UsesOwinAuth` → no authentication/authorization in this app at all (`FilterConfig` explicitly comments out the global `AuthorizeAttribute`). No auth migration satellite needed.
- `UsesOwin`/`UsesKatana` → no.
- `UsesSession`/`UsesTempData`/`UsesApplicationState` → `TempData["ErrorMessage"]` used once in `StudentsController.DeleteConfirmed` (small string) — default `CookieTempDataProvider` is sufficient, no `SessionStateTempDataProvider` needed.
- `UsesCustomDependencyResolver`/`UsesAutofac`/`UsesUnity`/`UsesNinject`/`UsesCastleWindsor` → no. Only manual `new` construction in `BaseController` (`db = SchoolContextFactory.Create()`, `notificationService = new NotificationService()`) — converting to constructor-injected built-in DI.
- `UsesAttributeRouting`/`UsesRouteConstraints`/`UsesAreaRouting` → no. Single default convention route only (`{controller}/{action}/{id}`); no `[Route]` attributes anywhere; `AreaRegistration.RegisterAllAreas()` is a no-op (no Areas folder exists).
- JSON endpoints → yes, see Wire Compatibility above.
- `UsesWebApiControllers`/`UsesHttpResponseMessage` → no (no `ApiController`/`System.Web.Http` anywhere).
- `UsesMvcControllers`/`UsesChildActions`/`UsesHtmlHelpers` → yes, plain MVC controllers + extensive HtmlHelper usage in views (see Views inventory below). No child actions (`Html.Action`/`RenderAction`/`[ChildActionOnly]` - confirmed absent via repo-wide grep).
- `UsesCustomFilters`/`UsesOutputCache`/`UsesHandleError` → only the stock `HandleErrorAttribute` registered globally — maps to built-in `UseExceptionHandler`/`UseStatusCodePages` middleware, no custom filter class exists.
- `UsesCustomModelBinders`/`UsesFromUri`/`UsesValueProviders` → no custom binders. `[Bind(Include = "...")]` used on several actions — `BindAttribute.Include` still exists unchanged in `Microsoft.AspNetCore.Mvc`.
- `UsesMvcViews`/`UsesBundling`/`UsesHtmlHelpers` → yes, see Views inventory.

**Gate**: Baseline complete. No side-by-side/YARP concerns (in-place mode), no shared-database-schema concerns (single host, no second live Framework host after this task completes), no background-job satellite needed (no `QueueBackgroundWorkItem`/timers found).

## Probe-Verified Compatibility Finding (important — changes scope of Views work)

**Verified by compiling a throwaway net10.0 `Microsoft.NET.Sdk.Web` project**: the classic MVC 5 `HtmlHelper` extension method surface — `Html.ActionLink(...)`, `Html.BeginForm(...)`, `Html.DropDownList(...)` (weakly-typed, non-`For` overload), `Html.TextBox(...)`, `Html.ValidationSummary(...)` — **still exists and compiles unchanged** in ASP.NET Core 10 under `Microsoft.AspNetCore.Mvc.Rendering` (`HtmlHelperLinkExtensions`, `HtmlHelperFormExtensions`, etc.), as long as `_ViewImports.cshtml` includes `@using Microsoft.AspNetCore.Mvc.Rendering`. This matches the razor-views skill's own guidance to "prefer Option A [keep HtmlHelper calls] during migration to minimize view changes."

**Consequence**: the 23 `.cshtml` files in this project do **not** need to be rewritten to TagHelper syntax. The only view-level changes required are:
1. Add `Views/_ViewImports.cshtml` (new file, required for the above + TagHelpers used implicitly by `asp-` nowhere currently, but added for future-proofing per skill Step 1).
2. Replace `@Scripts.Render(...)` / `@Styles.Render(...)` (9 files: `Shared/_Layout.cshtml`, `Courses/Create.cshtml`, `Courses/Edit.cshtml`, `Students/Create.cshtml`, `Students/Edit.cshtml`, `Instructors/Create.cshtml`, `Instructors/Edit.cshtml`, `Departments/Create.cshtml`, `Departments/Edit.cshtml`) with direct `<script>`/`<link>` tags per `migrating-mvc-bundling` — no Core equivalent exists for `System.Web.Optimization`.
3. Delete `Views/Web.config` (ASP.NET Framework Razor-host-factory config, meaningless and unused under Core; `_ViewImports.cshtml`/`_ViewStart.cshtml` replace its role).
4. Move static assets referenced by those tags from `Content/`/`Scripts/`/root `favicon.ico`/`Uploads/` into `wwwroot/` (ASP.NET Core's `UseStaticFiles()` only serves `wwwroot/` by default) and update the `<link>`/`<script>`/`<img>` paths accordingly (views + `_Layout.cshtml` + `Content/notifications.css` reference + `CoursesController`'s `Uploads/TeachingMaterials` path).
5. No TagHelper rewrite of `@Html.ActionLink`, `@Html.LabelFor`, `@Html.EditorFor`, `@Html.ValidationMessageFor`, `@Html.ValidationSummary`, `@Html.HiddenFor`, `@Html.DisplayFor`, `@Html.DisplayNameFor`, `@Html.AntiForgeryToken`, `@Html.BeginForm`, `@Html.DropDownList`, `@Html.TextBox`, `@Html.Raw`, `@Html.ValidationMessage` — all verified to compile as-is.
6. No child-action/ViewComponent conversion needed (none present).
7. No custom view-location precedence/`RazorViewEngine` subclass found — skip Step 2 of the razor-views skill entirely (no custom `ViewEngines.Engines` registration anywhere in the codebase).

## Views Inventory (23 `.cshtml` files, by controller)

| Folder | Files | Notable patterns |
|---|---|---|
| `Courses/` | Create, Edit, Delete, Details, Index | `Html.BeginForm` w/ `enctype=multipart/form-data` (file upload), `Html.DropDownList("DepartmentID", null, ...)` reading `ViewBag.DepartmentID` |
| `Departments/` | Create, Edit, Delete, Details, Index | `Html.DropDownList("InstructorID", null, ...)`, concurrency `RowVersion` hidden field |
| `Instructors/` | Create, Edit, Delete, Details, Index | Strongly-typed view model (`InstructorIndexData`), course-assignment checkboxes |
| `Students/` | Create, Edit, Delete, Details, Index | `Html.TextBox("SearchString", ...)`, `Html.BeginForm(..., FormMethod.Get)` for search, `PaginatedList<Student>` |
| `Home/` | Index, About, Contact | Plain views, no forms |
| `Notifications/` | Index | Plain dashboard view, JS-driven via `notifications.js` |
| `Shared/` | _Layout.cshtml, Error.cshtml | Layout has nav `ActionLink`s, bundle renders |
| root | `_ViewStart.cshtml`, `Web.config` | `_ViewStart` fine as-is; `Web.config` to be deleted |

## Controllers Inventory (6 controllers, 1092 LOC total)

All six (`BaseController`, `CoursesController`, `DepartmentsController`, `HomeController`, `InstructorsController`, `NotificationsController`, `StudentsController`) reference `System.Web.Mvc` and need the namespace swapped to `Microsoft.AspNetCore.Mvc`. Mechanical, consistent patterns across all of them (verified via full-file reads):

| Old API | New API | Files affected |
|---|---|---|
| `using System.Web.Mvc;` | `using Microsoft.AspNetCore.Mvc;` | all 7 files |
| `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` | `BadRequest()` | Courses, Departments, Instructors, Students (4 occurrences in Instructors, 3 each in others) |
| `HttpNotFound()` | `NotFound()` | same files as above |
| `using System.Net;` (only used for `HttpStatusCode.BadRequest` above) | remove after the above swap (verify no other `System.Net` usage remains per file) | Courses, Departments, Instructors, Students |
| `ActionResult`, `[HttpGet]`, `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[Bind(Include = "...")]`, `[ActionName(...)]`, `JsonResult`, `Json(...)` | **unchanged** — all exist identically in `Microsoft.AspNetCore.Mvc` | all |
| `JsonRequestBehavior.AllowGet` (2 call sites in `NotificationsController`) | remove the argument entirely — Core allows GET+JSON by default, no equivalent/opt-in needed | NotificationsController |
| `HttpPostedFileBase teachingMaterialImage` | `IFormFile teachingMaterialImage` (`Microsoft.AspNetCore.Http`) | CoursesController (Create + Edit) |
| `teachingMaterialImage.ContentLength` | `teachingMaterialImage.Length` | CoursesController |
| `teachingMaterialImage.SaveAs(filePath)` | `using var stream = new FileStream(filePath, FileMode.Create); await teachingMaterialImage.CopyToAsync(stream);` — actions become `async Task<ActionResult>` | CoursesController |
| `Server.MapPath("~/Uploads/TeachingMaterials/")` | inject `IWebHostEnvironment` (via constructor or `BaseController`), use `Path.Combine(_env.WebRootPath, "Uploads", "TeachingMaterials")` | CoursesController (Create, Edit, DeleteConfirmed all call `Server.MapPath`) |
| `TryUpdateModel(instructorToUpdate, "", new string[] {...})` | `await TryUpdateModelAsync(instructorToUpdate, "", i => i.LastName, i => i.FirstMidName, i => i.HireDate, i => i.OfficeAssignment)` — action becomes `async Task<ActionResult>` | InstructorsController.Edit(POST) |
| `BaseController` fields: `db = SchoolContextFactory.Create();` (ctor body), `notificationService = new NotificationService();` (field initializer) | constructor-injected `SchoolContext db`, `NotificationService notificationService` via `protected BaseController(SchoolContext db, NotificationService notificationService)` — all 6 derived controllers get an implicit inherited constructor (none currently declare their own constructor, so no cascading changes needed in subclasses) | BaseController (+ removes need for `SchoolContextFactory` static class entirely) |

**Lifetime mapping applied**: `SchoolContext` → `Scoped` (via `AddDbContext`, which registers Scoped by default — matches EF Core recommended lifetime); `NotificationService` → `Scoped` (matches original per-controller-instance/per-request `new` semantics).

## Global.asax / App_Start Inventory

| Old file/member | New location |
|---|---|
| `Global.asax` + `Global.asax.cs` | deleted; replaced by `Program.cs` |
| `Application_Start` → `AreaRegistration.RegisterAllAreas()` | dropped — no areas defined, Core has no area-registration step needed when there are none |
| `Application_Start` → `FilterConfig.RegisterGlobalFilters` (`HandleErrorAttribute` only) | `app.UseExceptionHandler("/Home/Error")` + `app.UseStatusCodePagesWithReExecute("/Home/Error")` in `Program.cs`; `App_Start/FilterConfig.cs` deleted |
| `Application_Start` → `RouteConfig.RegisterRoutes` (default route + `IgnoreRoute("{resource}.axd/{*pathInfo}")`) | `app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")` in `Program.cs`; the `.axd` ignore route has no Core equivalent/meaning (no `.axd` handlers exist under Core) and is dropped; `App_Start/RouteConfig.cs` deleted |
| `Application_Start` → `BundleConfig.RegisterBundles` | dropped — bundle contents inlined as direct `<script>`/`<link>` tags per view (see Views section); `App_Start/BundleConfig.cs` deleted |
| `Application_Start` → `InitializeDatabase()` (reads `ConfigurationManager.ConnectionStrings`, builds ad-hoc `DbContextOptionsBuilder`, calls `DbInitializer.Initialize`) | `Program.cs`: after `var app = builder.Build();`, resolve `SchoolContext` from a scope and call `DbInitializer.Initialize(context)` — reuses the DI-registered `SchoolContext` instead of a second manually-built one |

## Configuration Inventory

| Web.config entry | appsettings.json / Program.cs equivalent |
|---|---|
| `<connectionStrings><add name="DefaultConnection" .../>` | `appsettings.json` → `"ConnectionStrings": { "DefaultConnection": "..." }`, read via `builder.Configuration.GetConnectionString("DefaultConnection")` |
| `<appSettings>` → `webpages:Version`/`webpages:Enabled`/`ClientValidationEnabled`/`UnobtrusiveJavaScriptEnabled` | dropped — all four are ASP.NET Framework Web Pages/jQuery-validation-wireup settings with no Core meaning (Core's client validation is wired via `_ValidationScriptsPartial`/script includes, not config flags) |
| `<appSettings>` → `NotificationQueuePath` | `appsettings.json` → `"MessageQueue": { "QueuePath": "..." }`, bound to `MessageQueueOptions` via `IOptions<T>` (see MSMQ section) |
| `<system.web><compilation>/<httpRuntime>` (debug flag, targetFramework, maxRequestLength, executionTimeout) | dropped — Core has no `system.web`; `maxRequestLength` equivalent is `FormOptions.MultipartBodyLengthLimit`/Kestrel `MaxRequestBodySize` (not currently constrained elsewhere; skip unless a limit is actually needed — file upload is capped at 5MB in controller code already, well under any Core default) |
| `<system.webServer>` (`requestFiltering maxAllowedContentLength`, `validateIntegratedModeConfiguration`) | dropped — IIS-integration-pipeline-only settings, meaningless for Kestrel/out-of-process Core hosting |
| `ConfigurationManager.ConnectionStrings[...]`/`ConfigurationManager.AppSettings[...]` call sites: `Global.asax.cs`, `Data/SchoolContextFactory.cs`, `Services/NotificationService.cs` | replaced by constructor-injected `IConfiguration`/`IOptions<MessageQueueOptions>` — `SchoolContextFactory.cs` is deleted entirely (superseded by `AddDbContext` in `Program.cs`) |

## MSMQ Migration (`migrating-to-msmq-messaging` applied)

- `Services/NotificationService.cs` uses `System.Messaging.MessageQueue`/`Message`/`XmlMessageFormatter`/`MessageQueueException` — none exist on .NET Core.
- Verified replacement package **`MSMQ.Messaging` v1.0.4** exists on nuget.org ("A drop in replacement for System.Messaging for use in .NET Core") — same class names (`MessageQueue`, `Message`, `XmlMessageFormatter`, `MessageQueueException`, `MessageQueueAccessRights`, `MessageQueueErrorCode`, `MessagePriority`), different namespace (`MSMQ.Messaging` instead of `System.Messaging`).
- Plan: add `<PackageReference Include="MSMQ.Messaging" Version="1.0.4" />`, change the one `using System.Messaging;` to `using MSMQ.Messaging;` in `NotificationService.cs`, no other code changes needed (API-compatible).
- Replace `ConfigurationManager.AppSettings["NotificationQueuePath"]` with constructor-injected `IOptions<MessageQueueOptions>` (`MessageQueueOptions.QueuePath`), registered via `builder.Services.Configure<MessageQueueOptions>(builder.Configuration.GetSection("MessageQueue"))`.
- Note (carried forward, non-blocking): MSMQ itself is a Windows-only OS feature. This only affects **runtime** behavior on a non-Windows/non-MSMQ-enabled host (queue operations would throw) — it does not block **compilation**, which is all that's validated in this Linux CI/dev environment. Flagged here for the user/production-ops awareness; no code change can address this (inherent to MSMQ).

## wwwroot / Static Files

ASP.NET Core's `app.UseStaticFiles()` only serves from `wwwroot/` by convention. Move (git mv, preserving history):
- `Content/*` → `wwwroot/Content/*` (or `wwwroot/css/` — keeping `Content/` subfolder name to minimize reference-path churn)
- `Scripts/*` → `wwwroot/Scripts/*` (same reasoning)
- `favicon.ico` → `wwwroot/favicon.ico`
- `Uploads/` → `wwwroot/Uploads/` (also updates `CoursesController`'s upload target directory to `wwwroot/Uploads/TeachingMaterials`)
- Project_Readme.txt / ROLE_SETUP_GUIDE.md stay at root (not web-served assets)

## Package Changes (this task, on top of Task 01's baseline)

- Add: `MSMQ.Messaging` 1.0.4 (MSMQ replacement, see above)
- `AddControllersWithViews()` + `.AddJsonOptions(o => { o.JsonSerializerOptions.PropertyNamingPolicy = null; o.JsonSerializerOptions.DictionaryKeyPolicy = null; })` in `Program.cs` — no new package needed (System.Text.Json is built into the shared framework)

## Execution Checklist

- [ ] Program.cs created (hosting, DI registrations, middleware pipeline, routing, DB init)
- [ ] appsettings.json + appsettings.Development.json created
- [ ] Global.asax / Global.asax.cs / App_Start/*.cs deleted
- [ ] BaseController converted to constructor injection (SchoolContext, NotificationService, IWebHostEnvironment)
- [ ] SchoolContextFactory.cs deleted
- [ ] All 6 controllers: namespace + API swaps per table above
- [ ] NotificationService.cs: MSMQ.Messaging + IOptions config
- [ ] _ViewImports.cshtml created
- [ ] Views/Web.config deleted
- [ ] 9 views + layout: bundling refs replaced with direct tags
- [ ] Static assets moved to wwwroot/
- [ ] csproj: MSMQ.Messaging package added
- [ ] Build passes with 0 errors, 0 warnings
- [ ] progress-details.md written with full change log

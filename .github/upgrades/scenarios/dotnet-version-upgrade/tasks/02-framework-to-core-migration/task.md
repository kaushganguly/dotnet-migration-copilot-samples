# Task 02: Framework-to-Core Migration (System.Web → ASP.NET Core, retarget to net10.0)

Status: ✅ Done (manually tracked — see Tool Incident note in scenario-instructions.md)

## Objective
Retarget ContosoUniversity from net48 (SDK-style, task 01) to net10.0, fully removing
System.Web/ASP.NET MVC 5 and replacing it with ASP.NET Core MVC, following the
`migrating-aspnet-framework-to-core` skill in **In-Place** mode (per the confirmed
Project Approach option).

## Wire Compatibility Gate (migrating-webapi-odata applicability check)
- Endpoints inventoried: `NotificationsController.GetNotifications()` (GET, JsonResult),
  `NotificationsController.MarkAsRead(int id)` (POST, JsonResult).
- No OData package/controller anywhere in the project.
- Only consumer found: `wwwroot/Scripts/notifications.js` (`fetch('/Notifications/GetNotifications', ...)`),
  a same-origin AJAX call from the app's own page, polled every 5s. No external/3rd-party
  consumer, no API documentation, no contract tests, no repository hint requiring wire
  preservation for an external audience.
- All four STOP signals (Atom formatters, custom OData serializer, pinned externally-consumed
  shape, explicit repository hint) evaluated as **absent** — verdict: **PASS / in-scope for
  in-place migration** (not an OData scenario, so no v4 recipe needed).
- Even though not externally consumed, the JSON field casing (`Operation`, `CreatedAt`,
  `EntityType`, `Message`, `CreatedBy`) is hard-coded in `notifications.js` as PascalCase.
  ASP.NET Core's default System.Text.Json serializer would camelCase these and silently
  break the client JS. Mitigation: registered `AddNewtonsoftJson` with the default
  (PascalCase-preserving) `DefaultContractResolver` in `Program.cs` so the wire shape is
  byte-for-byte unchanged from the pre-migration behavior.

## Baseline (pre-migration) capture
- **Host**: `Global.asax`/`Global.asax.cs` — `Application_Start` called
  `AreaRegistration.RegisterAllAreas()`, `FilterConfig.RegisterGlobalFilters` (adds
  `HandleErrorAttribute`), `RouteConfig.RegisterRoutes` (single conventional route:
  `{controller}/{action}/{id}`, default Home/Index, ignores `*.axd`),
  `BundleConfig.RegisterBundles` (jquery/jqueryval/modernizr/bootstrap script bundles +
  CSS bundle), then `InitializeDatabase()` (manually built `SchoolContext` via
  `ConfigurationManager` connection string, called `DbInitializer.Initialize`).
- **Controllers** (6): `BaseController` (abstract, owns `SchoolContext db` +
  `NotificationService notificationService`, both created in its own constructor, disposed in
  `Dispose`), `HomeController`, `StudentsController`, `CoursesController` (file upload via
  `HttpPostedFileBase`), `DepartmentsController` (optimistic concurrency via `RowVersion`),
  `InstructorsController` (uses legacy 3-arg `TryUpdateModel` overload), `NotificationsController`
  (JSON endpoints, see gate above).
- **Data layer**: `SchoolContext` already EF Core 3.1 (not EF6) — TPH inheritance for
  Person/Student/Instructor, composite keys, explicit table mappings. `SchoolContextFactory`
  manually built a `DbContextOptionsBuilder` from `ConfigurationManager` per-request (no DI).
  `DbInitializer.Initialize` — pure EF Core seed code, no System.Web dependency.
- **Services**: `NotificationService` used `System.Messaging.MessageQueue` (MSMQ), reading queue
  path from `ConfigurationManager.AppSettings["NotificationQueuePath"]`. `LoggingService.cs` is a
  pre-existing empty (0-byte) file, already excluded from compilation since task 01.
- **Views**: 24 `.cshtml` files, only classic `Html.*` helpers (`ActionLink`, `DisplayFor`,
  `EditorFor`, `LabelFor`, `ValidationMessageFor`, `BeginForm`, `AntiForgeryToken`,
  `ValidationSummary`, `DropDownList`, `HiddenFor`, `TextBox`) — no Tag Helpers, no child
  actions (`Html.Action`/`RenderAction`), no `@helper`. `_Layout.cshtml` used
  `@Styles.Render`/`@Scripts.Render` bundles; 8 Create/Edit views used
  `@Scripts.Render("~/bundles/jqueryval")` for client validation scripts.
  `Views/Web.config` declared default Razor namespaces (`System.Web.Mvc*`, `ContosoUniversity`)
  — the `ContosoUniversity` entry is required because `PaginatedList<T>` (used unqualified in
  `Views/Students/Index.cshtml`) lives in the root `ContosoUniversity` namespace.
- **Static assets**: `Content/` (Site.css, notifications.css) and `Scripts/` (jquery, bootstrap,
  modernizr, jquery.validate*, notifications.js) — physically present files, referenced via
  System.Web.Optimization bundles; a few bundle entries (bootstrap.css/min.css) referenced
  files that did not actually exist on disk even before migration (dead references).
- **Web.config**: `appSettings` (webpages:Version/Enabled, Client/UnobtrusiveValidation flags,
  `NotificationQueuePath`), `connectionStrings` (`DefaultConnection` → LocalDB), 18
  `<assemblyBinding>` binding redirects (all removed per confirmed "Remove Binding Redirects"
  option — net10.0 doesn't use assembly binding redirects at all).

## Changes made
1. **Project file / SDK**: `TargetFramework` → `net10.0`; `OutputType` (Exe) removed implicitly
   via `Sdk="Microsoft.NET.Sdk.Web"` default; `ImplicitUsings` enabled. Removed every
   System.Web-era package (`Microsoft.AspNet.Mvc/Razor/WebPages`, `Microsoft.Web.Infrastructure`,
   `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` wasn't present but equivalent
   Razor-precompile packages removed), front-end "NuGet-as-CDN" packages
   (`bootstrap`, `jQuery`, `jQuery.Validation`, `Microsoft.jQuery.Unobtrusive.Validation`,
   `Modernizr`, `WebGrease`, `Antlr`) since their files are now plain static assets under
   `wwwroot`, and BCL polyfill packages that ship in the framework on net10.0
   (`System.Buffers`, `System.Memory`, `System.Numerics.Vectors`,
   `System.Threading.Tasks.Extensions`, `System.Runtime.CompilerServices.Unsafe`,
   `System.ComponentModel.Annotations`, `System.Collections.Immutable`,
   `System.Diagnostics.DiagnosticSource`, `NETStandard.Library`,
   `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.HashCode`, `Microsoft.Identity.Client`,
   `Microsoft.Data.SqlClient.SNI.runtime`, `Microsoft.Extensions.*` — now implicit via
   ASP.NET Core shared framework / transitive EF Core refs). Removed the
   `CopySQLClientNativeBinaries` MSBuild target (SNI native copy, obsolete with modern
   `Microsoft.Data.SqlClient`). Removed the `<Reference Include="System.Messaging/...">`
   legacy framework references.
   Upgraded: `Microsoft.Data.SqlClient` 2.1.4 → 7.1.1, `Microsoft.EntityFrameworkCore*`
   3.1.32 → 10.0.13, `Newtonsoft.Json` 13.0.3 → 13.0.4 (all via `get_supported_package_version`).
   Added: `MSMQ.Messaging` 1.0.4 (API-compatible replacement for `System.Messaging`),
   `Microsoft.AspNetCore.Mvc.NewtonsoftJson` 10.0.13 (wire-shape preservation, see gate above).
   Pinned `System.Drawing.Common` and `System.Security.Cryptography.Pkcs` to 10.0.13 to resolve
   NU1902/NU1903/NU1904 vulnerability warnings surfaced by `MSMQ.Messaging`'s own (older)
   transitive dependencies.
2. **Entry point/host**: Replaced `Global.asax`/`Global.asax.cs`/`App_Start/*.cs` with a single
   minimal-hosting `Program.cs`: registers `SchoolContext` via `AddDbContext` (reading
   `ConnectionStrings:DefaultConnection` from configuration), registers `NotificationService`
   as scoped, configures `MessageQueueOptions` from the `MessageQueue` config section,
   registers MVC (`AddControllersWithViews().AddNewtonsoftJson(...)`), replicates the original
   seed-on-startup behavior via a DI scope (`CreateScope()` → `DbInitializer.Initialize`,
   instead of the old manually-`new`'d context), configures `UseExceptionHandler("/Home/Error")`
   for non-Development, `UseStaticFiles`, `UseRouting`, `UseAuthorization`, and the single
   conventional default route (`{controller=Home}/{action=Index}/{id?}` — functionally
   identical to the old `RouteConfig`, `.axd` ignore rule is a no-op on Core and was dropped).
3. **Configuration**: Added `appsettings.json` (+ `appsettings.Development.json`) with
   `ConnectionStrings:DefaultConnection` (same LocalDB connection string as before) and
   `MessageQueue:QueuePath` (same default path as before). Deleted `Web.config`,
   `Web.Debug.config`, `Web.Release.config`, `Views/Web.config` (all System.Web-only config,
   no longer used by ASP.NET Core). All 18 assembly binding redirects removed (net10.0 doesn't
   use them).
4. **DI container**: `SchoolContext` and `NotificationService` are now constructor-injected
   (scoped) instead of manually `new`'d per-controller. Deleted `Data/SchoolContextFactory.cs`
   (superseded by `AddDbContext`). `BaseController` now takes `(SchoolContext, NotificationService)`
   via constructor and no longer disposes them in `Dispose()` (the DI container, not the
   controller, owns their scoped lifetime — disposing them manually would cause
   `ObjectDisposedException` on reused scoped instances).
5. **Controllers**: All 6 controllers (+ `BaseController`) migrated from `System.Web.Mvc` to
   `Microsoft.AspNetCore.Mvc`: `HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`,
   `HttpNotFound()` → `NotFound()`, `[Bind(Include = "...")]` → `[Bind("...")]`,
   `SelectList` → `Microsoft.AspNetCore.Mvc.Rendering.SelectList`, `JsonRequestBehavior.AllowGet`
   removed (GET `Json()` is allowed by default in Core). `CoursesController`: added
   `IWebHostEnvironment` constructor dependency, replaced `HttpPostedFileBase` with `IFormFile`
   (`ContentLength`→`Length`, `SaveAs(path)`→manual `FileStream` + `CopyTo`), replaced
   `Server.MapPath` with a local `MapPath` helper resolving against `ContentRootPath`.
   `InstructorsController.Edit(int?, string[])` POST action: replaced the legacy 3-arg
   `TryUpdateModel(model, prefix, string[] includeProperties)` with the async
   `TryUpdateModelAsync(model, prefix, Func<ModelMetadata,bool> propertyFilter)` overload
   (action is now `async Task<ActionResult>`). `HomeController.Unauthorized()` marked `new`
   to resolve the CS0114 warning against the inherited `ControllerBase.Unauthorized()` (kept
   the original method name/route — no other code references this dead/unused action, but
   renaming would still have been an unnecessary route-shape risk).
6. **Services**: `NotificationService` ported from `System.Messaging` to `MSMQ.Messaging`
   per the `migrating-to-msmq-messaging` skill — same class/member names, different
   namespace; queue path now comes from `IOptions<MessageQueueOptions>` (bound from
   `appsettings.json`'s `MessageQueue:QueuePath`) instead of `ConfigurationManager`.
7. **Views/static assets**: Moved `Content/` and `Scripts/` physically to `wwwroot/Content` and
   `wwwroot/Scripts` (git-tracked renames) so `app.UseStaticFiles()` serves them. `_Layout.cshtml`:
   replaced `@Styles.Render("~/Content/css")` / `@Scripts.Render(...)` bundle calls with direct
   `<link>`/`<script>` tags to the files that actually exist (`Site.css`, `notifications.css`,
   `modernizr-2.6.2.js`, `jquery-3.4.1.js`, `bootstrap.js`, `respond.js`, `notifications.js`);
   the previously dead `bootstrap.css`/`bootstrap.min.css` bundle references (files never
   existed on disk) were not carried forward. Replaced all 8
   `@Scripts.Render("~/bundles/jqueryval")` occurrences (Create/Edit views) with direct
   `<script>` tags for `jquery.validate.js` + `jquery.validate.unobtrusive.js`. Added
   `Views/_ViewImports.cshtml` with `@using ContosoUniversity` (replaces the Razor
   default-namespace declaration that used to live in the deleted `Views/Web.config`, required
   for `PaginatedList<T>` to resolve unqualified in `Students/Index.cshtml`) plus
   `@using ContosoUniversity.Models[.SchoolViewModels]` and the standard Tag Helpers import.
   Rewrote `Views/Shared/Error.cshtml` (dropped the `System.Web.Mvc.HandleErrorInfo` model and
   `HttpContext.Current.IsDebuggingEnabled` check — `HomeController.Error()` passes no model,
   matching ASP.NET Core's error-handling convention; the simplified view still renders the
   same user-facing heading/sub-heading as before).

## Verification
- `dotnet build` (Debug) and `dotnet build -c Release`: **0 Warnings, 0 Errors**.
- `grep` sweep for `System.Web`, `HttpContext.Current`, `System.Messaging` across all `.cs`/
  `.cshtml`/`.csproj` files: only 2 matches, both in code comments documenting the prior
  behavior being replaced (not live references).
- Manual smoke test (`dotnet run` against a temporary copy with DB-init disabled): confirmed
  HTTP host starts, routing/DI/Razor views pipeline resolves correctly. With DB-init enabled,
  startup fails with `PlatformNotSupportedException: LocalDB is not supported on this
  platform` — this sandbox has no Windows/LocalDB, which is an environment limitation that
  would have equally prevented the **original** System.Web app from running here (LocalDB and
  IIS/System.Web both require Windows); it is not a migration regression. Runtime requests to
  `/Notifications/GetNotifications` fail with `MSMQ not installed` for the same reason — MSMQ
  is Windows-only in both the old and new code paths.
- No test projects exist in the repository (`discover_test_projects` returned `[]`), so
  `passUnitTests=true` is vacuously satisfied; this is carried into Task 03 for final sign-off.

## Done when
- [x] Project retargeted to net10.0, builds with 0 errors/warnings (Debug + Release).
- [x] No System.Web/System.Messaging references remain in source.
- [x] Wire-contract gate evaluated and recorded for the only JSON endpoints in the app.
- [x] MSMQ migrated per `migrating-to-msmq-messaging` skill.
- [x] Static assets served from `wwwroot`; Razor views build and reference only existing files.
- [x] Binding redirects removed; configuration migrated to `appsettings.json`/`IConfiguration`.

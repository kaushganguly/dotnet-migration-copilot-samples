# Task 02 — Upgrade ContosoUniversity to net10.0 and migrate ASP.NET MVC to ASP.NET Core MVC

## Research (baseline capture, done prior to execution)

### Entry point / hosting
- `Global.asax.cs`: `Application_Start` calls `AreaRegistration.RegisterAllAreas()`,
  `FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters)`,
  `RouteConfig.RegisterRoutes(RouteTable.Routes)`, `BundleConfig.RegisterBundles(BundleTable.Bundles)`,
  then `InitializeDatabase()` which `new SchoolContext()`s and calls `DbInitializer.Initialize(context)`.
- No areas actually registered (no `Areas/` folder).
- Target: `Program.cs` minimal hosting model; `app.MapControllerRoute` for the one default route;
  seed DB via a scoped `IServiceProvider` at startup instead of a static method.

### Routing
- `App_Start/RouteConfig.cs`: `routes.IgnoreRoute("{resource}.axd/{*pathInfo}")` (no-op under Kestrel,
  drop) + one default route `{controller}/{action}/{id}` (controller=Home, action=Index, id=optional).
- No attribute routing in controllers currently.

### Filters
- `App_Start/FilterConfig.cs`: registers only `new HandleErrorAttribute()`. Auth filter is commented
  out in source — confirms no real authentication/authorization is implemented despite README claims.
  → Replace with ASP.NET Core exception-handling middleware (`UseExceptionHandler`/`UseStatusCodePages`
  in non-dev, `UseDeveloperExceptionPage` in dev). No auth middleware needed (none existed).

### Bundling (`App_Start/BundleConfig.cs`, package `Microsoft.AspNet.Web.Optimization`)
- Bundles: `~/bundles/jquery`, `~/bundles/jqueryval`, `~/bundles/modernizr`, `~/bundles/bootstrap`,
  `~/Content/css`. Referenced via `@Scripts.Render(...)`/`@Styles.Render(...)` in `_Layout.cshtml`
  and a few Create/Edit views.
- Per confirmed "Unsupported Packages = Resolve Inline" option: drop the package, move
  `Scripts`/`Content` physical files to `wwwroot/`, replace `@Scripts.Render`/`@Styles.Render` calls
  with direct `<script src="...">`/`<link rel="stylesheet" href="...">` tags, add
  `app.UseStaticFiles()`.

### Controllers (7: Base, Courses, Departments, Home, Instructors, Notifications, Students)
- All plain CRUD; namespace `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`.
- `CoursesController`: 2 usages of `HttpPostedFileBase` for file upload → `IFormFile`.
- `NotificationsController`: uses `JsonRequestBehavior.AllowGet` on `Json(...)` → ASP.NET Core
  `Json(...)` has no such parameter (GET JSON is allowed by default); just drop the argument.
- `BaseController`: no special base behavior beyond being a common base class — verify during
  migration, update namespace only unless something more is found.
- No `HttpContext.Current`, no `Session`, no custom model binders.

### Data layer
- `Data/SchoolContext.cs` — EF Core 3.1.32 DbContext, TPH inheritance (Person → Student/Instructor),
  composite keys, `datetime2` columns. No EF6/legacy APIs — should upgrade to EF Core 10 with
  minimal/no code changes (verify `OnModelCreating` fluent API calls against EF Core 10 breaking changes).
- `Data/SchoolContextFactory.cs` (design-time factory) — uses
  `ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString`. Must switch to
  reading from `appsettings.json` via a minimal `IConfigurationBuilder` (design-time factories can't
  use DI) — standard EF Core pattern: `new ConfigurationBuilder().AddJsonFile("appsettings.json")...`.
- Runtime DbContext registration: new — register `AddDbContext<SchoolContext>` in `Program.cs` using
  `IConfiguration.GetConnectionString("DefaultConnection")`.

### Configuration (`Web.config`)
- `appSettings` (5 keys) incl. `NotificationQueuePath` → move to `appsettings.json` root/`Notifications`
  section, bind via `IOptions<T>` where used (`NotificationService`), else `IConfiguration["Key"]`.
- `connectionStrings` (1: `DefaultConnection`) → `appsettings.json` `ConnectionStrings` section.
- `<runtime><assemblyBinding>` (~16 dependentAssembly redirects) → per "Document and Review" option:
  confirm not needed under net10.0 (modern SDK resolves versions directly, no binding redirects
  required), then remove `Web.config` entirely (fully superseded by `appsettings.json` + SDK project
  file). Document this removal explicitly.
- `Web.Debug.config`/`Web.Release.config` (XML transforms) → no longer applicable, remove.

### Messaging (`Services/NotificationService.cs`)
- Uses `System.Messaging` (Windows MSMQ, `MessageQueue`, `Message`). Per loaded
  `migrating-to-msmq-messaging` skill: swap to `MSMQ.Messaging` NuGet package (API-compatible
  namespace swap: `using MSMQ.Messaging;` in place of `using System.Messaging;`), move
  `NotificationQueuePath` from Web.config appSettings into `appsettings.json` bound via
  `IOptions<NotificationOptions>`, register in DI.
- NOTE: MSMQ itself is a Windows-only transport. Since this app is moving to net10.0 (cross-platform
  capable in principle), MSMQ remains Windows-only infra — out of scope to replace the transport
  itself (not requested); only the namespace/package + config binding migrates.

### Packages (per assessment + `get_supported_package_version` lookups to run during execution)
- Remove entirely: `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`,
  `Microsoft.AspNet.Web.Optimization`, `Microsoft.Web.Infrastructure`, `NETStandard.Library`,
  `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` (already gone), `WebGrease`,
  `Microsoft.jQuery.Unobtrusive.Validation` (replace via static libs in wwwroot/lib or drop if unused),
  `System.ComponentModel.Annotations` (built into net10.0), old transitive-only BCL shims
  (`System.Buffers`, `System.Collections.Immutable`, `System.Memory`, `System.Numerics.Vectors`,
  `System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`,
  `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.HashCode`, `Microsoft.Extensions.*` base packages —
  all either in-box for net10.0 or superseded by the framework-provided versions).
- `Antlr` (3.4.1.9004, incompatible per assessment): check actual usage — if (as expected) it's an
  indirect, now-unused transitive artifact of old MVC tooling, drop entirely.
- Upgrade: `Microsoft.EntityFrameworkCore*` family → 10.0.14 (or latest resolvable via
  `get_supported_package_version`), `Microsoft.Data.SqlClient` → latest (resolves the NU1903 vuln),
  `Newtonsoft.Json` → latest 13.x (if still needed — ASP.NET Core defaults to System.Text.Json;
  keep only if code depends on Newtonsoft-specific behavior), `Microsoft.Identity.Client` → evaluate
  actual usage (assessment flagged as deprecated) — remove if unused.
- `System.Messaging` → `MSMQ.Messaging` (see above).
- Remove `<Reference Include="System.Web" />` and other System.Web-only framework references added
  in task 01 — no longer needed once ASP.NET Core takes over.

### Views
- Standard Razor views, `@Scripts.Render`/`@Styles.Render` (→ static tags, see Bundling above),
  `@Html.ActionLink` (ASP.NET Core still supports this via `Microsoft.AspNetCore.Mvc.Rendering` —
  verify, likely no change needed), no child actions (`Html.Action`/`Html.RenderAction`) found.
- Need new `_ViewImports.cshtml` (`@using` + `@addTagHelper`) and verify `_ViewStart.cshtml` still works.
- `web.config` inside `Views/` (the MVC view-engine marker file) can be removed — not needed under
  ASP.NET Core's Razor view engine.

### Nullable reference types
- Per confirmed option: enable `<Nullable>enable</Nullable>` and fix resulting warnings. Codebase is
  small (7 controllers, handful of models/services) — expect a manageable number of warnings.

## Done when
- `<TargetFramework>` is `net10.0`, `Sdk="Microsoft.NET.Sdk.Web"` retained, `OutputType` back to `Exe`.
- `Program.cs` replaces `Global.asax`/`Global.asax.cs`; app starts and serves the default route.
- All 7 controllers compile against `Microsoft.AspNetCore.Mvc`; no `System.Web.*` references remain.
- Web.config fully replaced by `appsettings.json` (+ `appsettings.Development.json` if useful);
  binding-redirect review documented; legacy config files removed.
- NotificationService uses `MSMQ.Messaging` + `IOptions`-bound configuration.
- EF Core upgraded to the assessment-recommended version; `SchoolContextFactory` reads from
  `appsettings.json`; DbContext registered via DI in `Program.cs`.
- Static assets served from `wwwroot/`; bundling package removed; views updated accordingly.
- `<Nullable>enable</Nullable>` set; no nullable warnings remaining.
- `dotnet build` succeeds with 0 errors and 0 warnings (no suppressions without approval).
- No automated tests exist in this solution (confirmed during task 01 baseline capture), so
  `passUnitTests=true` is trivially satisfied once build is clean — document this explicitly rather
  than silently skipping test execution.
- No remaining references to removed/incompatible packages (`Microsoft.AspNet.*`, `Antlr`,
  `WebGrease`, etc.) — verify via `dotnet list package` / grep.
- `Microsoft.Data.SqlClient` vulnerable version (2.1.4) no longer present — security compliance check.

## Status: In progress

# 001-upgrade-dotnet-to-net10: 001-upgrade-dotnet-to-net10

Execute task 001-upgrade-dotnet-to-net10.

## Research Notes (pre-execution)

### Environment constraint
Linux sandbox has no Visual Studio / `Microsoft.WebApplication.targets`. A stub targets file was
placed under the installed SDK's `MSBuild\...\WebApplications\` path solely to let MCP tools
(`get_dotnet_upgrade_options`, assessment) evaluate the legacy project. It becomes irrelevant the
moment the project is converted to SDK-style (first step below), since the import is removed
entirely.

### Affected files (confirmed by direct inspection)
- `ContosoUniversity.csproj` — legacy format, `packages.config`, Web Application project type.
- `packages.config` — 43 packages, many framework-only (MVC5/Razor/WebPages/Optimization/
  WebGrease/Antlr/CodeDom/Web.Infrastructure/jQuery.Unobtrusive.Validation/NETStandard.Library),
  rest are EF Core 3.1.32 / Extensions 3.1.32 / SqlClient 2.1.4 / Newtonsoft.Json 13.0.3 /
  Identity.Client — all need version bumps or removal.
- `Global.asax` / `Global.asax.cs` — `Application_Start` wires `AreaRegistration`,
  `FilterConfig`, `RouteConfig`, `BundleConfig`, then calls `InitializeDatabase()` (EF Core
  `EnsureCreated` + `DbInitializer.Initialize`) — becomes `Program.cs` top-level statements.
- `App_Start/RouteConfig.cs` — one default route + ignore route for `.axd` → `MapControllerRoute`
  in `Program.cs`; ignore route not needed (no `.axd` handlers in Core).
- `App_Start/FilterConfig.cs` — registers only `HandleErrorAttribute` (commented-out global
  `AuthorizeAttribute`) → drop; use `UseExceptionHandler`/`UseStatusCodePages` instead.
- `App_Start/BundleConfig.cs` — bundles jquery/jqueryval/modernizr/bootstrap/css → no Core
  built-in equivalent; replaced by direct static references to files moved into `wwwroot/`.
- `Controllers/BaseController.cs` — constructs `SchoolContextFactory.Create()` +
  `new NotificationService()` → constructor DI (`SchoolContext db, NotificationService
  notificationService`), each of 6 derived controllers (`Home`, `Students`, `Courses`,
  `Instructors`, `Departments`, `Notifications`) needs an explicit constructor forwarding to
  `base(db, notificationService)` since the base no longer has a parameterless ctor.
- `Controllers/StudentsController.cs`, `CoursesController.cs`, `InstructorsController.cs`,
  `DepartmentsController.cs`, `HomeController.cs`, `NotificationsController.cs` — namespace
  `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`; `HttpNotFound()` → `NotFound()`;
  `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`; `[Bind(Include=
  "...")]` → `[Bind("...")]`; `Json(x, JsonRequestBehavior.AllowGet)` → `Json(x)`;
  `SelectList`/`HtmlHelper` APIs unchanged (available in `Microsoft.AspNetCore.Mvc.Rendering`).
  `CoursesController` additionally: `HttpPostedFileBase` → `IFormFile`, `Server.MapPath(...)` →
  `Path.Combine(_env.WebRootPath, ...)` via injected `IWebHostEnvironment`.
  `InstructorsController.Edit(POST)` additionally: `TryUpdateModel(model, prefix, string[])` →
  `await TryUpdateModelAsync(model, prefix, expr1, expr2, ...)` (method becomes async).
- `Data/SchoolContext.cs` — no changes needed (already EF Core, no System.Web/Framework-only
  APIs). `Data/SchoolContextFactory.cs` — deleted; replaced by `AddDbContext` registration in
  `Program.cs` reading `IConfiguration.GetConnectionString("DefaultConnection")`.
  `Data/DbInitializer.cs` — no changes, invoked once at startup from `Program.cs`.
- `Services/NotificationService.cs` — `using System.Messaging` → `using MSMQ.Messaging`
  (NuGet package `MSMQ.Messaging`); `ConfigurationManager.AppSettings["NotificationQueuePath"]`
  → injected `IOptions<NotificationQueueOptions>` (new options class) bound from
  `appsettings.json` `"NotificationQueue:QueuePath"`.
- `Services/LoggingService.cs` — empty file, no changes.
- `Models/*.cs`, `Models/SchoolViewModels/*.cs` — pure POCOs with DataAnnotations, no changes.
- `PaginatedList.cs` — no changes (plain C#, no System.Web).
- `Views/**/*.cshtml` (22 views) — all use only `HtmlHelper` methods available unchanged in
  ASP.NET Core (`Html.BeginForm`, `Html.EditorFor`, `Html.LabelFor`, `Html.ValidationSummary`,
  `Html.ValidationMessageFor`, `Html.ActionLink`, `Html.DropDownList`, `Html.HiddenFor`,
  `Html.Raw`). Every view with a `@section Scripts { @Scripts.Render(...) }` block needs that
  replaced with nothing (jqueryval bundle folded into the global `_Layout.cshtml` script refs,
  since ASP.NET Core doesn't bundle) or direct `<script>` tags. `_Layout.cshtml` uses
  `@Styles.Render`/`@Scripts.Render` → direct `<link>`/`<script>` tags against `wwwroot/`.
  `Views/Web.config` is a Razor-view-engine marker file specific to System.Web — deleted (no
  Core equivalent/needed); add `_ViewImports.cshtml` instead (tag helpers + model namespaces).
- `Content/`, `Scripts/`, `favicon.ico`, `Uploads/` → moved under `wwwroot/`.
- `Web.config` → `appsettings.json` (`ConnectionStrings:DefaultConnection`,
  `NotificationQueue:QueuePath`); `Web.Debug.config`/`Web.Release.config` (config transforms,
  not inspected in depth — low risk, superseded by `appsettings.{Environment}.json`) → deleted.
- `Properties/AssemblyInfo.cs` — SDK-style projects auto-generate assembly info; delete to avoid
  duplicate-attribute build errors (`CS0579`) unless it contains custom attributes worth keeping
  (checked: only generates standard `Assembly*` attributes already synthesized by the SDK).

### Package plan
Keep & upgrade to latest stable compatible with `net10.0`: `Microsoft.EntityFrameworkCore`,
`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`,
`Microsoft.Data.SqlClient`, `Newtonsoft.Json`.
Add: `MSMQ.Messaging` (replaces `System.Messaging` reference).
Drop entirely (covered by the ASP.NET Core shared framework or no longer needed):
`Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`,
`Microsoft.AspNet.Web.Optimization`, `WebGrease`, `Antlr`, `Microsoft.Web.Infrastructure`,
`Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.jQuery.Unobtrusive.Validation`,
`Microsoft.Identity.Client` (unused directly), `NETStandard.Library`, all `Microsoft.Extensions.*`
3.1.32 (superseded by shared framework / transitive from EF Core), `Microsoft.Bcl.*`,
`System.Buffers`/`System.Memory`/`System.Collections.Immutable`/`System.Runtime.CompilerServices.
Unsafe`/`System.Threading.Tasks.Extensions`/`System.Numerics.Vectors`/`System.ComponentModel.
Annotations`/`System.Diagnostics.DiagnosticSource` (all in-box in net10.0), `bootstrap`/`jQuery`/
`jQuery.Validation`/`Modernizr` (client libs — keep files on disk under `wwwroot`, just not
NuGet-managed since this app does not use LibMan/npm).

### Build/validation plan
`dotnet build` is correct (SDK-style net10.0 web project, no WPF/WinForms/resx-with-images/
multi-targeting) — no `msbuild.exe` needed. Run `dotnet restore` + `dotnet build` from the
project directory after each major edit pass; iterate on compiler errors.

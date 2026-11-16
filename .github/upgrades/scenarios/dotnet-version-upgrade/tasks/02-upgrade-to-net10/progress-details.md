# Progress — Task 02: Upgrade ContosoUniversity to net10.0 and migrate to ASP.NET Core hosting

## MCP tooling note
`AppModUpgrade-start_task('02-upgrade-to-net10')` continued to fail after task 01
("Task '02-upgrade-to-net10' not found in plan.md or tasks.md. Available top-level tasks: ")
even though `plan.md`/`tasks.md` contain the task in the documented canonical format and
`get_state` lists the scenario, assessment, plan, and tasks artifacts as present. This is the
same persistent backend parsing issue recorded for task 01. Work was executed directly and
tracked manually here, per the workaround already agreed for task 01.

## Wire Compatibility Gate
See `task.md` → `## Wire Compatibility` for the full `migrating-webapi-odata` gate record.
**Verdict: NOT APPLICABLE** (no OData/Atom, no externally-consumed contract — `NotificationsController`
JSON endpoints are consumed only by this app's own `Scripts/notifications.js`). Serialization naming
preserved via `AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null)` so the
existing PascalCase-reading JS (`notification.Operation`, `.CreatedAt`, `.EntityType`, `.Message`,
`.CreatedBy`) needed no changes.

## Changes made

### Project file / SDK
- `ContosoUniversity.csproj`: `TargetFramework` → `net10.0`; `Nullable`/`ImplicitUsings` disabled
  (explicit, matches "Leave Disabled" plan decision); kept `GenerateAssemblyInfo=false` (project
  still uses `Properties/AssemblyInfo.cs`).
- Removed all System.Web-era / bundling / polyfill packages: `Microsoft.AspNet.Mvc`, `.Razor`,
  `.WebPages`, `.Web.Optimization`; `Microsoft.Web.Infrastructure`; `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`
  *(was never referenced — not present)*; `NETStandard.Library`; `System.Buffers`;
  `System.ComponentModel.Annotations`; `System.Memory`; `System.Numerics.Vectors`;
  `System.Threading.Tasks.Extensions`; `System.Collections.Immutable`; `System.Diagnostics.DiagnosticSource`;
  `System.Runtime.CompilerServices.Unsafe` — all superseded by the net10.0 framework reference.
- Removed `Antlr`, `WebGrease` (confirmed zero code references; were internal to the removed
  `System.Web.Optimization` minification pipeline) and `Microsoft.Identity.Client` (confirmed zero
  code references anywhere in the solution — unused leftover, not part of the active auth-free app).
- Removed `bootstrap`, `jQuery`, `jQuery.Validation`, `Microsoft.jQuery.Unobtrusive.Validation`,
  `Modernizr` NuGet package references — these only delivered static JS/CSS content files; the
  physical files already exist under `Scripts/`/`Content/` (checked into source) and are now served
  as static assets directly (see Program.cs below), so the NuGet packages added no runtime value.
- Removed `Microsoft.Data.SqlClient.SNI.runtime` (obsolete — modern `Microsoft.Data.SqlClient`
  resolves native SNI assets automatically via NuGet runtime assets) and `Microsoft.Bcl.AsyncInterfaces`
  / `Microsoft.Bcl.HashCode` (BCL types now built into net10.0).
- Upgraded: `Microsoft.EntityFrameworkCore(.Abstractions/.Relational/.SqlServer/.Tools)` 3.1.32 → 10.0.15;
  `Microsoft.Data.SqlClient` 2.1.4 → 7.1.2 (resolves the NU1903 vulnerability deferred from task 01);
  `Newtonsoft.Json` 13.0.3 → 13.0.4 (still used for MSMQ message payload (de)serialization).
- Added `MSMQ.Messaging` 1.0.4 (drop-in `System.Messaging` replacement for .NET Core/.NET, same
  type/namespace shape per the `migrating-to-msmq-messaging` skill).
- Added explicit pins `System.Drawing.Common` 11.0.0 and `System.DirectoryServices` 11.0.0 to
  override a transitive critical vulnerability (GHSA-rxg9-xrhp-64gj, `System.Drawing.Common` 4.7.0)
  pulled in by `MSMQ.Messaging` → `System.DirectoryServices` 4.7.0. Confirmed via
  `dotnet list package --vulnerable --include-transitive`: **no vulnerable packages remain**.
- `<Reference>` GAC items (System.Web.*, System.EnterpriseServices, System.Messaging, etc.) all
  removed — none are referenceable or needed under `Microsoft.NET.Sdk.Web` on net10.0.

### Entry point / hosting
- Added `Program.cs` (minimal hosting): `AddControllersWithViews()` with the JSON naming-policy
  override described above; `AddDbContext<SchoolContext>` wired to `IConfiguration`
  (`ConnectionStrings:DefaultConnection`); `Configure<MessageQueueOptions>` +
  `AddScoped<NotificationService>()` for DI; `UseExceptionHandler("/Home/Error")` (ports
  `FilterConfig`'s `HandleErrorAttribute`) + `UseHsts()` outside Development; `UseStaticFiles()` for
  `wwwroot`, plus three additional `UseStaticFiles` registrations mapping `Content/`, `Scripts/`,
  `Uploads/` from `ContentRootPath` at their original `/Content`, `/Scripts`, `/Uploads` request
  paths (no files moved — all existing `~/Content/...`/`~/Scripts/...`/`~/Uploads/...` URLs in views
  and controllers keep working unchanged); `UseRouting()` + `MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")`
  (ports `RouteConfig`'s single default route — no attribute routing existed); a startup DI scope
  that calls `DbInitializer.Initialize(context)` (ports `Global.asax.cs`'s `InitializeDatabase()`).
- Removed `Global.asax`, `Global.asax.cs`, `App_Start/RouteConfig.cs`, `App_Start/FilterConfig.cs`,
  `App_Start/BundleConfig.cs` (all content ported into `Program.cs` above).
- Removed `Web.config`, `Web.Debug.config`, `Web.Release.config`, `Views/Web.config` (System.Web
  Razor host configuration — obsolete under ASP.NET Core). Added `appsettings.json` /
  `appsettings.Development.json` with the `DefaultConnection` connection string (unchanged value,
  `TrustServerCertificate=True` added since Microsoft.Data.SqlClient 7.x defaults to stricter TLS
  validation) and `MessageQueue:QueuePath` (same default path as the old `NotificationQueuePath`
  app setting). The 22 binding-redirect `<dependentAssembly>` entries were reviewed (per the
  "Document and Review" plan decision) and dropped — .NET Core does not use assembly binding
  redirects at all, so there is nothing to carry forward.

### DI / data access
- Removed `Data/SchoolContextFactory.cs` (static `ConfigurationManager`-based factory) — replaced
  by proper DI: `SchoolContext` is registered via `AddDbContext` and constructor-injected.
- `Controllers/BaseController.cs` changed from field-initializer construction
  (`db = SchoolContextFactory.Create()`, `notificationService = new NotificationService()`) to
  constructor injection of `SchoolContext` and `NotificationService` (both DI-managed, scoped
  lifetime); removed the manual `Dispose(bool)` override (the container disposes scoped services).
- `Services/NotificationService.cs`: `System.Messaging` → `MSMQ.Messaging` (namespace-only swap,
  same API); `ConfigurationManager.AppSettings["NotificationQueuePath"]` → constructor-injected
  `IOptions<MessageQueueOptions>` (new `Services/MessageQueueOptions.cs`); now implements
  `IDisposable` explicitly.

### Controllers
All 6 derived controllers (`CoursesController`, `DepartmentsController`, `HomeController`,
`InstructorsController`, `StudentsController`, `NotificationsController`) gained explicit
constructors calling `base(context, notificationService)` (required once `BaseController` no
longer has a parameterless constructor) and switched `using System.Web.Mvc;` →
`using Microsoft.AspNetCore.Mvc;` (+ `Microsoft.AspNetCore.Mvc.Rendering` for `SelectList`).
Mechanical API swaps applied solution-wide:
- `HttpNotFound()` → `NotFound()`
- `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`
- `[Bind(Include = "...")]` → `[Bind("...")]` (ASP.NET Core's `BindAttribute` takes `include` as
  constructor args, no `Include` named property)
- `JsonRequestBehavior.AllowGet` → removed (ASP.NET Core's `Json()` always allows GET)
- `CoursesController`: `HttpPostedFileBase` → `IFormFile`; `.ContentLength` → `.Length`;
  `.SaveAs(path)` → `using (var stream = File.Create(path)) { file.CopyTo(stream); }`;
  `Server.MapPath(...)` → a small `MapPath` helper using injected `IWebHostEnvironment.ContentRootPath`
  (preserves the original "~/Uploads/TeachingMaterials/..." virtual-path convention).
- `InstructorsController.Edit(int?, string[])`: classic `TryUpdateModel(model, prefix, string[] includeProperties)`
  has no direct ASP.NET Core equivalent; converted to `async Task<ActionResult>` using
  `await TryUpdateModelAsync<Instructor>(instructorToUpdate, "", i => i.LastName, i => i.FirstMidName, i => i.HireDate, i => i.OfficeAssignment)`
  (same explicit allow-list semantics, expression-based instead of string-based).
- `HomeController.Unauthorized()` now declared `public new ActionResult Unauthorized()` to
  explicitly shadow `ControllerBase.Unauthorized()` (a new member in ASP.NET Core's base class that
  didn't exist in MVC 5) — avoids a hiding warning while preserving the existing custom view-based
  behavior and all existing callers/links to `Home/Unauthorized`.

### Views
- Added `Views/_ViewImports.cshtml` (`@using` for `ContosoUniversity`, `.Models`,
  `.Models.SchoolViewModels`; `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`).
- `Views/Shared/_Layout.cshtml` and the 8 Create/Edit views using `@Scripts.Render(...)`/
  `@Styles.Render(...)`: replaced with static `<link>`/`<script>` tags pointing at the same
  physical files (`bootstrap.css`, `Site.css`, `notifications.css`, `modernizr-2.6.2.js`,
  `jquery-3.4.1.js`, `bootstrap.js`, `respond.js`, `notifications.js`,
  `jquery.validate.min.js` + `jquery.validate.unobtrusive.min.js`) — matches the "Resolve Inline"
  plan decision; no bundling/minification package needed.
- `Views/Shared/Error.cshtml`: dropped the `@model System.Web.Mvc.HandleErrorInfo` model and the
  `HttpContext.Current.IsDebuggingEnabled` conditional detail block (no ASP.NET Core equivalent
  object is passed by `HomeController.Error()`, which has always just called `return View()` with
  no model) — now a plain static error page, still rendered via the `UseExceptionHandler("/Home/Error")`
  pipeline added in `Program.cs`.
- All other `@Html.*` helpers used across the 24 view files (`ActionLink`, `DisplayFor`,
  `DisplayNameFor`, `LabelFor`, `ValidationMessageFor`, `EditorFor`, `AntiForgeryToken`, `Raw`,
  `ValidationSummary`, `HiddenFor`, `DropDownList`, `ValidationMessage`, `TextBox`,
  `Html.BeginForm`) required **no changes** — ASP.NET Core's `IHtmlHelper`/`IHtmlHelper<T>` still
  ship equivalent extension methods with matching signatures (confirmed by a clean, 0-error,
  0-warning build; initially assumed `ActionLink` needed a compatibility shim, but
  `Microsoft.AspNetCore.Mvc.Rendering.HtmlHelperLinkExtensions.ActionLink` already exists with
  matching overloads — the shim was written, found to collide/ambiguous-overload with the built-in
  method, and removed).

## Validation

- `dotnet restore`: clean, no warnings.
- `dotnet build` (project and `ContosoUniversity.sln`), including `dotnet build -warnaserror`:
  **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `dotnet list package --vulnerable --include-transitive`: **no vulnerable packages**.
- `grep` sweep for `System.Web.Mvc`, `System.Web.Optimization`, `HttpContext.Current`,
  `System.Messaging` across all `.cs`/`.cshtml` files (excluding `obj`/`bin`): **zero matches**.
- `dotnet run` smoke test: the ASP.NET Core host starts, builds the DI container, and reaches
  `DbInitializer.Initialize()` before failing with
  `System.PlatformNotSupportedException: LocalDB is not supported on this platform` — this is an
  **environment limitation** (no SQL Server/LocalDB available in this Linux sandbox), not a
  migration defect; the original MVC 5 app had the exact same unconditional
  `InitializeDatabase()` call in `Application_Start()` and would have failed identically on this
  host. Full HTTP request/response verification against a real SQL Server instance is out of scope
  for this environment.

## Success criteria status
- `passBuild=true` — ✅ confirmed (0 errors, 0 warnings, solution + project, including
  `-warnaserror`).
- `generateNewUnitTests=false` — ✅ respected; no test project was created.
- `passUnitTests=true` — ✅ trivially satisfied; no test project exists anywhere in the solution
  (confirmed before and after this task), so there are no tests to fail.
- `securityComplianceCheck=true` — ✅ confirmed via `dotnet list package --vulnerable
  --include-transitive` returning no vulnerable packages (both the direct NU1903
  `Microsoft.Data.SqlClient` finding from task 01 and the transitive `System.Drawing.Common`
  finding introduced by `MSMQ.Messaging` in this task are resolved).

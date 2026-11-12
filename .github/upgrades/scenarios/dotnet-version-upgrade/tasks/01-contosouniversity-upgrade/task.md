# Task 01: ContosoUniversity — Framework 4.8 → .NET 10.0 (All-at-Once, In-place rewrite)

Status: in_progress (manual execution — `start_task`/`complete_task` MCP tools are non-functional
in this session; see scenario-instructions.md "Known Tool Issues" for details. Proceeding per the
execution.md contract manually, with this file serving as the enriched task memory.)

## Objective
Convert ContosoUniversity.csproj from legacy .NET Framework 4.8 / System.Web MVC 5 to an
SDK-style net10.0 ASP.NET Core MVC project, in a single pass (small solution, 1 project).

## Research Findings (complete inventory)

### Project file
- Legacy non-SDK csproj, `TargetFrameworkVersion=v4.8`, `OutputType=Library`, Web Application
  project type GUIDs, `packages.config`-based restore, 45 packages.
- Must become SDK-style `Microsoft.NET.Sdk.Web`, `<TargetFramework>net10.0</TargetFramework>`,
  `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- `Content/bootstrap.css`/`bootstrap.min.css` referenced in csproj but **not present on disk**
  (never NuGet-restored in this sandbox). `Scripts/*.js` ARE checked in but at older jquery
  version (3.4.1) than packages.config declares (3.7.1). Resolution: fetch pinned-version static
  assets (bootstrap 5.3.3 css+js, jquery 3.7.1, jquery-validation 1.21.0, modernizr 2.6.2 —
  matching packages.config versions) into `wwwroot/lib/` for the Core static-file model
  (internet access confirmed available).

### Packages (target versions resolved via get_supported_package_version)
| Package | Old | New | Notes |
|---|---|---|---|
| Microsoft.EntityFrameworkCore | 3.1.32 | keep EFCore but bump | already EF Core, not EF6 |
| Microsoft.EntityFrameworkCore.SqlServer | 3.1.32 | 10.0.15 | |
| Microsoft.Data.SqlClient | 2.1.4 | 7.1.1 | security vuln fix (CVE) per assessment |
| Newtonsoft.Json | 13.0.3 | 13.0.4 | used by NotificationService for queue message body |
| System.Messaging (GAC ref) | n/a | MSMQ.Messaging 1.0.4 | see MSMQ section below |
| Antlr | 3.4.1.9004 | **removed** | confirmed unused (grep: 0 hits in .cs) — was pulled in transitively, binding-redirect only |
| Microsoft.Identity.Client | 4.21.1 | **removed** | confirmed unused (grep: 0 hits in .cs) |
| Microsoft.AspNet.Mvc / WebPages / Razor / Web.Optimization / WebGrease / Microsoft.Web.Infrastructure / Microsoft.CodeDom.Providers.DotNetCompilerPlatform / NETStandard.Library / System.Buffers / System.Collections.Immutable / System.ComponentModel.Annotations / System.Diagnostics.DiagnosticSource / System.Memory / System.Numerics.Vectors / System.Runtime.CompilerServices.Unsafe / System.Threading.Tasks.Extensions / Microsoft.Bcl.* / bootstrap / jQuery / jQuery.Validation / Microsoft.jQuery.Unobtrusive.Validation / Modernizr | various | **removed** | all obsolete under SDK-style/.NET 10 — superseded by framework-provided or static assets |

### MSMQ (Services/NotificationService.cs)
- Uses `System.Messaging.MessageQueue/Message/XmlMessageFormatter` (Windows-only GAC assembly,
  referenced directly in old csproj as `<Reference Include="System.Messaging"/>`).
- Per `migrating-to-msmq-messaging` skill: replace `using System.Messaging` → `using MSMQ.Messaging`
  (identical API/class names), add `MSMQ.Messaging` 1.0.4 PackageReference, move
  `NotificationQueuePath` from Web.config appSettings → `appsettings.json` "MessageQueue:QueuePath",
  bind via `IOptions<MessageQueueOptions>` injected into `NotificationService` (replacing
  `ConfigurationManager.AppSettings`).
- Risk flagged: MSMQ itself is a Windows OS feature; this Linux sandbox cannot exercise it at
  runtime, but the code must still *compile* (build is the success criterion, no MSMQ-dependent
  unit tests exist). Preserved as-is functionally, only namespace/config/DI updated.
- `Services/LoggingService.cs` is a 0-byte empty file, confirmed unreferenced anywhere — delete it.

### Data layer
- `Data/SchoolContext.cs` — pure EF Core DbContext, no System.Web dependency, compiles unchanged.
- `Data/SchoolContextFactory.cs` — static factory using `ConfigurationManager.ConnectionStrings`;
  **replace** with constructor-injected `DbContextOptions<SchoolContext>` via
  `builder.Services.AddDbContext<SchoolContext>(o => o.UseSqlServer(connectionString))` in
  Program.cs; delete the factory, inject `SchoolContext` directly into `BaseController`.
- `Data/DbInitializer.cs` — static seeding method, framework-agnostic, unchanged; invoked once at
  startup via a DI scope in Program.cs (replacing Global.asax `InitializeDatabase()`).

### Models
- All of `Models/*.cs` and `Models/SchoolViewModels/*.cs` use only `System.ComponentModel.DataAnnotations`
  — fully portable, **zero changes needed**.
- `PaginatedList.cs` — plain generic List<T> helper, framework-agnostic, unchanged.

### Controllers (7 total, all converting System.Web.Mvc → Microsoft.AspNetCore.Mvc)
- `BaseController`: ctor-inject `SchoolContext db` + `NotificationService notificationService`
  (both via DI) instead of static factory / `new NotificationService()`. Keep hardcoded
  `"System"` username (no auth system present — out of scope to add one).
- `HomeController`: trivial, `ActionResult`→`IActionResult`, no other changes. Note:
  `Unauthorized()` action has no matching view in Views/Home — pre-existing dead code, left as-is
  (not reachable, no AuthorizeAttribute registered; preserving existing behavior, not introducing
  regressions).
- `StudentsController`, `DepartmentsController`, `CoursesController`, `InstructorsController`,
  `NotificationsController`: convert API surface per mapping table below. `DepartmentsController`
  has `DbUpdateConcurrencyException` handling — preserve as-is (EF Core exception, unchanged).
  `InstructorsController.Edit(POST)` uses `TryUpdateModel(obj, prefix, string[])` →
  becomes `async Task<IActionResult>` using
  `await TryUpdateModelAsync<Instructor>(instructorToUpdate, "", i => i.LastName, i => i.FirstMidName, i => i.HireDate, i => i.OfficeAssignment)`.
  `CoursesController` file upload: `Server.MapPath` → injected `IWebHostEnvironment.WebRootPath`;
  `HttpPostedFileBase` → `IFormFile` (`.Length`, `.FileName`, `.CopyTo(stream)` instead of `.SaveAs`).

#### API mapping applied across controllers
| System.Web.Mvc | Microsoft.AspNetCore.Mvc |
|---|---|
| `ActionResult` | `IActionResult` |
| `HttpNotFound()` | `NotFound()` |
| `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` | `BadRequest()` |
| `JsonRequestBehavior.AllowGet` param to `Json()` | removed (not needed; GET JSON allowed by default) |
| `[Bind(Include = "...")]` | kept as `[Bind("...")]` (still supported in ASP.NET Core) |
| `Server.MapPath(...)` | `IWebHostEnvironment.WebRootPath` / `Path.Combine` |
| `HttpPostedFileBase` | `IFormFile` |
| `Trace.TraceError` (System.Diagnostics) | unchanged, still available |

### Views (25 .cshtml files reviewed)
- `Html.BeginForm`, `Html.AntiForgeryToken`, `Html.ValidationSummary`, `Html.LabelFor`,
  `Html.EditorFor`, `Html.ValidationMessageFor`, `Html.HiddenFor`, `Html.DropDownList`,
  `Html.DisplayNameFor`, `Html.DisplayFor`, `Html.ActionLink`, `Url.Action`, `Url.Content` are ALL
  available as-is in ASP.NET Core MVC's `IHtmlHelper` — **no changes needed** to the bulk of view
  markup.
- `@Scripts.Render(...)` / `@Styles.Render(...)` (System.Web.Optimization) do **not** exist in
  ASP.NET Core — replace with static `<script src="~/lib/...">` / `<link href="~/lib/...">` tags
  in `_Layout.cshtml`; remove the `@section Scripts { @Scripts.Render(...) }` blocks from
  Create/Edit views of Courses/Departments/Instructors/Students (jquery validation now loaded
  globally in layout, so these empty sections can simply be dropped).
- `Views/Shared/Error.cshtml` uses `System.Web.Mvc.HandleErrorInfo` + `HttpContext.Current` —
  rewrite to use the existing `Models/ErrorViewModel.cs` (`@model ContosoUniversity.Models.ErrorViewModel`)
  matching the standard ASP.NET Core template; `HomeController.Error()` updated to populate it with
  `Activity.Current?.Id ?? HttpContext.TraceIdentifier`.
  `FilterConfig.HandleErrorAttribute` global filter → replaced by
  `app.UseExceptionHandler("/Home/Error")` middleware in Program.cs.
- `Views/Web.config` (Razor host config) — **deleted**; ASP.NET Core uses `_ViewImports.cshtml`
  instead (new file, `@using ContosoUniversity`, `@using ContosoUniversity.Models`,
  `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`).
- `_ViewStart.cshtml` — layout path unchanged, kept as-is.

### App_Start / Global.asax → Program.cs
- `RouteConfig`: single default route + axd ignore (irrelevant in Core) →
  `app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")`.
- `FilterConfig`: only `HandleErrorAttribute` (commented-out `AuthorizeAttribute`, confirming
  **no auth system in this app** — preserved, no auth added) → handled via exception middleware.
- `BundleConfig`: 5 bundles → static asset references in `_Layout.cshtml` head/body
  (bootstrap.css+site.css in `<head>`, modernizr early in `<head>`, jquery+bootstrap.js+
  jquery.validate*+notifications.js before `</body>`).
- `Global.asax`/`Global.asax.cs` → deleted; replaced by minimal-hosting `Program.cs`:
  `AddControllersWithViews()`, `AddDbContext<SchoolContext>()`, `Configure<MessageQueueOptions>()`,
  `AddScoped<NotificationService>()`, static files, exception handler, default route, and a
  startup DB-init scope calling `DbInitializer.Initialize`.

### Web.config → appsettings.json
- `ConnectionStrings:DefaultConnection` → `appsettings.json` `ConnectionStrings.DefaultConnection`
  (same LocalDB connection string content, kept verbatim — this is a sandbox/dev DB and out of
  scope to change).
- `appSettings` `NotificationQueuePath` → `appsettings.json` `MessageQueue:QueuePath`.
- `ClientValidationEnabled` / `UnobtrusiveJavaScriptEnabled` / `webpages:*` → **dropped**, these
  are System.Web-era Razor engine flags with no ASP.NET Core equivalent/need (unobtrusive
  validation is simply enabled by including jquery.validate.unobtrusive.js, which is preserved).
- 20 assembly `<bindingRedirect>` entries — **documented here then removed** (per "Document and
  Review Before Removing" Upgrade Option): all redirects target assemblies that either no longer
  exist as separate deployable assemblies under .NET 10 (they're part of the shared framework /
  replaced NuGet packages), so binding redirects (a .NET Framework GAC/fusion concept) are not
  applicable under .NET 10's assembly-loading model. Full original list preserved below for audit:
  Microsoft.Web.Infrastructure, Antlr3.Runtime, Newtonsoft.Json, System.Web.Optimization,
  WebGrease, System.Web.Helpers, System.Web.WebPages, System.Web.Mvc,
  System.Threading.Tasks.Extensions, Microsoft.Extensions.DependencyInjection(.Abstractions),
  Microsoft.EntityFrameworkCore.Abstractions, Microsoft.Extensions.Caching.Abstractions,
  Microsoft.Extensions.Configuration.Abstractions, Microsoft.Extensions.Logging.Abstractions,
  Microsoft.Extensions.Options, Microsoft.Extensions.Primitives, System.ComponentModel.Annotations,
  System.Runtime.CompilerServices.Unsafe, System.Memory, Microsoft.Data.SqlClient, netstandard.
- `<system.web>`/`<system.webServer>` IIS/ASP.NET classic settings (maxRequestLength, httpRuntime,
  handlers) — dropped; equivalents (`IFormOptions.MultipartBodyLengthLimit`,
  Kestrel/Request body size limits) not currently needed since no evidence of hitting these
  limits functionally; the 10MB upload limit is already enforced explicitly in
  `CoursesController` (5MB check) at the application level.

## Done-when (from plan.md)
- [ ] `ContosoUniversity.csproj` is SDK-style, targets net10.0, nullable enabled, builds with `dotnet build`
- [ ] Global.asax/.cs removed, Program.cs created with full DI/middleware pipeline
- [ ] All 7 controllers converted to Microsoft.AspNetCore.Mvc, compiling without errors/warnings
- [ ] Web.config migrated to appsettings.json (connection string + queue path); binding redirects documented above then dropped
- [ ] Views updated: Scripts/Styles.Render replaced with static tags, Error.cshtml modernized, Views/Web.config removed, _ViewImports.cshtml added
- [ ] wwwroot/lib static assets staged (bootstrap, jquery, jquery-validation, modernizr matching packages.config versions)
- [ ] MSMQ migrated to MSMQ.Messaging + options pattern
- [ ] Antlr / Microsoft.Identity.Client / all System.Web.* packages removed
- [ ] `dotnet build` succeeds with zero errors and zero warnings
- [ ] No test projects exist (confirmed via .sln inspection) — passUnitTests trivially satisfied
- [ ] progress-details.md written documenting all changes

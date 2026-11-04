# Progress Details: 01-contosouniversity-upgrade

## Summary

ContosoUniversity was migrated from ASP.NET MVC 5 / .NET Framework 4.8 to ASP.NET Core MVC on
.NET 10.0. This was an in-place, all-at-once rewrite (Direct Migration — no System.Web Adapters
shim), consistent with the confirmed Upgrade Options in `scenario-instructions.md`.

## Known tool limitation (disclosed)

`start_task` could not parse `plan.md` in this environment: the plan-parser's internal LLM call
uses MCP sampling, and every sampling request in this CLI host deterministically returns
"The user cancelled the request" (confirmed via the MCP server's own log files, 5/5 retries
failing identically). This is a host/environment infrastructure limitation, not a plan.md
formatting problem. Work was tracked manually in this task folder instead, and `complete_task`
was verified to work normally (it does not depend on the same code path).

## Files modified / added / removed

### Project file
- `ContosoUniversity.csproj` — retargeted `net48` → `net10.0`; removed framework-included
  packages (Microsoft.AspNet.Mvc/Razor/WebPages, Microsoft.Web.Infrastructure,
  Microsoft.CodeDom.Providers.DotNetCompilerPlatform, NETStandard.Library, Antlr, Microsoft.Identity.Client,
  bootstrap/jQuery/Modernizr/WebGrease NuGet packages, Microsoft.AspNet.Web.Optimization); removed
  legacy `<Reference>` items (System.Messaging, System.Configuration, netstandard); bumped
  Microsoft.EntityFrameworkCore*, Microsoft.EntityFrameworkCore.SqlServer, and
  Microsoft.EntityFrameworkCore.Tools to 10.0.14; bumped Microsoft.Data.SqlClient to 7.1.1
  (fixing the one reported security vulnerability); added a direct, pinned reference to
  `System.Security.Cryptography.Pkcs` 10.0.14 to override a vulnerable transitive version
  (9.0.18) pulled in by Microsoft.Data.SqlClient; enabled `ImplicitUsings`.
- `packages.config` — removed (replaced entirely by PackageReference in the SDK-style csproj).

### Startup / hosting
- `Global.asax`, `Global.asax.cs` — removed; replaced by `Program.cs` (minimal hosting model):
  registers `AddControllersWithViews()`, `AddDbContext<SchoolContext>` (connection string from
  configuration), `NotificationService` as a DI singleton, conventional routing, exception
  handling middleware, static files, and the database initialization call (ported from
  `Application_Start`).
- `App_Start/RouteConfig.cs`, `FilterConfig.cs`, `BundleConfig.cs` — removed; routing now
  configured in `Program.cs` (`MapControllerRoute`); bundling replaced by static `<script>`/
  `<link>` tags in views (see below).
- `Properties/AssemblyInfo.cs` — removed (SDK-style projects auto-generate assembly attributes;
  keeping this file would cause duplicate-attribute build errors).
- `Web.config`, `Web.Debug.config`, `Web.Release.config`, `Views/Web.config` — removed; replaced
  by `appsettings.json` / `appsettings.Development.json` (connection string + logging config).

### Static assets
- `Content/`, `Scripts/`, `Uploads/` moved into `wwwroot/` (standard ASP.NET Core static file
  location); relative URL paths (`~/Content/...`, `~/Scripts/...`, `~/Uploads/...`) preserved.
- `Views/Shared/_Layout.cshtml` and 7 Create/Edit views (`Departments`, `Courses`, `Students`,
  `Instructors`) — replaced `@Styles.Render`/`@Scripts.Render` bundle calls with static
  `<script>`/`<link>` tags pointing at the actual files under `wwwroot`.
- `Views/Shared/Error.cshtml` — removed the `System.Web.Mvc.HandleErrorInfo` model and
  `HttpContext.Current.IsDebuggingEnabled` check (no ASP.NET Core equivalent in this simple
  form); kept a static error message.
- `Views/_ViewImports.cshtml` — added (did not exist before; views used Views/Web.config's
  `<namespaces>` section, which has no ASP.NET Core equivalent). Imports `ContosoUniversity`,
  `ContosoUniversity.Models`, `ContosoUniversity.Models.SchoolViewModels`, and registers the
  built-in Tag Helpers.

### NotificationService (architectural change — disclosed)
- `Services/NotificationService.cs` — rewritten. The original implementation used
  `System.Messaging.MessageQueue` (MSMQ), which is a **Windows-only** technology with **no
  supported equivalent on modern, cross-platform .NET** (not a namespace rename — a genuine
  platform incompatibility). Replaced with a `ConcurrentQueue<Notification>`-backed in-process
  queue, registered as a DI singleton, preserving the exact same public method signatures
  (`SendNotification`, `ReceiveNotification`, `MarkAsRead`, `Dispose`) used by
  `BaseController`/`NotificationsController`. **Behavioral implication**: notifications are now
  process-local and in-memory only — they do not survive an app restart and are not shared across
  multiple instances/processes, unlike the original durable, cross-process MSMQ queue. This was a
  deliberate, scoped decision (per the confirmed "Resolve Inline" / "Direct Migration" options)
  given MSMQ's platform incompatibility.

### Controllers
- `BaseController.cs` — changed from parameterless constructor with manual
  `SchoolContextFactory.Create()` / `new NotificationService()` to constructor-based DI
  (`SchoolContext`, `NotificationService`).
- All 6 controllers (`Home`, `Students`, `Departments`, `Instructors`, `Courses`,
  `Notifications`) — added pass-through constructors for DI; namespace swapped
  `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc` (+ `Microsoft.AspNetCore.Mvc.Rendering` for
  `SelectList` where used).
- `HomeController.cs` — renamed `Unauthorized()` action to `AccessDenied()` to avoid hiding the
  inherited `ControllerBase.Unauthorized()` member (fixes CS0114 warning); no other code
  referenced the old action name.
- `StudentsController.cs`, `DepartmentsController.cs` — `HttpStatusCodeResult(BadRequest)` →
  `BadRequest()`; `HttpNotFound()` → `NotFound()`; `[Bind(Include = "...")]` → `[Bind("...")]`.
- `InstructorsController.cs` — same fixes as above, plus: `TryUpdateModel(model, prefix,
  string[])` (no direct ASP.NET Core equivalent) rewritten as `async Task<ActionResult> Edit(...)`
  using expression-based `await TryUpdateModelAsync(instructorToUpdate, "", i => i.LastName, ...)`.
- `CoursesController.cs` — `HttpPostedFileBase` → `IFormFile`; `Server.MapPath(...)` →
  `IWebHostEnvironment.WebRootPath` (injected via constructor); `.ContentLength` → `.Length`;
  `SaveAs(...)` → `async`/`CopyToAsync` with `FileStream`; qualified `System.IO.File.Exists/Delete`
  calls to avoid ambiguity with the inherited `ControllerBase.File(...)` method.
- `NotificationsController.cs` — removed `JsonRequestBehavior.AllowGet` argument (no longer
  needed/available; ASP.NET Core `JsonResult` has no such restriction).

### Data layer
- `Data/SchoolContextFactory.cs` — removed (depended on `System.Configuration.ConfigurationManager`,
  unavailable after migrating config to `appsettings.json`; no longer referenced now that
  `SchoolContext` is registered and resolved via DI in `Program.cs`).
- `Data/SchoolContext.cs`, `Data/DbInitializer.cs`, `PaginatedList.cs`, `Models/*` — unchanged;
  already framework-agnostic (pure EF Core / POCOs with no `System.Web` dependency).

### Antlr package (disclosed decision)
- The assessment recommended replacing `Antlr` 3.4.1.9004 with `Antlr4` 4.6.6. A full-repo search
  found **zero** direct Antlr API usage in code, so the package was simply **removed** rather than
  replaced — avoids adding an unused dependency, no functional loss.

## Build / validation results

- `dotnet build ContosoUniversity.sln` (and the single `.csproj`) → **Build succeeded, 0
  Warning(s), 0 Error(s)**.
- No test projects exist anywhere in the repository (`find . -iname "*Test*.csproj"` returned
  nothing; confirmed via `dotnet test` as well, which found no test projects to run). The
  `passUnitTests=true` success criterion is trivially satisfied since there are no unit tests to
  fail.

## Environment workaround used during this task (not part of the repo)

This Linux sandbox lacks Visual Studio's `Microsoft.WebApplication.targets`, which the original
legacy `.csproj` imported. A minimal stub targets file was created at
`/usr/share/dotnet/sdk/10.0.403/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets`
to unblock MSBuild evaluation for MCP tooling (`get_dotnet_upgrade_options`,
`generate_dotnet_upgrade_assessment`, `convert_project_to_sdk_style`). This is an ephemeral,
sandbox-only workaround — it is not committed to the repository and has no effect outside this
environment.

# Task 02 — Full ASP.NET Core / .NET 10.0 migration — Progress Details

## Outcome: Completed. `dotnet build` succeeds with 0 errors and 0 warnings on net10.0.

## Summary of changes

### Project file (`ContosoUniversity.csproj`)
- Retargeted `net48` → `net10.0`.
- Enabled `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- Removed all legacy framework `<Reference>` items (`Microsoft.CSharp`, `System.Configuration`,
  `System.Messaging`, `netstandard`, `System.Net.Http`, etc.) — no longer needed under the
  modern SDK/BCL.
- Removed the `CopySQLClientNativeBinaries` custom target (Windows-only SNI native binary
  copy step tied to the old `Microsoft.Data.SqlClient.SNI.runtime` package layout; the
  current `Microsoft.Data.SqlClient` 7.1.1 handles this internally).
- Removed all MVC5/WebPages/Optimization/Antlr/WebGrease/jQuery* "classic ASP.NET" packages.
- Package versions bumped to current stable:
  - `Microsoft.EntityFrameworkCore`, `.SqlServer`, `.Tools`: 3.1.32 → 10.0.14
  - `Microsoft.Data.SqlClient`: 2.1.4 → 7.1.1
  - `Newtonsoft.Json`: 13.0.3 → 13.0.4
  - Added `MSMQ.Messaging` 1.0.4 (drop-in replacement exposing an MSMQ API for .NET Core;
    see MSMQ note below)
  - Pinned `System.DirectoryServices`, `System.Drawing.Common`, `System.Security.Cryptography.Pkcs`
    to 10.0.14 to resolve NU1902/NU1903/NU1904 vulnerability warnings on transitive
    dependencies pulled in by `MSMQ.Messaging` (which otherwise resolves old/vulnerable
    versions of these).
- Removed all explicit `<Content>`/`<None>` item globs for static assets — the SDK's default
  globbing + the new `wwwroot` convention handle these.

### Hosting model
- Removed `Global.asax` / `Global.asax.cs`.
- Removed `App_Start/RouteConfig.cs`, `FilterConfig.cs`, `BundleConfig.cs`.
- Added `Program.cs` using the minimal-hosting model: registers MVC (`AddControllersWithViews`),
  registers `SchoolContext` via `AddDbContext` (SQL Server provider, connection string from
  `IConfiguration`), registers `NotificationService` as a DI singleton, configures the
  standard ASP.NET Core middleware pipeline (`UseExceptionHandler`/`UseHsts` in production,
  `UseHttpsRedirection`, `UseStaticFiles`, `UseRouting`, default `MapControllerRoute`), and
  runs `DbInitializer.Initialize` once at startup using a created DI scope (replacing the
  original `Application_Start` call).

### Configuration
- Removed `Web.config` (and its `Web.Debug.config`/`Web.Release.config` transforms).
- Added `appsettings.json` with the `DefaultConnection` connection string and the
  `NotificationQueuePath` setting (previously `<appSettings>` keys).
- Added `appsettings.Development.json`.
- Removed the `<runtime><assemblyBinding>` binding-redirect section entirely — not needed
  under .NET (CLR) / PackageReference-based dependency resolution. This matches the
  "Remove" binding-redirects option confirmed during planning.
- Removed `Data/SchoolContextFactory.cs` (manual `ConfigurationManager`-based factory) in
  favor of standard DI-registered `DbContext` + constructor injection.

### Static assets
- Moved `Content/`, `Scripts/`, `Uploads/`, `favicon.ico` under `wwwroot/` (ASP.NET Core's
  static-file convention). Virtual paths (`~/Content/...`, `~/Scripts/...`,
  `~/Uploads/...`) are unchanged since `wwwroot` is the static-file root.
- Removed all `@Scripts.Render(...)` / `@Styles.Render(...)` bundle calls (the
  `Microsoft.AspNet.Web.Optimization` package has no ASP.NET Core equivalent — confirmed
  "Resolve Inline" option from planning) and replaced them with direct `<link>`/`<script>`
  tags in `_Layout.cshtml` and the 8 Create/Edit views that referenced the `jqueryval`
  bundle for client-side validation scripts.
- Added `Views/_ViewImports.cshtml` registering the built-in ASP.NET Core Tag Helpers.

### Controllers (all 7)
- Changed base namespace from `System.Web.Mvc` to `Microsoft.AspNetCore.Mvc` throughout.
- `BaseController`: now takes `SchoolContext` and `NotificationService` via constructor
  injection (removed the manual `SchoolContextFactory.Create()` / `new NotificationService()`
  wiring and the manual `Dispose` override — both are now DI-container-managed lifetimes).
  All 7 concrete controllers updated with matching DI constructors.
- `HttpNotFound()` → `NotFound()`; `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` →
  `BadRequest()`.
- `[Bind(Include = "...")]` → `[Bind("...")]` (ASP.NET Core `BindAttribute` constructor shape).
- `CoursesController`: `HttpPostedFileBase` → `IFormFile`; `.ContentLength` → `.Length`;
  `.SaveAs(path)` → `FileStream` + `CopyTo`; `Server.MapPath(...)` → a small `MapPath` helper
  built on injected `IWebHostEnvironment.WebRootPath` (uploads now live under
  `wwwroot/Uploads/TeachingMaterials`).
- `NotificationsController`: `JsonResult`/`JsonRequestBehavior.AllowGet` → plain `Json(...)`
  (ASP.NET Core doesn't restrict GET+JSON the way MVC5 did, so the behavior argument is
  simply dropped).
- `InstructorsController.Edit(int?, string[])`: MVC5's
  `TryUpdateModel(model, prefix, string[] includeProperties)` has no direct ASP.NET Core
  equivalent; replaced with the async `TryUpdateModelAsync(model, prefix, propertyFilter)`
  overload using a `HashSet<string>` property-name filter (action made `async Task<ActionResult>`).

### Services
- `NotificationService`: `using System.Messaging;` → `using MSMQ.Messaging;` (the actual
  namespace exposed by the `MSMQ.Messaging` NuGet package is `MSMQ.Messaging`, not
  `System.Messaging`, despite the package's purpose). Switched from constructor-read
  `ConfigurationManager.AppSettings` to injected `IConfiguration`. API surface
  (`SendNotification`, `ReceiveNotification`, `MarkAsRead`) is otherwise unchanged.
  **Runtime note**: MSMQ is a Windows-only OS feature; this service will only function at
  runtime on Windows with MSMQ installed, same as the original. This was flagged as a known
  risk during planning (Feature not caught by the automated assessment).

### Views
- `Views/Shared/Error.cshtml`: removed `@model System.Web.Mvc.HandleErrorInfo` and the
  `HttpContext.Current.IsDebuggingEnabled` check (no `[HandleError]` filter equivalent is
  wired up in Core; simplified to a static error view, matching what `HomeController.Error()`
  already returns with no model).
- All other 22 views needed **no structural changes** — `Html.ActionLink`, `Html.BeginForm`,
  `Html.EditorFor`, `Html.DisplayFor`, `Html.LabelFor`, `Html.ValidationMessageFor`,
  `Html.ValidationSummary`, `Html.DropDownList`, `Html.AntiForgeryToken` all have direct
  ASP.NET Core `HtmlHelper` equivalents with identical signatures used here.

### Nullable reference types
- Enabled `<Nullable>enable</Nullable>` (confirmed option from planning) and fixed all
  resulting warnings:
  - EF Core entity/model classes: required reference-type properties initialized
    (`= string.Empty`), optional navigation properties / value types marked `?`, collection
    navigation properties initialized to empty collections.
  - Controllers: `Find(id)` results assigned via `var` (nullable-correct) with existing
    `if (x == null) return NotFound();` guards; added missing null guards to the 3
    `DeleteConfirmed` actions that previously assumed a non-null `Find` result.
  - Views: added `!` null-forgiving operator on `model.Administrator!.FullName` /
    `model.OfficeAssignment!.Location` expression-tree lambdas passed to
    `DisplayFor`/`DisplayNameFor`/`LabelFor`/`EditorFor`/`ValidationMessageFor` (these
    helpers only use the lambda to resolve a member path; they do not dereference at that
    point, so this doesn't change runtime behavior — matches pre-existing MVC5 behavior
    where `OfficeAssignment` can be null for a brand-new `Instructor`).

## Build validation
```
dotnet build
...
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Known environment limitations at runtime (not migration defects)
This Linux CI sandbox has no SQL Server / LocalDB and no MSMQ, so a full end-to-end runtime
smoke test could not be completed here:
- `dotnet run` builds and starts, but `DbInitializer.Initialize` fails with
  `PlatformNotSupportedException: LocalDB is not supported on this platform` when it tries to
  open the configured `(LocalDB)\MSSQLLocalDB` connection — this requires a real SQL Server
  instance (LocalDB, a container, or Azure SQL) to validate against, consistent with the
  original application's requirements.
- `NotificationService`'s MSMQ calls require Windows + the MSMQ Windows feature; they cannot
  be exercised on Linux.

Both are pre-existing infrastructure dependencies of the original application (not introduced
by this migration) and would need a Windows host with SQL Server / MSMQ available to validate
at runtime.

## Tests
No automated test project exists in the repository (confirmed via assessment data —
`TestCoverageRecommendedProjectCount: 0` — and manual repository inspection). The
`passUnitTests: true` success criterion has no existing test suite to run; this is a
pre-existing gap, not something introduced or regressed by this migration.

## Tooling note
`start_task`/`complete_task` could not be used for this task — see scenario-instructions.md
and the final summary for details on why (no MCP sampling support in this CLI host). This
file was written manually to preserve the same record these tools would have produced.

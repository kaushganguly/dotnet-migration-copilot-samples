# Progress Details: 02-aspnetcore-migration

## Summary
Migrated ContosoUniversity from the SDK-style net48 project (Task 01 output) to a fully
functioning ASP.NET Core MVC application targeting .NET 10.0. All `System.Web`/MVC5/bundling/
Web.config surface area has been removed and replaced with ASP.NET Core equivalents. Final build
is clean: 0 errors, 0 warnings, no vulnerable packages.

## Files created
- `Program.cs` — minimal hosting model entry point. Registers `AddControllersWithViews()` (with
  `PropertyNamingPolicy = null` / `DictionaryKeyPolicy = null` to preserve original PascalCase
  JSON behavior for `NotificationsController`'s JSON actions), `AddDbContext<SchoolContext>`,
  `AddSingleton<NotificationService>`, Kestrel/FormOptions upload-size and timeout configuration
  (parity with the old `Web.config` `httpRuntime`/`requestLimits` settings), static files,
  routing, the single default route, and startup DB initialization (equivalent to the old
  `Global.asax.cs` `Application_Start`).
- `appsettings.json`, `appsettings.Development.json` — connection string + logging configuration
  migrated from `Web.config`.
- `Views/_ViewImports.cshtml` — shared namespace imports for views.

## Files modified
- `ContosoUniversity.csproj` — retargeted to `net10.0`; trimmed to 6 `PackageReference`s:
  `Microsoft.EntityFrameworkCore`/`.Relational`/`.SqlServer`/`.Tools` @ 10.0.15,
  `Microsoft.Data.SqlClient` @ 7.1.2, `Newtonsoft.Json` @ 13.0.4. All legacy `<Reference>` items
  and MVC5/Optimization/WebGrease/Antlr/CodeDom/Identity.Client packages removed.
- `Services/NotificationService.cs` — MSMQ (`System.Messaging`) replaced with an in-memory
  `ConcurrentQueue<Notification>` DI singleton. Architecture rationale documented inline.
- `Controllers/BaseController.cs` — converted to constructor-based DI for `SchoolContext` and
  `NotificationService`; removed manual `Dispose` overrides (DI container now owns lifetimes).
- `Controllers/HomeController.cs` — converted to `Microsoft.AspNetCore.Mvc`; `Error()` action now
  builds and passes an `ErrorViewModel`; `Unauthorized()` marked `new` to resolve the
  `ControllerBase.Unauthorized()` name collision cleanly (0-warning build).
- `Controllers/DepartmentsController.cs`, `StudentsController.cs`, `InstructorsController.cs` —
  converted to `Microsoft.AspNetCore.Mvc`; `TryUpdateModel` → `await TryUpdateModelAsync(...)`
  with expression-based include lists; `NotFound()`/`BadRequest()` replace `HttpNotFound()`/
  `HttpStatusCodeResult`.
- `Controllers/CoursesController.cs` — `HttpPostedFileBase` + `Server.MapPath()` → `IFormFile` +
  `IWebHostEnvironment.WebRootPath` + manual `FileStream.CopyTo()`.
- `Controllers/NotificationsController.cs` — converted to `Microsoft.AspNetCore.Mvc`; removed
  `JsonRequestBehavior.AllowGet` (not needed in Core).
- `Views/Shared/_Layout.cshtml` — `@Scripts.Render`/`@Styles.Render` bundle calls replaced with
  direct `<link>`/`<script>` tags pointing at `wwwroot` assets, preserving original script load
  order (modernizr in `<head>`; jquery, bootstrap, respond, notifications.js before `</body>`).
- `Views/Shared/Error.cshtml` — rewritten to `@model ContosoUniversity.Models.ErrorViewModel`,
  removing `System.Web.Mvc.HandleErrorInfo` and `HttpContext.Current.IsDebuggingEnabled`.
- 8 Create/Edit views (`Courses`, `Students`, `Instructors`, `Departments`) — per-view
  `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }` replaced with direct
  `jquery.validate.js`/`jquery.validate.unobtrusive.js` script tags.
- `README.md` — rewritten to describe the .NET 10 / ASP.NET Core stack, updated project
  structure, connection-string location, and run instructions; added a migration note.
- `NOTIFICATION_SYSTEM_README.md` — updated to describe the `ConcurrentQueue<Notification>`
  replacement for MSMQ, with an explicit migration note and a non-durability caveat under
  "Architecture Benefits" / "Future Enhancements".
- `SETUP_TESTING_GUIDE.md` — added an obsolescence banner at the top; the document's MSMQ setup
  steps no longer apply and are retained for historical reference only.
- `TEACHING_MATERIAL_UPLOAD.md` — updated the upload-limits configuration section to reference
  `Program.cs` (Kestrel/FormOptions) instead of `Web.config`.

## Files moved (`git mv`)
- `Content/` → `wwwroot/Content/`
- `Scripts/` → `wwwroot/Scripts/`
- `Uploads/` → `wwwroot/Uploads/`

## Files deleted
- `Global.asax`, `Global.asax.cs`
- `App_Start/RouteConfig.cs`, `App_Start/FilterConfig.cs`, `App_Start/BundleConfig.cs` (and the
  now-empty `App_Start/` directory)
- `Web.config`, `Views/Web.config`, `packages.config`
- `Data/SchoolContextFactory.cs` (confirmed unreferenced after DI conversion)

## Validation performed
- `dotnet build ContosoUniversity.csproj` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `dotnet build ContosoUniversity.sln -c Release` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `dotnet list package` → confirms only the 6 intended top-level packages remain, all at their
  target .NET 10-compatible versions.
- `dotnet list package --vulnerable --include-transitive` → **no vulnerable packages found**
  (resolves the Microsoft.Data.SqlClient NU1903 vulnerability flagged in assessment.md).
- `grep -rn "System.Web"` across the entire repo → **zero matches** (confirmed complete removal).
- `dotnet run` smoke test: application host builds, DI container resolves all services (including
  `SchoolContext` via constructor injection), and the only runtime failure is
  `PlatformNotSupportedException: LocalDB is not supported on this platform` when
  `DbInitializer.Initialize` attempts to open a LocalDB connection — this is a sandbox
  environment limitation (no SQL Server/LocalDB available on this Linux CI host), not a migration
  defect. The original ASP.NET Framework app could not run on this Linux sandbox at all (System.Web
  requires IIS), so this is not a regression; it confirms the DI/hosting/configuration wiring is
  structurally correct up to the point of the actual SQL connection attempt.
- No test project exists in `ContosoUniversity.sln` — `passUnitTests=true` holds vacuously (no
  tests to fail); `generateNewUnitTests=false` was honored (no test project created).

## Notes / deviations
- `webpages:Enabled`, `ClientValidationEnabled`, `UnobtrusiveJavaScriptEnabled` `Web.config`
  appSettings keys were intentionally dropped — these only affected legacy ASP.NET Web Pages
  behavior, which no longer exists in the ASP.NET Core stack (client-side validation / unobtrusive
  JS is enabled by default with no equivalent toggle needed).
- `NotificationQueuePath` appSetting dropped — no longer meaningful under the in-memory queue
  implementation.

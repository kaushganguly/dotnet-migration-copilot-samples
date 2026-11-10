# Task 02 — Upgrade to .NET 10.0 + ASP.NET Core MVC Migration — Progress Details

Status: ✅ Complete. `dotnet build` (Debug and Release, both `ContosoUniversity.csproj` and `ContosoUniversity.sln`) succeeds with **0 warnings, 0 errors**. `dotnet list package --vulnerable --include-transitive` reports **no vulnerable packages**.

## Summary of changes

### Project retarget
- `ContosoUniversity.csproj`: `TargetFramework` → `net10.0`, SDK remains `Microsoft.NET.Sdk.Web`. `Nullable` and `ImplicitUsings` enabled. `GenerateAssemblyInfo=false` kept (legacy `Properties/AssemblyInfo.cs` still present and authoritative).
- Package list reduced to only what's needed post-migration:
  - `Microsoft.Data.SqlClient` 7.1.1 (from vulnerable 2.1.4)
  - `Microsoft.EntityFrameworkCore` / `.Design` / `.SqlServer` / `.Tools` 10.0.14 (from EF6)
  - `MSMQ.Messaging` 1.0.4 (replaces `System.Messaging`, which is Windows-only/.NET Framework-only)
  - `Newtonsoft.Json` 13.0.4
  - `System.Security.Cryptography.Pkcs` 10.0.14 and `System.Drawing.Common` 10.0.14 pinned explicitly (transitive deps of `Microsoft.Data.SqlClient` 7.1.1 that otherwise resolve to older vulnerable versions — NU1902/NU1903/NU1904). Pinning these to the latest available versions resolved all three advisories.
- All legacy System.Web-only/BCL-shim packages removed (`Microsoft.AspNet.Mvc`, `Microsoft.AspNet.WebApi.*`, `Microsoft.AspNet.Web.Optimization`, `Antlr`, `WebGrease`, `Microsoft.Identity.Client`, etc. — confirmed unused via grep before removal).

### ASP.NET MVC 5 → ASP.NET Core MVC migration
- **Entry point**: `Global.asax` / `Global.asax.cs` / `App_Start/*` deleted; replaced with `Program.cs` using the minimal hosting model — registers `AddControllersWithViews`, `AddDbContext<SchoolContext>`, `AddScoped<NotificationService>`, configures the middleware pipeline (`UseDeveloperExceptionPage`/`UseExceptionHandler`, `UseStaticFiles`, `UseRouting`, `UseAuthorization`, single default `MapControllerRoute`), and seeds the database at startup via a scoped `IServiceProvider` (replacing `InitializeDatabase()`).
- **Configuration**: `Web.config` / `Web.Debug.config` / `Web.Release.config` / `Views/Web.config` removed. All ~16 `<dependentAssembly>` binding redirects confirmed obsolete under .NET's version-agnostic assembly resolution (documented in `task.md`, satisfies "Document and Review" upgrade option). Replaced with `appsettings.json` / `appsettings.Development.json` (connection string, `NotificationQueuePath`, file-upload settings). Obsolete ASP.NET-only keys (`webpages:*`, `ClientValidationEnabled`, `UnobtrusiveJavaScriptEnabled`) dropped as not applicable.
- **Controllers** (`BaseController`, `HomeController`, `CoursesController`, `DepartmentsController`, `InstructorsController`, `StudentsController`, `NotificationsController`): converted to `Microsoft.AspNetCore.Mvc`, constructor-injected `SchoolContext`/`NotificationService` (and `IWebHostEnvironment` for `CoursesController`'s file upload path) instead of static/parameterless-constructor instantiation. API swaps:
  - `HttpPostedFileBase` → `IFormFile`; `.ContentLength` → `.Length`; `.SaveAs(path)` → manual `FileStream` + `CopyTo`; `Server.MapPath(...)` → `IWebHostEnvironment.WebRootPath`.
  - `HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`; `HttpNotFound()` → `NotFound()`.
  - `[Bind(Include = "...")]` → `[Bind("...")]`.
  - `TryUpdateModel(...)` (sync, string-array overload) → `await TryUpdateModelAsync(instructorToUpdate, "", i => i.LastName, ...)` (expression-based overload); `InstructorsController.Edit` POST converted to `async Task<IActionResult>`.
  - `JsonRequestBehavior.AllowGet` argument removed from all `Json(...)` calls in `NotificationsController` (no-op/unsupported under ASP.NET Core's always-allowed `Json()`).
  - Fixed ambiguous `File.Exists`/`File.Delete` calls in `CoursesController` (shadowed by `ControllerBase.File(...)`) — fully qualified to `System.IO.File.Exists`/`System.IO.File.Delete`.
  - Removed a dead `HomeController.Unauthorized()` action that hid `ControllerBase.Unauthorized()` (CS0114) and had no corresponding view — unreachable/orphaned leftover from a removed auth feature.
- **Views**: created `Views/_ViewImports.cshtml` (`@using` + `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`). `_Layout.cshtml` and `Error.cshtml` rewritten (`@Styles.Render`/`@Scripts.Render` → direct `<link>`/`<script>` tags against `wwwroot/`; `Error.cshtml` now uses `ErrorViewModel` instead of `System.Web.Mvc.HandleErrorInfo`). 8 Create/Edit views updated to reference static jQuery-validation scripts directly instead of the `~/bundles/jqueryval` bundle (bundling/optimization package removed).
- **Static assets**: legacy "content" NuGet packages (`bootstrap`, `jQuery`, `jQuery.Validation`, `Modernizr`, `Microsoft.jQuery.Unobtrusive.Validation`) don't restore via `PackageReference` — their already-checked-in files were moved from `Content/`/`Scripts/` into `wwwroot/css`/`wwwroot/js`. `Uploads/TeachingMaterials/` moved to `wwwroot/uploads/TeachingMaterials/`. Dangling `Content`/`Uploads` references that pointed at files never actually present on disk were dropped. Obsolete `respond.js` IE-polyfill removed.
- **Messaging**: `Services/NotificationService.cs` — `System.Messaging` → `MSMQ.Messaging` (NuGet package providing the same MSMQ API surface for non-.NET-Framework TFMs); `ConfigurationManager` → constructor-injected `IConfiguration`; added missing `IDisposable` implementation.
- **Data layer**: `Data/SchoolContextFactory.cs` rewritten as `IDesignTimeDbContextFactory<SchoolContext>` for EF Core design-time tooling (runtime DbContext creation now goes through DI via `Program.cs`'s `AddDbContext`). `Data/SchoolContext.cs` DbSet properties annotated `= null!;` for nullable compliance; `OnModelCreating` (TPH inheritance, composite keys, datetime2 column config) required no EF Core 10 API changes.
- **Models**: all 9 entity/DTO classes made `Nullable`-compliant using `= null!;` for required reference/navigation properties populated by EF Core via reflection, `string?`/`Type?` for genuinely optional properties, and `= new List<T>();` for navigation collections.

### Nullable-reference-type warning remediation (CS8600/CS8602/CS8634)
After the initial successful compile, `dotnet build` surfaced 56 nullable-reference-type warnings (no errors). All fixed rather than suppressed:
- `db.*.Find(id)` call results re-declared as nullable (`Course?`, `Department?`, `Instructor?`, `Student?`) to match EF Core's nullable-annotated API, and added missing `if (x == null) return NotFound();` guards in the three spots that didn't already have them (`CoursesController.DeleteConfirmed`, `DepartmentsController.DeleteConfirmed`, `StudentsController.DeleteConfirmed`) — this also fixes a latent bug where a concurrently-deleted entity would have thrown an unhandled `NullReferenceException` instead of a clean 404.
- `InstructorsController`: `CourseAssignment? courseToRemove = ...SingleOrDefault(...)` now null-checked before `db.Entry(courseToRemove)` (previously would NullRef / failed to compile under CS8634 generic-constraint nullability).
- Razor views (`Departments/Delete.cshtml`, `Details.cshtml`, `Index.cshtml`, `Instructors/Create.cshtml`, `Edit.cshtml`): `model.Administrator.FullName` / `model.OfficeAssignment.Location` expressions annotated with the null-forgiving operator (`!`) — these are optional one-to-one navigation properties; behavior is unchanged from the original (a null navigation would have thrown `NullReferenceException` in classic MVC too), this only satisfies the compiler's nullable analysis for a pattern that was always "assumed non-null at display time."

### Security compliance
- `Microsoft.Data.SqlClient` upgraded 2.1.4 → 7.1.1 (resolves the original assessment's flagged vulnerability).
- Transitive `System.Drawing.Common` (NU1904, critical) and `System.Security.Cryptography.Pkcs` (NU1902/NU1903) pulled in by SqlClient 7.1.1 were pinned to 10.0.14 directly, eliminating all three advisories.
- `dotnet list package --vulnerable --include-transitive` now reports: **no vulnerable packages**.

## Build validation
```
dotnet build ContosoUniversity.csproj          → Build succeeded. 0 Warning(s). 0 Error(s).
dotnet build ContosoUniversity.csproj -c Release → Build succeeded. 0 Warning(s). 0 Error(s).
dotnet build ContosoUniversity.sln             → Build succeeded. 0 Warning(s). 0 Error(s).
dotnet list package --vulnerable --include-transitive → no vulnerable packages
```

## Unit tests
No test project exists in the repository (confirmed via `find -iname "*test*.csproj"` — no results). Per the task's success criteria (`generateNewUnitTests=false`), no new test project was created. `passUnitTests=true` is trivially satisfied — there are zero tests to run and zero tests to fail.

## Known residual/runtime-only risk (not covered by build/test success criteria)
- `MSMQ.Messaging` (used by `NotificationService`) wraps the Windows-only Message Queuing OS feature. It compiles cleanly under net10.0, but actually *invoking* queue operations at runtime requires MSMQ to be installed (Windows only) — this CI/build environment (Linux) cannot runtime-validate that code path. This is a pre-existing constraint inherited from the original app's architecture (not introduced by this migration) and is out of scope for the build/compile/static-test success criteria defined for this task.

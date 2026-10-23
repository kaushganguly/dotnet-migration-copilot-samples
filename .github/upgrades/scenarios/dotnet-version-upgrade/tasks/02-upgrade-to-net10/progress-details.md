# Progress Details — 02-upgrade-to-net10

**Status**: Complete

## Project file & packages
- Retargeted `ContosoUniversity.csproj` to `net10.0`; enabled
  `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- Removed all legacy Framework/System.Web/MVC5/Web.Optimization/Antlr/
  Microsoft.Identity.Client packages (verified unused via repo-wide grep
  before removal).
- Added/updated packages for net10.0: `Microsoft.EntityFrameworkCore*` 10.0.13
  (Abstractions/Analyzers/Relational/SqlServer/Tools/Design),
  `Microsoft.Data.SqlClient` 7.1.1 (replaces vulnerable 2.1.4, resolves
  assessment finding NuGet.0004), `MSMQ.Messaging` 1.0.4 (net-core-compatible
  MSMQ client, replaces `System.Messaging`), `Newtonsoft.Json` 13.0.4.
- Pinned `System.Drawing.Common`, `System.DirectoryServices`,
  `System.Security.Cryptography.Pkcs` to `10.0.13` as direct
  `PackageReference`s to override vulnerable transitive versions pulled in by
  `MSMQ.Messaging` → `System.DirectoryServices` and the SqlClient chain — this
  was required to reach 0 NuGet-vulnerability warnings.
- Removed bogus `Content` entries in the csproj that referenced non-existent
  files (`ROLE_SETUP_GUIDE.md`, `Project_Readme.txt`, a phantom
  `Uploads/TeachingMaterials/*.jpg`, and `Web.Debug.config`/`Web.Release.config`
  transforms) — these predated the SDK-style conversion and had no backing
  file on disk.
- Removed the 20+ assembly binding redirects previously in `Web.config` —
  not applicable under modern .NET assembly resolution.
- An initially-added `<InvariantGlobalization>true</InvariantGlobalization>`
  setting was found during runtime smoke testing to be incompatible with
  `Microsoft.Data.SqlClient` (throws `PlatformNotSupportedException` /
  `NotSupportedException: Globalization Invariant Mode is not supported` when
  opening a SQL connection) and was removed.

## Hosting model migration (System.Web → ASP.NET Core)
- Deleted `Global.asax`, `Global.asax.cs`, `App_Start/` (RouteConfig,
  FilterConfig, BundleConfig), `Web.config`, `Views/Web.config`,
  `Properties/AssemblyInfo.cs`.
- Created `Program.cs` using the minimal hosting model: registers
  `AddControllersWithViews()`, `AddDbContext<SchoolContext>` (SQL Server,
  connection string from configuration), MSMQ `MessageQueueOptions` via the
  Options pattern, default MVC route
  (`{controller=Home}/{action=Index}/{id?}`), static files, and a scoped
  `DbInitializer.Initialize` call at startup (same eager-initialization
  behavior as the original `Global.asax.Application_Start`).
- Added `Properties/launchSettings.json` for local dev convenience (http/https
  profiles, `ASPNETCORE_ENVIRONMENT=Development`).
- Moved `Content/`, `Scripts/`, `Uploads/` into `wwwroot/` (via `git mv`) and
  updated `.gitignore` paths (`Uploads/TeachingMaterials/*` →
  `wwwroot/Uploads/TeachingMaterials/*`, etc.) accordingly.
- Created `appsettings.json` / `appsettings.Development.json` with the SQL
  Server connection string and `MessageQueue:QueuePath` (migrated from the
  `Web.config` `NotificationQueuePath` appSetting).

## Data layer
- Removed `Data/SchoolContextFactory.cs` (static `ConfigurationManager`-based
  factory) — replaced by constructor DI (`AddDbContext` in `Program.cs`).
- Added `= null!;` initializers to all `DbSet<T>` properties in
  `SchoolContext.cs` (standard EF Core idiom for nullable-enabled contexts).

## Models
- Rewrote all model classes (`Person`, `Student`, `Instructor`, `Department`,
  `OfficeAssignment`, `CourseAssignment`, `Enrollment`, `Course`,
  `Notification`, `ErrorViewModel`, `AssignedCourseData`,
  `InstructorIndexData`) to be nullable-reference-type compliant: required
  strings default to `= string.Empty;`, genuinely-optional single-entity
  navigation properties are `Type?`, collection navigation properties are
  initialized `= new List<T>();`, and the `RowVersion` concurrency token is
  `byte[]?`.

## Services (MSMQ migration)
- Rewrote `Services/NotificationService.cs` to use the `MSMQ.Messaging`
  NuGet package instead of `System.Messaging`. Changed to **lazy** queue
  connection (constructed on first use rather than in the constructor) so
  that non-Windows hosts (MSMQ is Windows-only) only fail when a
  notification is actually sent, not on every controller instantiation.
- Added `Services/MessageQueueOptions.cs` for Options-pattern configuration
  of the queue path (bound from `MessageQueue:QueuePath` in configuration).
- Deleted `Services/LoggingService.cs` — found to be an empty/unused file.

## Controllers
- Rewrote all 7 controllers (`BaseController`, `HomeController`,
  `DepartmentsController`, `CoursesController`, `InstructorsController`,
  `StudentsController`, `NotificationsController`):
  - Namespace swap `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`.
  - Constructor DI for `SchoolContext` + `NotificationService` (replacing the
    static factory), plus `IWebHostEnvironment` (Courses, for file uploads)
    and `ILogger<T>` (Students).
  - `HttpNotFound()` → `NotFound()`; `new HttpStatusCodeResult(400)` →
    `BadRequest()`.
  - `[Bind(Include = "...")]` → `[Bind("...")]`.
  - `HttpPostedFileBase` → `IFormFile` (async save via `FileStream` +
    `CopyToAsync`).
  - `Server.MapPath(...)` → `IWebHostEnvironment.WebRootPath`.
  - `TryUpdateModel(...)` → `TryUpdateModelAsync(...)` (expression-based
    overload).
  - `Trace.TraceError(...)` → `ILogger.LogError(...)`.
  - Removed `JsonRequestBehavior.AllowGet` (not applicable to
    `Microsoft.AspNetCore.Mvc.Json`).
  - `HomeController.Unauthorized()` renamed-via-`new` to avoid hiding
    `ControllerBase.Unauthorized()` (resolved CS0114).

## Views
- Rewrote `Views/Shared/_Layout.cshtml`: replaced `@Styles.Render` /
  `@Scripts.Render` bundling with direct `<link>`/`<script>` tags against
  `wwwroot` content (per plan's "Unsupported Packages" resolution for
  `Microsoft.AspNet.Web.Optimization`).
- Created `Views/_ViewImports.cshtml` (tag helpers, shared usings).
- Rewrote `Views/Shared/Error.cshtml` to use `ErrorViewModel` instead of
  `System.Web.Mvc.HandleErrorInfo` / `HttpContext.Current`.
- Patched the 8 views using `@section Scripts { @Scripts.Render("~/bundles/jqueryval") }`
  to reference the jquery.validate scripts directly.
- Added null-forgiving (`!`) operators in 10 views
  (Students/Details, Instructors/Index+Edit+Create, Departments/Index
  +Details+Delete, Courses/Index+Details+Delete) to resolve CS8602
  nullable-dereference warnings in `LabelFor`/`DisplayFor`/etc. expression
  lambdas — compile-time only, erased from the expression tree, zero
  behavioral change.
- Confirmed via Microsoft Learn documentation that ASP.NET Core's
  `IHtmlHelper`/`IHtmlHelper<T>` retains `ActionLink`, `BeginForm`,
  `AntiForgeryToken`, `DropDownList`, `TextBox`, `Hidden`/`HiddenFor`,
  `ValidationMessage`, `ValidationSummary`, `LabelFor`, `EditorFor`,
  `DisplayFor`, `DisplayNameFor` — so view helper syntax largely ported
  unchanged.

## Build & validation
- First build: 0 errors, 29 warnings (3 NuGet vulnerability warnings + 1
  CS0114 + 25 CS8602). All warnings resolved (see package pins and view
  null-forgiving operators above).
- Final build (project and solution): **0 Warnings, 0 Errors**.
- Confirmed zero remaining `System.Web` references in any `.cs`/`.cshtml`
  file (repo-wide grep).
- `dotnet list package --vulnerable --include-transitive`: no vulnerable
  packages. `dotnet list package --deprecated`: no deprecated packages.
- Runtime smoke test (`dotnet run`) confirmed the app starts, DI container
  composes successfully, and execution reaches
  `DbInitializer.Initialize` → `EnsureCreated()`, which then fails with
  `PlatformNotSupportedException: LocalDB is not supported on this platform`.
  This is an **environment limitation, not a code defect**: LocalDB is a
  Windows-only SQL Server Express feature, and this Linux CI sandbox has no
  reachable SQL Server instance. The original .NET Framework app had the
  identical hard dependency (eager `Application_Start` DB initialization
  requiring a real SQL Server/LocalDB connection) and would fail the same
  way on a host without SQL Server — this is pre-existing behavior, not a
  regression introduced by the migration.

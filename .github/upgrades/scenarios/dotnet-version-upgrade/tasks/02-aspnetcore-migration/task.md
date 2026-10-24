# Task 02: ASP.NET MVC5/System.Web -> ASP.NET Core migration

## Objective
Migrate the ASP.NET MVC 5 / System.Web application (controllers, views, routing,
configuration, startup pipeline, notification queue service) to ASP.NET Core 10, preserving
all existing application functionality: Students, Courses, Instructors, Departments,
Notifications.

## Scope / Changes
- **Entry point / hosting**: Replaced `Global.asax`/`Global.asax.cs` +
  `App_Start/{RouteConfig,FilterConfig,BundleConfig}.cs` with a single minimal-hosting
  `Program.cs`:
  - `AddControllersWithViews()` with `AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null)`
    — **required fix**: preserves the classic PascalCase JSON wire format that
    `wwwroot/Scripts/notifications.js` depends on (`.Operation`, `.CreatedAt`, `.EntityType`,
    `.Message`, `.CreatedBy`); ASP.NET Core's default camelCase policy would otherwise silently
    break the notification dashboard UI.
  - `AddDbContext<SchoolContext>()` wired to `appsettings.json` `ConnectionStrings:DefaultConnection`
    (same value as original `Web.config`).
  - `Configure<MessageQueueOptions>()` + `AddScoped<NotificationService>()` for the MSMQ-backed
    notification service.
  - Default route registered equivalent to `RouteConfig.RegisterRoutes`
    (`{controller=Home}/{action=Index}/{id?}`); the legacy `.axd` ignore-route has no meaning
    under Kestrel and was dropped.
  - `DbInitializer.Initialize()` invoked in a startup scope, equivalent to
    `Global.asax Application_Start`.
  - `UseExceptionHandler("/Home/Error")` + `UseHsts()` for non-development environments,
    equivalent to the original `HandleErrorAttribute` global filter (the app has no
    authentication/authorization filters to carry over).
- **Configuration**: `Web.config` -> `appsettings.json` / `appsettings.Development.json`.
  Connection string and MSMQ queue path (`MessageQueue:QueuePath`) carried over verbatim.
  `Properties/launchSettings.json` added for `dotnet run` profiles.
- **Controllers**: `BaseController`, `HomeController`, `StudentsController`,
  `CoursesController`, `InstructorsController`, `DepartmentsController`,
  `NotificationsController` — ported to `Microsoft.AspNetCore.Mvc.Controller`, preserving all
  action logic, view model shapes, file-upload handling (teaching materials in
  `CoursesController`), optimistic-concurrency handling
  (`DepartmentsController`/`DbUpdateConcurrencyException`), and notification-send call sites.
  Pre-existing original-app behavior intentionally preserved as-is (not "fixed"), e.g.
  `.Single()` vs `.SingleOrDefault()` usages that throw rather than 404 on missing records.
- **Data**: `Data/SchoolContext.cs` ported to EF Core (TPH inheritance for
  Instructor/Student via `Person`, composite keys, relationships, concurrency tokens all
  preserved). `Data/SchoolContextFactory.cs` (EF6-specific design-time factory) removed — no
  longer needed under EF Core tooling.
- **Services**: `Services/NotificationService.cs` ported; underlying queue transport migrated
  from `System.Messaging` (Windows-only, built into .NET Framework) to the `MSMQ.Messaging`
  NuGet package, with the queue connection made lazy (`_queue ??= CreateQueue(...)`) so the
  app/DI container can construct without requiring MSMQ to be present at startup (relevant for
  non-Windows hosts, including this Linux build/sandbox environment, where MSMQ cannot
  function but the code must still compile and the service must still be constructible).
  New `Services/MessageQueueOptions.cs` options class added for `appsettings.json` binding.
  `Services/LoggingService.cs` removed (empty file, zero usages/call sites anywhere in the
  codebase — confirmed via repo-wide grep before removal).
- **Views**: all `.cshtml` views ported — `@Scripts.Render(...)` bundling calls
  (System.Web.Optimization, removed) replaced with explicit `<script>` tags;
  `Views/_ViewImports.cshtml` added for Tag Helpers / shared usings; nullable-safe
  null-forgiving operators (`!`) added where EF navigation properties are known non-null at
  that point in the view; `Views/Web.config` removed (no longer needed under Razor Views in
  ASP.NET Core).
- **Static assets**: `Content/`, `Scripts/`, `Uploads/` moved to `wwwroot/Content`,
  `wwwroot/Scripts`, `wwwroot/Uploads` (ASP.NET Core static file convention via
  `app.UseStaticFiles()`). `Scripts/notifications.js` relocated unchanged — its PascalCase
  JSON expectations are what drove the `Program.cs` JSON options fix above.
- **Models**: nullable reference annotations added throughout (`Course`, `CourseAssignment`,
  `Department`, `Enrollment`, `ErrorViewModel`, `Instructor`, `Notification`,
  `OfficeAssignment`, `Person`, `Student`, `SchoolViewModels/*`) — non-nullable reference
  properties given default initializers (e.g. `string Title = string.Empty;`), optional
  navigation properties marked `?`, collections given `= new List<T>()` initializers.

## Result
Build verified via `dotnet build` — 0 warnings, 0 errors. `dotnet run` smoke test confirms the
DI container, `SchoolContext`, and `NotificationService` all construct successfully; the only
runtime failure encountered (`PlatformNotSupportedException: LocalDB is not supported on this
platform`) is an environmental limitation of this Linux sandbox (no LocalDB/SQL Server
available) and not a migration defect — the original .NET Framework app has the identical
LocalDB dependency and would fail identically here.

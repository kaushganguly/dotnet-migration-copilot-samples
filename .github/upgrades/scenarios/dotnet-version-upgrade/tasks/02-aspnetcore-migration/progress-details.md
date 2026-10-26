# Task 02 Progress Details

## Summary
Completed the full ASP.NET MVC 5 -> ASP.NET Core MVC migration for the ContosoUniversity
project: new minimal-hosting `Program.cs`, `appsettings.json`-based configuration, DI-based
controllers, Core-compatible controller API usage, view fixes (bundling removal, Error view),
static asset relocation to `wwwroot`, and MSMQ/NotificationService hardening.

## Files added
- `Program.cs`
- `appsettings.json`, `appsettings.Development.json`
- `Views/_ViewImports.cshtml`
- `Services/MessageQueueOptions.cs`

## Files deleted
- `Global.asax`, `Global.asax.cs`
- `App_Start/RouteConfig.cs`, `App_Start/FilterConfig.cs`, `App_Start/BundleConfig.cs`
- `Web.config`, `Views/Web.config`
- `Data/SchoolContextFactory.cs`

## Files modified
- `Controllers/BaseController.cs` - DI constructor, removed manual Dispose
- `Controllers/HomeController.cs` - namespace, Error() rewrite, Unauthorized() `new`
- `Controllers/CoursesController.cs` - IFormFile upload, IWebHostEnvironment, async actions
- `Controllers/DepartmentsController.cs` - namespace, BadRequest/NotFound, Bind syntax
- `Controllers/InstructorsController.cs` - namespace, async TryUpdateModelAsync, Bind syntax
- `Controllers/StudentsController.cs` - namespace, BadRequest/NotFound, Bind syntax
- `Controllers/NotificationsController.cs` - namespace, Json() without JsonRequestBehavior
- `Services/NotificationService.cs` - MSMQ.Messaging namespace, IOptions config, resilience
- `Views/Shared/_Layout.cshtml` - removed bundling, direct link/script tags
- `Views/Shared/Error.cshtml` - rewritten around ErrorViewModel
- 8x Create/Edit views (Departments, Students, Courses, Instructors) - jqueryval script tags
- `ContosoUniversity.csproj` - removed Uploads glob override, added System.Security.Permissions
- `.gitignore` - updated Uploads paths to wwwroot/Uploads

## Static assets moved
- `Content/Site.css` -> `wwwroot/css/site.css`
- `Content/notifications.css` -> `wwwroot/css/notifications.css`
- `Scripts/*.js(.map)` -> `wwwroot/js/*`
- `Uploads/TeachingMaterials/` -> `wwwroot/Uploads/TeachingMaterials/`

## Validation
- `dotnet build` from a clean (wiped bin/obj) tree: **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `dotnet list package --vulnerable --include-transitive`: no vulnerable packages found.
- Local runtime smoke test (`dotnet run` + `curl`):
  - 200 OK: `/`, `/Notifications`, `/Notifications/GetNotifications`, `wwwroot/css/site.css`,
    `wwwroot/js/notifications.js`.
  - 500: `/Students`, `/Courses`, `/Departments`, `/Instructors`, `/Home/About` - all due to
    `System.PlatformNotSupportedException: LocalDB is not supported on this platform` (no SQL
    Server/LocalDB reachable in this Linux sandbox). This is an unchanged, pre-existing
    constraint from the original app (same connection string, same Windows-only LocalDB
    dependency) - not a defect introduced by the migration.
  - MSMQ absence (no Windows Message Queuing feature in this sandbox) no longer 500s every page:
    `NotificationService` now degrades gracefully when the queue is unavailable.

## Outcome
Task complete. No `System.Web`, `System.Messaging`, or other ASP.NET Framework-only API
references remain anywhere in the project (verified by the successful build, which would fail to
resolve those namespaces under the SDK-style `net10.0` TargetFramework).

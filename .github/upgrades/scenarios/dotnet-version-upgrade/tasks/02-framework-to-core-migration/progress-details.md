# Progress Details: 02-framework-to-core-migration

## Summary
Migrated ContosoUniversity from ASP.NET MVC 5 / System.Web (net48, SDK-style from task 01) to
ASP.NET Core MVC on net10.0, in-place, per the confirmed Upgrade Options
(All-at-Once / In-place rewrite / Resolve Inline / Direct Migration to ASP.NET Core APIs /
Remove Binding Redirects). Full details, research, and rationale are in `task.md` in this
folder (research was written there before any code change, per workflow requirements).

## Wire Compatibility
See `task.md` → "Wire Compatibility Gate" section for the full inventory, evidence, and PASS
verdict for `NotificationsController`'s two JSON endpoints. Mitigation applied:
`Microsoft.AspNetCore.Mvc.NewtonsoftJson` registered with the default (PascalCase) contract
resolver in `Program.cs`, so `notifications.js`'s hard-coded PascalCase field reads
(`notification.Operation`, `.CreatedAt`, `.EntityType`, `.Message`, `.CreatedBy`) keep working
unchanged against the new ASP.NET Core JSON output.

## Files changed
- `ContosoUniversity.csproj` — TargetFramework → net10.0; full package list rewrite (see task.md).
- `Program.cs` — new (replaces Global.asax/Global.asax.cs/App_Start/*.cs).
- `appsettings.json`, `appsettings.Development.json` — new.
- `Global.asax`, `Global.asax.cs` — deleted.
- `App_Start/BundleConfig.cs`, `App_Start/FilterConfig.cs`, `App_Start/RouteConfig.cs` — deleted.
- `Web.config`, `Web.Debug.config`/`Web.Release.config` (already absent), `Views/Web.config` — deleted.
- `Data/SchoolContextFactory.cs` — deleted (superseded by DI `AddDbContext`).
- `Controllers/BaseController.cs` — rewritten for constructor injection.
- `Controllers/HomeController.cs` — namespace migration + `Unauthorized()` → `new` keyword fix.
- `Controllers/StudentsController.cs` — namespace migration.
- `Controllers/CoursesController.cs` — namespace migration + `IFormFile`/`IWebHostEnvironment`.
- `Controllers/DepartmentsController.cs` — namespace migration.
- `Controllers/InstructorsController.cs` — namespace migration + async `TryUpdateModelAsync`.
- `Controllers/NotificationsController.cs` — namespace migration.
- `Services/NotificationService.cs` — `System.Messaging` → `MSMQ.Messaging` + `IOptions`.
- `Views/Shared/_Layout.cshtml` — bundle calls → direct static tags.
- `Views/Shared/Error.cshtml` — rewritten for ASP.NET Core error model.
- `Views/_ViewImports.cshtml` — new (replaces Views/Web.config default namespaces).
- `Views/{Departments,Students,Courses,Instructors}/{Create,Edit}.cshtml` (8 files) — jqueryval
  bundle → direct validation script tags.
- `Content/*` → `wwwroot/Content/*`, `Scripts/*` → `wwwroot/Scripts/*` (git renames).

## Verification performed
- `dotnet build` (Debug): 0 Warnings, 0 Errors.
- `dotnet build -c Release`: 0 Warnings, 0 Errors.
- `dotnet restore`: clean, no NuGet vulnerability warnings (pinned transitive deps).
- Full-repo grep for residual `System.Web`/`HttpContext.Current`/`System.Messaging`: none
  (except explanatory code comments).
- Manual temporary smoke test of `dotnet run` (reverted before commit) confirmed the
  ASP.NET Core host/DI/routing/Razor pipeline initializes and serves requests correctly;
  the only runtime failures observed (LocalDB and MSMQ unavailable) are pre-existing Windows-only
  platform dependencies that are outside the scope of this sandbox and were not introduced by
  the migration.
- `discover_test_projects` confirms there are no unit test projects in this repository.

## Deviation note
`start_task`/`complete_task` for this task could not be invoked (same backend tool fault
documented in `scenario-instructions.md` → "Tool Incident"). This `task.md`/`progress-details.md`
pair and the `tasks.md` checklist update were produced manually to keep the workflow's
artifact trail complete and consistent with what the tooling would have produced.

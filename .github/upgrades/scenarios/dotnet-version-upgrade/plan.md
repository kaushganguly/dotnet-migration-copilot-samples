# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Selected Strategy
**All-At-Once** — Single project solution; upgraded in one pass rather than phased/tiered.
**Rationale**: 1 project (`ContosoUniversity.csproj`), clear scope, no dependency graph to manage.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph |
| Project Approach (Web Projects) | In-place rewrite | <=10 controllers (7), 1092 LOC, task explicitly requests full in-place migration, no continuous-deployment constraint stated |
| Unsupported Packages | Resolve Inline | MVC5-era packages (Microsoft.AspNet.Mvc/Razor/WebPages/Web.Optimization, Antlr, WebGrease, Modernizr, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Web.Infrastructure) are removed and replaced by ASP.NET Core built-ins, resolved within the same pass |
| Unsupported API Handling | Fix Inline | Single project, in-place, limited complex-change count |
| Windows Native APIs | No Compatibility Pack | Sole Windows-only usage is MSMQ (System.Messaging); clear cross-platform alternative (in-process queue) exists; environment requires Linux/container compatibility |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small project, in-place rewrite, low/no HttpContext.Current usage |
| Assembly Binding Redirects | Remove Binding Redirects | Standard VS-generated redirects; not needed on SDK-style net10.0 |
| Nullable Reference Types | Leave Disabled | Default; can be enabled as a distinct follow-up effort |
| Test Coverage | Not Generate | Task success criteria explicitly sets generateNewUnitTests=false; no existing test projects |

## Project List

- `ContosoUniversity.csproj` (legacy ASP.NET MVC 5 Web Application, `net48`) -> SDK-style `Microsoft.NET.Sdk.Web`, `net10.0`, ASP.NET Core MVC

## Tasks

### 001-sdk-style-and-host-scaffold
Convert `ContosoUniversity.csproj` to SDK-style (`Microsoft.NET.Sdk.Web`, implicit file globbing,
`PackageReference` instead of `packages.config`), targeting `net10.0`. Create the ASP.NET Core
hosting scaffold: `Program.cs` (minimal hosting model) replacing `Global.asax`/`Global.asax.cs`,
`appsettings.json`/`appsettings.Development.json` replacing `Web.config` connectionStrings and
appSettings, DI container registrations (`SchoolContext` via `AddDbContext`, `NotificationService`),
static file middleware + `wwwroot` structure replacing `Content`/`Scripts`, and endpoint routing
(`MapControllerRoute`) replacing `App_Start/RouteConfig.cs`. Remove obsolete MVC5-only NuGet packages
(Antlr, WebGrease, Modernizr, Microsoft.AspNet.*, Microsoft.CodeDom.Providers.DotNetCompilerPlatform,
Microsoft.Web.Infrastructure) - ASP.NET Core provides equivalents natively. Remove assembly binding
redirects (not applicable to SDK-style/net10.0).
**Done when**: Project is SDK-style, targets net10.0, Program.cs boots an ASP.NET Core MVC app,
wwwroot contains static assets, project restores successfully.

### 002-data-layer-and-efcore-upgrade
Upgrade Microsoft.EntityFrameworkCore* packages (currently 3.1.32) and Microsoft.Data.SqlClient
to the latest versions compatible with net10.0. Port Data/SchoolContext.cs,
Data/SchoolContextFactory.cs, and Data/DbInitializer.cs - register SchoolContext via DI
(AddDbContext<SchoolContext> in Program.cs, reading the connection string from
appsettings.json) instead of the ConfigurationManager-based factory. Resolve any EF Core
3.1 -> latest breaking API changes. Models (Models/*.cs) are plain POCOs with DataAnnotations -
expected to port with no/minimal change.
**Done when**: SchoolContext is DI-registered and resolves via constructor injection, EF Core
packages are on net10.0-compatible versions, project builds with the data layer in place.

### 003-mvc-controllers-and-views-migration
Migrate the 7 controllers (BaseController, CoursesController, DepartmentsController,
HomeController, InstructorsController, NotificationsController, StudentsController) from
System.Web.Mvc to Microsoft.AspNetCore.Mvc: Controller/ActionResult base types,
IActionResult/JsonResult returns (drop JsonRequestBehavior.AllowGet - always allowed in
ASP.NET Core), model binding and validation attribute equivalents, TempData/ModelState usage.
Migrate the 26 Razor views under Views/ (layout, _ViewStart.cshtml, CRUD views for Courses/
Departments/Instructors/Students/Home) to ASP.NET Core Razor syntax - @Html.*/@Scripts.Render/
@Styles.Render (bundling) replaced with direct script/link tags against wwwroot.
Replace App_Start/FilterConfig.cs global HandleErrorAttribute with ASP.NET Core exception-handling
middleware. Each controller (with its views) is its own execution-time subtask per the
web-controller-migration-units breakdown hint (MUST - 7 > 5 controllers); use
get_code_dependencies on each controller before migrating it.
**Done when**: All 7 controllers compile against Microsoft.AspNetCore.Mvc, all 26 views render
under ASP.NET Core Razor, routing/filters/bundling equivalents are in place.

### 004-notification-service-msmq-replacement
Replace the MSMQ-backed (System.Messaging) Services/NotificationService.cs with a
cross-platform, in-process, thread-safe implementation (System.Collections.Concurrent.ConcurrentQueue<Notification>)
registered as a DI singleton, preserving the existing public surface
(SendNotification/ReceiveNotification/MarkAsRead) so BaseController and
NotificationsController require no behavioral changes beyond DI injection instead of
direct instantiation. System.Messaging has no cross-platform .NET replacement (per
Windows Native APIs option: No Compatibility Pack selected), so this is an isolated,
MUST-priority subtask per the system-messaging-replacement breakdown hint.
**Done when**: NotificationService no longer references System.Messaging, is registered via DI,
and NotificationsController/BaseController consume it successfully; project builds without
Windows-only dependencies.

### 005-final-build-and-validation
Full solution build with zero errors and zero warnings. Run `dotnet list package --vulnerable`
(and `--deprecated`) to confirm no known security vulnerabilities remain
(securityComplianceCheck success criterion). No automated tests exist in the repo and
generateNewUnitTests=false per task success criteria, so no test execution/generation is
performed - passUnitTests is satisfied vacuously (no tests to fail). Document the final state in
progress-details.md for each task.
**Done when**: dotnet build succeeds with 0 errors/0 warnings, dotnet list package --vulnerable
reports no vulnerabilities, all plan tasks are marked complete.

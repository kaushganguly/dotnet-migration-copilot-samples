# Upgrade Plan — ContosoUniversity to .NET 10.0

## Summary

ContosoUniversity.csproj is a single non-SDK-style ASP.NET MVC 5 (System.Web) project
currently targeting `net48` (effectively `net482`). It must be upgraded to `net10.0`.
Because `System.Web` has no equivalent on modern .NET, reaching `net10.0` requires a
full in-place migration to ASP.NET Core MVC, alongside the SDK-style project conversion,
package upgrades, and config/API compatibility fixes.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project (1 total) — no dependency graph to manage |
| Project Approach (Web Projects) | In-place rewrite | Small web project (7 controller files incl. base, ~2k LOC, 27 views), low assessment difficulty, no live-traffic constraint |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | No `HttpContext.Current` usage anywhere in the codebase; small, in-place project |
| Unsupported Packages | Resolve Inline | Only 1 package (`Microsoft.AspNet.Web.Optimization`) has no known replacement |
| Assembly Binding Redirects | Remove | Standard VS auto-generated boilerplate; SDK-style projects auto-manage resolution |
| Nullable Reference Types | Leave Disabled | Migration is already a full System.Web → ASP.NET Core rewrite — high-risk/demanding enough on its own |

### Selected Strategy
**All-At-Once** — The solution's single project is upgraded in one pass.
**Rationale**: 1 project, net48 → net10.0, no inter-project dependency graph.

## Project List

- `ContosoUniversity.csproj` (ASP.NET MVC 5 / System.Web, non-SDK-style, `net48`) → SDK-style `Microsoft.NET.Sdk.Web`, `net10.0`

## Tasks

### 01-migrate-to-aspnetcore-net10: Migrate ContosoUniversity from System.Web MVC 5 (net48) to ASP.NET Core MVC on net10.0

Scope: the solution's single project, `ContosoUniversity.csproj`. Convert it from a
non-SDK-style `net48` ASP.NET MVC 5 project to an SDK-style `Microsoft.NET.Sdk.Web`
project targeting `net10.0`, and migrate the application from `System.Web` to
ASP.NET Core MVC in place (In-Place mode — all migration steps execute sequentially
within this one task: project file/SDK, host, configuration, DI, controllers,
middleware, views/static assets, cleanup/verification).

Assessment context: 45 NuGet packages (26 need upgrade, 2 incompatible), 65 total
issues, 12 mandatory/potential binding-redirect issues (all standard VS
auto-generated boilerplate — to be removed). `Microsoft.AspNet.Mvc`,
`Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.Web.Infrastructure`,
and several `System.*` compatibility packages are superseded by framework/SDK
references and must be removed rather than upgraded. `Microsoft.AspNet.Web.Optimization`
is flagged incompatible with no known replacement (bundling must be redone with
ASP.NET Core static files/Tag Helpers instead). EF Core 3.1.32 is already in use
(not EF6) and should be bumped to the 10.0.x line. `Microsoft.Data.SqlClient` 2.1.4
has a known vulnerability and should be bumped (assessment suggests 7.1.1).

Known risks the executor will encounter: (1) `Global.asax.cs` and `App_Start/`
(`RouteConfig.cs`, `FilterConfig.cs`, `BundleConfig.cs`) must become `Program.cs`
minimal-hosting startup, endpoint routing, and static files; (2)
`Services/NotificationService.cs` uses `System.Messaging.MessageQueue` (MSMQ),
which does not exist on modern .NET and is Windows-only — replace with a
cross-platform in-process queue (e.g. `System.Threading.Channels`) behind the same
public method surface (`SendNotification`, `ReceiveNotification`, `MarkAsRead`) so
`BaseController` and `NotificationsController` need minimal changes; (3)
`NotificationsController.GetNotifications`/`MarkAsRead` return `JsonResult` consumed
by the app's own `Scripts/notifications.js`, which expects PascalCase property names
(`data.success`, `notification.Operation`, `notification.CreatedAt`, etc.) — this is
an internal, same-app wire contract (no external consumers, no OData), so the fix is
to configure ASP.NET Core JSON options with `PropertyNamingPolicy = null` rather than
a deeper compatibility shim; (4) `Data/SchoolContextFactory.cs` and `Global.asax.cs`
construct `SchoolContext`/read connection strings manually via `ConfigurationManager`
— replace with constructor-injected `DbContext` registered in `Program.cs` and
`IConfiguration`-based connection strings (`appsettings.json`); (5) no authentication
is configured anywhere (no `<authentication>`/`<membership>`/`machineKey` in
`Web.config`; `BaseController` hardcodes `userName = "System"`), so no auth migration
is required; (6) Views use standard Razor syntax that is mostly ASP.NET Core
compatible — check for `@Scripts.Render`/`@Styles.Render`/`Html.Action` usage in
`Views/Shared/_Layout.cshtml` and replace with ASP.NET Core static files/Tag Helpers
and partials; (7) no test projects exist in the solution, so there is nothing to
validate beyond the application build/startup itself.

Research starting points: review `assessment/projects/ContosoUniversity.md` and the
aggregate NuGet package report for the full package list; inventory `Global.asax.cs`
events, `App_Start/*.cs`, `Web.config` (`appSettings`, `connectionStrings`), and all
`Controllers/*.cs` before writing `Program.cs`; load the
`migrating-aspnet-framework-to-core` skill (In-Place mode) and its satellites
(`migrating-mvc-routing` for `RouteConfig.cs`, `migrating-mvc-razor-views` for
bundling/views) before each corresponding step; load `migrating-to-msmq-messaging`
or implement an in-process channel-based replacement for `NotificationService`.

**Done when**: `ContosoUniversity.csproj` is SDK-style, targets `net10.0`, and
contains no `packages.config`/`System.Web` references; the project builds with
`dotnet build` with 0 errors and 0 new warnings; the app starts under
`dotnet run`/`Microsoft.AspNetCore.Mvc.Testing`-style smoke check (or equivalent
headless verification) and serves the Home/Students/Courses/Instructors/Departments
pages and the `Notifications/GetNotifications` JSON endpoint with the original
response shape; no binding redirects remain in the project; `NotificationService`
no longer references `System.Messaging`.

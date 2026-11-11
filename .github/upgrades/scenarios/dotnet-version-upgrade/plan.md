# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project in the solution — no dependency graph to manage. |
| Project Approach (Web Projects) | In-place rewrite | Small web project (6 controllers, ~2k LOC), low assessment difficulty, no continuous-deployment constraint — side-by-side overhead not warranted. |
| Unsupported Packages | Resolve Inline | Small number of incompatible packages (Microsoft.AspNet.Web.Optimization), with a known replacement pattern (static asset references) — resolved within the main task. |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | In-place approach confirmed; low/isolated HttpContext.Current usage expected in a small MVC app — no compatibility shim needed. |
| Assembly Binding Redirects | Remove Binding Redirects | All 12 flagged redirects are auto-generated/boilerplate MSB3836 conflicts against the same package set being upgraded; SDK-style conversion auto-manages redirects going forward. |

### Selected Strategy
**All-At-Once** — Single project upgraded in one atomic operation.
**Rationale**: 1 project (ContosoUniversity.csproj), currently net48/classic csproj, low assessment difficulty (🟢), no dependency graph to manage.

## Tasks

### 01-sdk-style-conversion: Convert ContosoUniversity to an SDK-style project

ContosoUniversity.csproj is currently a legacy (non-SDK-style) `ClassicClassLibrary`-kind web
application project using `packages.config` for package management (45 packages). This task
converts the project file to SDK-style format on its **current** target framework (net48) —
no TFM change yet. Conversion must migrate `packages.config` entries to `PackageReference`
items, and remove items/configuration that SDK-style projects auto-generate (default
compile globs, etc.). This project also currently has 12 binding-redirect issues (6
mandatory MSB3836 conflicts, 6 downgrade warnings) in `Web.config` — per the confirmed
Assembly Binding Redirects option (Remove), strip the hand-authored `<assemblyBinding>`
entries since SDK-style/PackageReference projects auto-manage binding redirects.

Use the project conversion tool to perform the conversion, then verify the project still
restores and builds on net48 before moving to the next task (conversion is a structural
change only — it must not be conflated with the TFM/API-surface change in task 02).

**Done when**: ContosoUniversity.csproj is SDK-style, uses `PackageReference` for all
former `packages.config` entries, manual binding redirects are removed from `Web.config`,
and the project restores/builds successfully while still targeting net48.

### 02-dotnet10-aspnetcore-migration: Upgrade to net10.0 and migrate ASP.NET MVC (System.Web) to ASP.NET Core

Retarget ContosoUniversity to `net10.0` and migrate its ASP.NET MVC 5 / System.Web stack to
ASP.NET Core MVC, in place (single project, no side-by-side host). The assessment flags:
41 package issues (many packages — `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`,
`Microsoft.AspNet.WebPages`, `Microsoft.Web.Infrastructure`, `NETStandard.Library`,
`System.Buffers`, `System.ComponentModel.Annotations`, `System.Memory`,
`System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe`,
`System.Threading.Tasks.Extensions`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`
— functionality now included in the framework/SDK, remove the references); one
**incompatible** package (`Microsoft.AspNet.Web.Optimization` — `System.Web.Optimization`
bundling/minification has no .NET Core equivalent and must be replaced with direct
`<link>`/`<script>` tags to static content, per Feature.0001); a **security vulnerability**
in `Microsoft.Data.SqlClient` 2.1.4 (upgrade to 7.1.1, per NuGet.0004); EF Core packages
pinned at 3.1.32 (upgrade to the 10.0.x line); and `Global.asax.cs` application-startup
logic that must move to an ASP.NET Core `Program.cs`/hosting model (Feature.1000).

Research starting points for the executor: inventory all `Controllers/*.cs` (6 controllers:
Base, Courses, Departments, Home, Instructors, Notifications, Students) and their action
signatures/attributes for MVC5 → ASP.NET Core MVC API differences; inventory `Views/` for
Razor syntax/`@Html` helper differences; check `App_Start/` (`RouteConfig`, `BundleConfig`,
`FilterConfig`, etc.) for System.Web.Routing/Optimization usage to port to ASP.NET Core
endpoint routing and static files; check `Web.config` for `<appSettings>`/
`<connectionStrings>`/custom sections to port to `appsettings.json` +
`Microsoft.Extensions.Configuration`; check `Global.asax.cs` for `Application_Start`/session
and error-handling hooks; check `Services/` and `Data/` (EF Core `DbContext`) for any direct
`HttpContext.Current`, `System.Web.Caching`, or `System.Web.SessionState` usage that needs
direct ASP.NET Core equivalents (`IHttpContextAccessor`, `IMemoryCache`, `ISession`) per the
confirmed **Direct Migration to ASP.NET Core APIs** option (no System.Web Adapters shim).
`Services/` includes a notification system (see `NOTIFICATION_SYSTEM_README.md`) — confirm
whether it depends on System.Web (e.g. `HttpContext`, background timers tied to IIS) and
port accordingly. `Uploads/` and `TEACHING_MATERIAL_UPLOAD.md` indicate file-upload handling
that must move from `HttpPostedFileBase`/`Server.MapPath` to `IFormFile`/`IWebHostEnvironment`.

Include a full solution build and run of any existing automated test suite in this task's
Done-when criteria — there is only one task after SDK-style conversion, so this is the
final implementation task.

**Done when**: ContosoUniversity.csproj targets `net10.0`; all MVC5/System.Web package
references are removed or replaced with ASP.NET Core equivalents; `Microsoft.Data.SqlClient`
is upgraded to a non-vulnerable version; EF Core packages are upgraded to the 10.0.x line;
routing, bundling, configuration, and application-startup logic are ported to ASP.NET Core
equivalents (`Program.cs`, endpoint routing, static files, `appsettings.json`); the solution
builds with 0 errors and 0 warnings; and any existing automated tests pass.

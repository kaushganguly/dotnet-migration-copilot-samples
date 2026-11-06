# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single .NET Framework project — no dependency graph to manage |
| Project Approach | In-place rewrite | ≤10 controllers (6), minimal middleware, low complexity assessment |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (WebGrease, System.Web.Optimization-family) — small enough to resolve directly |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small web project, in-place approach confirmed, low `HttpContext`/session usage |
| Assembly Binding Redirects | Remove | Redirects are standard Visual Studio auto-generated boilerplate; no real version conflicts |
| Nullable Reference Types | Leave Disabled | Keep scope focused on the framework migration; enable separately later |

### Selected Strategy
**All-At-Once** — All project changes (SDK-style conversion, retargeting, MVC→Core migration, package updates) applied together in a single atomic pass.
**Rationale**: 1 project, single .NET Framework project with no dependents — per strategy default logic this maps to All-at-Once with a merged single task (small-solution adaptation rule).

## Project List

- **ContosoUniversity.csproj** — ASP.NET MVC 5 web application (net48, non-SDK-style, `packages.config`) → ASP.NET Core 10 MVC (SDK-style, `net10.0`, PackageReference)

## Tasks

### 01-upgrade-contosouniversity-to-net10: Migrate ContosoUniversity from ASP.NET MVC 5 / .NET Framework 4.8 to ASP.NET Core MVC / .NET 10.0

ContosoUniversity.csproj is the only project in the solution: a non-SDK-style ASP.NET MVC 5 web application targeting net48, using `packages.config` for 45 NuGet packages (26 need upgrade per assessment, 2 incompatible — WebGrease and the System.Web.Optimization bundling stack, which have no ASP.NET Core equivalent and must be replaced by direct static-asset references). It already uses EF Core 3.1 (not EF6) for data access via `SchoolContext`/`SchoolContextFactory`, so the data layer upgrade is a package-version bump (EF Core 3.1 → EF Core 10) rather than an EF6→EF Core rewrite. The app has no authentication, no session usage, and no third-party DI container — services are instantiated manually (`SchoolContextFactory.Create()`, `new NotificationService()`). It uses `System.Messaging` (MSMQ) in `Services/NotificationService.cs` for an admin notification queue, which has no direct .NET Core equivalent and must move to the `MSMQ.Messaging` NuGet package per the `migrating-to-msmq-messaging` skill, with queue path configuration moved from `Web.config`/`ConfigurationManager` to `appsettings.json`/`IOptions`.

This task covers the full in-place migration in one pass: convert the project to SDK-style targeting `net10.0`; replace `packages.config` with `PackageReference` and upgrade all compatible packages (EF Core 3.1→10, Microsoft.Data.SqlClient, Newtonsoft.Json already current, etc.); remove Antlr/WebGrease/System.Web.Optimization/System.Web.Mvc/System.Web.* references; rewrite `Global.asax.cs` startup logic (`AreaRegistration`, `RouteConfig`, `FilterConfig`, `BundleConfig`, EF Core database initialization) into `Program.cs` using the ASP.NET Core minimal hosting model; migrate `Web.config` `<appSettings>`/`<connectionStrings>` to `appsettings.json` bound via `IConfiguration`/`IOptions`; migrate the 6 controllers (`HomeController`, `StudentsController`, `CoursesController`, `InstructorsController`, `DepartmentsController`, `NotificationsController`, plus `BaseController`) from `System.Web.Mvc.Controller`/`ActionResult` to `Microsoft.AspNetCore.Mvc.Controller`/`IActionResult`, replacing `HttpStatusCodeResult`/`HttpNotFound()` with `StatusCode()`/`NotFound()`; migrate the 27 Razor views (`@Html.ActionLink`, `@Html.BeginForm`, `@Html.*For` helpers, `@Scripts.Render`/`@Styles.Render` bundling) to TagHelpers and static `wwwroot` asset references with `_ViewImports.cshtml`; and register DI services (`SchoolContext`, `NotificationService`) in `Program.cs`.

Research starting points for the executor: `Services/NotificationService.cs` (MSMQ), `Data/SchoolContext.cs` + `Data/SchoolContextFactory.cs` + `Data/DbInitializer.cs` (EF Core wiring), `Global.asax.cs` + `App_Start/*.cs` (startup/routing/bundling), `Controllers/BaseController.cs` (shared notification-send logic used by all controllers), `Views/Shared/_Layout.cshtml` (bundle references), `packages.config` (package inventory). No existing test project exists in the solution — per task scope `generateNewUnitTests=false`, no new test project is authored; `passUnitTests=true` is satisfied vacuously (no tests to fail).

#skill:migrating-aspnet-framework-to-core
#skill:migrating-mvc-controllers
#skill:migrating-mvc-razor-views
#skill:migrating-to-msmq-messaging
#skill:migrating-mvc-routing

**Done when**: Solution builds with 0 errors and 0 warnings (`dotnet build`) targeting `net10.0`; project is SDK-style with `PackageReference` only (no `packages.config`); no `System.Web*` references remain anywhere in code or project file; all 6 controllers compile against `Microsoft.AspNetCore.Mvc`; all views render-compile (Razor compilation succeeds as part of build); app starts (`dotnet run`) and serves the home page; `NotificationService` compiles against `MSMQ.Messaging`; `Web.config` settings are available via `IConfiguration`.

## Environment Notes

- This Linux sandbox has no VS Web Application build tooling (`Microsoft.WebApplication.targets`) and no full .NET Framework reference assemblies, so the legacy project cannot be MSBuild-evaluated directly by MCP tools here. A local, temporary patch (commenting the `Microsoft.WebApplication.targets` import) was applied only to let `generate_dotnet_upgrade_assessment` load the project, then reverted immediately — it does not affect the real migration, which replaces the csproj entirely with an SDK-style ASP.NET Core project that builds natively everywhere, including this sandbox.
- `MSMQ.Messaging` targets `netstandard2.0` and depends on `System.DirectoryServices`, which compiles on Linux but throws `PlatformNotSupportedException` at runtime on non-Windows hosts (MSMQ itself is Windows-only). This does not block `passBuild=true` on this sandbox; it is a pre-existing Windows-only runtime dependency of the sample's notification feature, inherited unchanged from the original app.

# Upgrade Plan — ContosoUniversity to .NET 10.0

## Selected Strategy
**All-At-Once** — 1 project in the solution, no dependency graph to manage. Upgrade
SDK-style conversion, target framework, and the ASP.NET MVC → ASP.NET Core MVC
rewrite in a single pass.

## Upgrade Options

### Strategy
- Upgrade Strategy: **All-at-Once**

### Project Structure
- Project Approach (Web Projects): **In-place rewrite**

### Compatibility
- Unsupported Packages: **Resolve Inline**
- System.Web Adapters: **Direct Migration to ASP.NET Core APIs**

### Modernization
- Assembly Binding Redirects: **Remove**
- Nullable Reference Types: **Enable**

### Reliability
- Test Coverage: **N/A** (not recommended by assessment; no existing test project)

## Projects

- `ContosoUniversity.csproj` — ASP.NET MVC 5 web application, old-style csproj,
  `packages.config`, targets `net48`. 7 controllers (Base, Courses, Departments,
  Home, Instructors, Notifications, Students), EF Core 3.1.32 data access
  (`Data/SchoolContext.cs`), 2 custom services (Logging, Notification), OWIN-free
  (no `Startup.cs`), no third-party DI container, no third-party logging framework,
  no ASP.NET Identity / Forms auth / cookie middleware detected.

## Tasks

### 01-sdk-style-conversion: Convert ContosoUniversity.csproj to SDK-style format

ContosoUniversity.csproj is currently an old-style (`ToolsVersion="15.0"`) web
application project using `packages.config` for its 45 NuGet package references,
targeting `net48`. This task converts it to the modern SDK-style project format
on its **current** target framework (net48) — a structural change only, kept
separate from the TFM upgrade so the two different failure modes (project
structure vs. API surface) are not conflated. `packages.config` entries migrate
to `PackageReference` items as part of this conversion (no separate task for
that). Confirm the .NET SDK and any `global.json` constraints are compatible
with the toolchain used for conversion before starting.

Research starting points: review the full dependency list before conversion
(45 packages, 26 need upgrading per the assessment, 2 are incompatible with any
.NET 10 TFM — `Antlr` and `Microsoft.AspNet.Web.Optimization`); review
`ContosoUniversity.csproj`'s custom MSBuild properties (`MvcBuildViews`,
`IISExpress*` settings, Web Deploy / publish profile references) that have no
SDK-style equivalent and should be dropped rather than carried forward.

**Done when**: `ContosoUniversity.csproj` is SDK-style (`<Project Sdk="...">`),
still targets `net48`, uses `PackageReference` for all retained dependencies
(`packages.config` removed), and the project still restores/builds on `net48`
before the TFM/framework migration begins.

### 02-upgrade-to-net10-aspnetcore-mvc: Migrate ContosoUniversity to .NET 10.0 and ASP.NET Core MVC

Retarget the SDK-style `ContosoUniversity.csproj` to `net10.0` and rewrite the
ASP.NET MVC 5 (`System.Web`) web application as an ASP.NET Core MVC application
in place (confirmed Project Approach: In-place rewrite — small project, ≤10
controllers, team accepts a single-pass cutover). This is the core task and
covers all of: TFM change, package updates, removing/replacing incompatible
packages, and the full web-stack rewrite (hosting model, routing, bundling,
configuration, DI, controllers/views).

Key assessment signals: 65 total issues (30 mandatory), 26 of 45 packages need
upgrading, 2 packages (`Antlr`, `Microsoft.AspNet.Web.Optimization`) have no
compatible version for `net10.0` (Resolve Inline — `Antlr` appears to be an
unused/transitive leftover, confirm no code references `Antlr.Runtime` before
removing; `Microsoft.AspNet.Web.Optimization` bundling/minification
(`App_Start/BundleConfig.cs`, `@Scripts.Render`/`@Styles.Render` in views) has
no .NET Core equivalent package and must be replaced with direct
`<script>`/`<link>` tags or ASP.NET Core static file + bundling tooling), and
12 binding-redirect entries in `Web.config` (all standard NuGet/VS boilerplate
— remove, not needed in SDK-style/.NET Core). Nullable reference types should
be enabled on the converted project (small, low-complexity codebase).

Scope includes:
- **Hosting/startup**: Replace `Global.asax.cs` (`MvcApplication.Application_Start`,
  manual EF Core `SchoolContext` initialization via `DbInitializer.Initialize`)
  and `App_Start/{RouteConfig,FilterConfig,BundleConfig}.cs` with an ASP.NET
  Core `Program.cs` — minimal hosting, `AddControllersWithViews()`, DI
  registration of `SchoolContext` (`AddDbContext`, reading `DefaultConnection`
  from configuration), MVC conventional routing, and startup DB
  initialization/seeding equivalent to `DbInitializer`.
- **Controllers** (7 total — `BaseController`, `CoursesController`,
  `DepartmentsController`, `HomeController`, `InstructorsController`,
  `NotificationsController`, `StudentsController`): migrate from
  `System.Web.Mvc.Controller` to `Microsoft.AspNetCore.Mvc.Controller`;
  each controller is its own migration unit (>5 controllers — MUST break out
  individually at execution time); use `get_code_dependencies` per controller
  before changing it.
- **Views/Razor**: migrate `.cshtml` views (`@Html.*`, `@Scripts.Render`,
  `@Styles.Render`, `_ViewStart`/`_Layout`) to ASP.NET Core Razor equivalents;
  replace Web Optimization bundle references with static asset tags.
- **Static assets**: move `Content/`/`Scripts/` assets into `wwwroot/` and
  configure the static files middleware.
- **Data access**: `Data/SchoolContext.cs` and EF Core packages already use EF
  Core 3.1.32 (not EF6) — upgrade EF Core packages to the version supported by
  `net10.0`; port `DbInitializer`/seeding logic to run via the new hosting
  model instead of `Global.asax`.
- **Services**: `Services/LoggingService.cs` and `Services/NotificationService.cs`
  — review for `System.Web`/`HttpContext.Current` dependencies (none detected
  in current codebase) and register via built-in DI.
- **Configuration**: move `Web.config` `<appSettings>` / `<connectionStrings>`
  (`DefaultConnection`, notification queue settings) to `appsettings.json` /
  `IConfiguration`.
- **Uploads/NOTIFICATION_SYSTEM_README-documented features**: confirm any
  file-upload (`Uploads/` folder, `TEACHING_MATERIAL_UPLOAD.md`) and
  notification functionality continue to work under the new hosting model.
- Remove the 12 `<assemblyBinding>` entries from `Web.config`/replace
  `Web.config` with `appsettings.json` (SDK-style/PackageReference projects do
  not need hand-authored binding redirects).
- Enable `<Nullable>enable</Nullable>` on the project and address resulting
  warnings (fix, don't suppress).

**Done when**: `ContosoUniversity.csproj` targets `net10.0`, the application
runs on the ASP.NET Core hosting model (no `System.Web`/`Global.asax`/`Web.config`
web-stack remnants), all 7 controllers and their views are migrated and
functionally equivalent, `Antlr` and `Microsoft.AspNet.Web.Optimization` are
removed/replaced, all other packages are updated to `net10.0`-compatible
versions, binding redirects are removed, nullable reference types are enabled,
and the project builds with 0 errors and 0 warnings.

### 03-final-validation: Build, test, and finalize the .NET 10.0 upgrade

Full-solution validation after the atomic upgrade. Build the solution targeting
`net10.0` and fix any remaining compilation errors in a single bounded pass (not
an open-ended retry loop). Since no test project exists in the solution today,
"unit tests" success is scoped to: the project builds cleanly, and a smoke-level
functional check of the app's startup/DB-initialization path succeeds. Document
any deferred recommendations (e.g., further UI/manual verification of migrated
views) for the user as a post-upgrade follow-up — do not treat them as blocking.

**Done when**: `dotnet build` succeeds for `ContosoUniversity.csproj` on
`net10.0` with 0 errors and 0 warnings, and the application starts up
successfully (DB initialization path exercised) confirming the migration is
functionally complete.

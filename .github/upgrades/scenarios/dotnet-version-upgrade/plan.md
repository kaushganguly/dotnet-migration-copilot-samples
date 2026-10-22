# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 -> .NET 10.0

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph to manage |
| Project Approach (Web Projects) | In-place rewrite | 7 controllers (<=10), low complexity assessment (🟢 Low), no live-traffic constraint |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Antlr, Microsoft.AspNet.Web.Optimization) - small enough to research inline |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small web project, in-place approach confirmed, 0 occurrences of HttpContext.Current |
| Assembly Binding Redirects | Remove Binding Redirects | Redirects are standard NuGet-generated boilerplate; .NET Core does not need them |
| Test Coverage | Skip | No existing test project in the solution; success criteria explicitly set generateNewUnitTests=false |

### Selected Strategy
**All-At-Once** — All work for the single project (ContosoUniversity.csproj) happens in one pass.
**Rationale**: 1 project, .NET Framework 4.8 -> net10.0, no dependency graph to manage.

## Project List

- **ContosoUniversity.csproj** — ASP.NET MVC 5 (System.Web) web application, old-style csproj with packages.config (45 packages), EF Core 3.1.32 data access, currently net48 -> net10.0.

## Tasks

### 01-prerequisites-sdk-conversion: Verify toolchain and convert project to SDK-style format

Verify the .NET 10 SDK is installed and available, and that no `global.json` pins an
incompatible SDK version. Convert `ContosoUniversity.csproj` from the legacy
(non-SDK-style, `ToolsVersion`-based) project format to SDK-style, on its current
target framework (net48) — this is a structural change only, not a TFM change.
Migrate `packages.config` (45 packages) to `PackageReference` as part of this
conversion (handled by the conversion tooling, not a separate task).

The project currently imports `Microsoft.WebApplication.targets` (VS Web
Application project type) and has `ProjectTypeGuids` for a Web Application
project — removing these is part of SDK-style conversion since the web
hosting model itself (System.Web -> ASP.NET Core) is addressed in the next
task. Research starting points: inspect `ContosoUniversity.csproj` for
`ItemGroup`s that need SDK-style equivalents (Content, Compile excludes,
App_Start, Views as content, Web.config transforms), and confirm the
solution file references stay valid after conversion.

**Done when**: `ContosoUniversity.csproj` is SDK-style, `packages.config` is
removed (packages expressed as `PackageReference`), and the project still
describes itself as targeting net48 (TFM change happens in the next task).

### 02-upgrade-to-net10: Upgrade TFM, packages, and migrate hosting from System.Web/MVC 5 to ASP.NET Core

Retarget `ContosoUniversity.csproj` to `net10.0` and update all NuGet package
references to versions compatible with net10.0 (assessment: 45 packages total,
24 recommended for upgrade, 2 incompatible with no SDK-style target framework
equivalent — Antlr and Microsoft.AspNet.Web.Optimization — resolve these inline
per the Unsupported Packages option; several others, e.g.
Microsoft.AspNet.Mvc/Razor/WebPages/Web.Infrastructure/CodeDom.Providers, are
framework-reference-only on .NET Core and should simply be removed).
Migrate the ASP.NET MVC 5 (System.Web) hosting model to ASP.NET Core hosting
in-place: replace `Global.asax`/`Global.asax.cs` application startup with a
`Program.cs`/minimal hosting model, convert `App_Start` (RouteConfig,
FilterConfig, BundleConfig) to ASP.NET Core endpoint routing and
middleware, replace `System.Web.Optimization` bundling/minification
(Feature.0001) with direct `<link>`/`<script>` tags against wwwroot content,
and migrate the 7 MVC controllers (Base, Courses, Departments, Home,
Instructors, Notifications, Students) and their Razor views to ASP.NET Core
MVC equivalents. Upgrade Entity Framework Core from 3.1.32 to the net10.0-
compatible EF Core version across all EF Core packages (EntityFrameworkCore,
.Abstractions, .Analyzers, .Relational, .SqlServer, .Tools), upgrade
Microsoft.Data.SqlClient from 2.1.4 to a version without the known security
vulnerability (NuGet.0004), and upgrade all Microsoft.Extensions.* packages
from 3.1.32 to the net10.0-aligned versions. Migrate `Web.config`
configuration (connection strings, appSettings incl. `NotificationQueuePath`
for the notification system) to `appsettings.json` + the ASP.NET Core
configuration system. Remove the 12 assembly binding redirects found in
`Web.config` — not needed under modern .NET assembly resolution. Resolve
resulting API-compatibility issues (65 total issues flagged by the
assessment across Binding, Feature, NuGet, and Project rule categories).
Research starting points: `App_Start/RouteConfig.cs`,
`App_Start/BundleConfig.cs`, `App_Start/FilterConfig.cs`, `Global.asax.cs`,
the `Services/` folder (notification system using
`System.Messaging`/MSMQ-style queue path — verify compatibility on
net10.0), and `Data/` (EF Core `DbContext` and migrations).

**Done when**: `ContosoUniversity.csproj` targets `net10.0`, all package
references resolve for net10.0 with no incompatible/vulnerable/deprecated
packages remaining unaddressed, the app starts via ASP.NET Core hosting
(no `System.Web` dependency), all controllers/views/services compile and
function equivalently, and `dotnet build` succeeds solution-wide with zero
errors and zero warnings in the project.

### 03-final-validation: Build, test, and document the completed upgrade

Run a full solution build and confirm zero errors and zero warnings. There is
no existing unit test project in this solution, so there is nothing to
execute for `passUnitTests` — confirm this via solution/project inventory
rather than assuming. Perform a security compliance pass: confirm no
remaining package in the dependency tree carries a known vulnerability
(particularly the Microsoft.Data.SqlClient CVE flagged in the assessment)
and no deprecated package (Microsoft.Identity.Client) remains unresolved.
Document any deferred recommendations (if any) for follow-up.

**Done when**: `dotnet build` (solution-wide) succeeds with 0 errors / 0
warnings, no known-vulnerable or deprecated package remains referenced, and
a short completion summary is recorded in `progress-details.md` for this
task.

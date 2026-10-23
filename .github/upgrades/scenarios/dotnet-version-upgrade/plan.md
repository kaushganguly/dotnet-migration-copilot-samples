# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 -> .NET 10.0

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph to manage |
| Project Approach (Web Projects) | In-place rewrite | 7 controllers (<=10), low complexity assessment (🟢 Low), explicit user requirement for full MVC-to-Core migration |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Antlr, Microsoft.AspNet.Web.Optimization) — both have known replacements, small enough to research inline |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small web project, in-place approach confirmed, explicit user requirement for full migration (no compatibility shim layer) |
| Assembly Binding Redirects | Document and Review Before Removing | 20+ redirects present, some span major version jumps (e.g. Newtonsoft.Json 0.0.0.0 -> 13.0.0.0) |
| Nullable Reference Types | Enable | Small codebase (~2,000 LOC), low complexity assessment |

### Selected Strategy
**All-At-Once** — All work for the single project (ContosoUniversity.csproj) happens in one pass.
**Rationale**: 1 project, .NET Framework 4.8 -> net10.0, no dependency graph to manage.

## Project List

- **ContosoUniversity.csproj** — ASP.NET MVC 5 (System.Web) web application, old-style csproj with packages.config (45 packages), EF Core 3.1.32 data access, MSMQ-based notification system (`System.Messaging`), currently net48 -> net10.0.

## Tasks

### 01-prerequisites-sdk-conversion: Verify toolchain and convert project to SDK-style format

Verify the .NET 10 SDK is installed and available (already confirmed compatible),
and that no `global.json` pins an incompatible SDK version (none present in repo).
Convert `ContosoUniversity.csproj` from the legacy (non-SDK-style,
`ToolsVersion`-based) project format to SDK-style, on its current target
framework (net48) — this is a structural change only, not a TFM change.
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
24+ recommended for upgrade, 2 incompatible with no SDK-style target framework
equivalent — Antlr and Microsoft.AspNet.Web.Optimization — resolve these inline
per the Unsupported Packages option: Antlr -> Antlr4, and
Microsoft.AspNet.Web.Optimization bundling/minification (Feature.0001) ->
direct `<link>`/`<script>` tags against wwwroot content; several others, e.g.
Microsoft.AspNet.Mvc/Razor/WebPages/Web.Infrastructure/CodeDom.Providers, are
framework-reference-only on .NET Core and should simply be removed).

Migrate the ASP.NET MVC 5 (System.Web) hosting model to ASP.NET Core hosting
in-place (Direct Migration to ASP.NET Core APIs — no System.Web Adapters shim):
replace `Global.asax`/`Global.asax.cs` application startup (Feature.1000) with a
`Program.cs`/minimal hosting model, convert `App_Start` (RouteConfig,
FilterConfig, BundleConfig) to ASP.NET Core endpoint routing and
middleware (`#skill:migrating-mvc-routing`), and migrate the 7 MVC controllers
(Base, Courses, Departments, Home, Instructors, Notifications, Students) and
their Razor views to ASP.NET Core MVC equivalents.

Migrate the MSMQ-based notification system (`Services/NotificationService.cs`,
`System.Messaging`) to a .NET-Core-compatible MSMQ client package
(`#skill:migrating-to-msmq-messaging`), including moving the queue path
configuration (`NotificationQueuePath` appSetting) to the ASP.NET Core
configuration system.

Upgrade Entity Framework Core from 3.1.32 to the net10.0-compatible EF Core
version across all EF Core packages (EntityFrameworkCore, .Abstractions,
.Analyzers, .Relational, .SqlServer, .Tools), upgrade Microsoft.Data.SqlClient
from 2.1.4 to a version without the known security vulnerability
(NuGet.0004, target ~7.1.1), replace the deprecated Microsoft.Identity.Client
package if it is actually used in code (else remove), and upgrade all
Microsoft.Extensions.* packages from 3.1.32 to the net10.0-aligned versions.

Migrate `Web.config` configuration (connection strings, appSettings incl.
`NotificationQueuePath` for the notification system) to `appsettings.json` +
the ASP.NET Core configuration system (`IConfiguration`). Review the 20+
assembly binding redirects found in `Web.config`, document their origin
(standard NuGet/VS-generated vs. the major-version-jump entries such as
Newtonsoft.Json and System.Web.Mvc), then remove them — not needed under
modern .NET assembly resolution. Enable `<Nullable>enable</Nullable>` on the
project (small, low-complexity codebase) and address resulting warnings.
Resolve resulting API-compatibility issues (65 total issues flagged by the
assessment across Binding, Feature, NuGet, and Project rule categories).

Research starting points: `App_Start/RouteConfig.cs`,
`App_Start/BundleConfig.cs`, `App_Start/FilterConfig.cs`, `Global.asax.cs`,
`Controllers/BaseController.cs` (notification send hook),
`Services/NotificationService.cs` + `Services/LoggingService.cs`, and
`Data/` (EF Core `DbContext` and migrations).

**Done when**: `ContosoUniversity.csproj` targets `net10.0`, all package
references resolve for net10.0 with no incompatible/vulnerable/deprecated
packages remaining unaddressed, the app starts via ASP.NET Core hosting
(no `System.Web` dependency), all controllers/views/services compile and
function equivalently, `Web.config` settings are available via
`IConfiguration`/`appsettings.json`, and `dotnet build` succeeds with zero
errors and zero warnings in the project.

### 03-final-validation: Build, test, and document the completed upgrade

Run a full solution build (`dotnet build`) and confirm zero errors and zero
warnings. There is no existing unit test project in this solution — confirm
this via solution/project inventory rather than assuming, so `passUnitTests`
has nothing to execute (vacuously satisfied). Perform a security compliance
pass: confirm no remaining package in the dependency tree carries a known
vulnerability (particularly the Microsoft.Data.SqlClient CVE flagged in the
assessment) and no deprecated package (Microsoft.Identity.Client) remains
unresolved. Document any deferred recommendations (if any) for follow-up.

**Done when**: `dotnet build` (solution-wide) succeeds with 0 errors / 0
warnings, no known-vulnerable or deprecated package remains referenced, and
a short completion summary is recorded in `progress-details.md` for this
task.


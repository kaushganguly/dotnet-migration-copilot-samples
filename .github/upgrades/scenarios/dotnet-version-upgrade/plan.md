# Upgrade Plan: ContosoUniversity to .NET 10.0

### Selected Strategy
**All-At-Once** - All projects (1 project: ContosoUniversity.csproj) upgraded simultaneously in a single operation.
**Rationale**: Single project, no dependency graph to manage. Project is a small ASP.NET MVC 5 (System.Web) web app (6 controllers, about 1979 LOC, assessed Low difficulty), so it is migrated in-place directly onto ASP.NET Core on .NET 10 rather than scaffolded side-by-side.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph |
| Project Approach | In-place rewrite | Small web surface (6 controllers), low complexity, no CI-green constraint |
| Unsupported Packages | Resolve Inline | 2 incompatible packages, both with known replacements |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | 0 HttpContext.Current usages, no HttpModule or HttpHandler, in-place approach |
| Assembly Binding Redirects | Remove Binding Redirects | Target net10.0 does not use assemblyBinding; all packages upgraded to compatible versions |
| Nullable Reference Types | Leave Disabled | No explicit team request; keep migration scope focused |

## Tasks

### 01-prerequisites-sdk-style-conversion: Verify toolchain and convert ContosoUniversity to SDK-style format

Verify the .NET 10 SDK is installed and compatible, then convert ContosoUniversity.csproj from the legacy non-SDK-style project format to SDK-style format, while staying on net48 for this task. The assessment shows the project uses an old ToolsVersion format, a packages.config file with 45 NuGet packages, and legacy MSBuild imports including Microsoft.WebApplication.targets and Microsoft.CSharp.targets. This is a structural change only, with no TFM change and no API surface changes, and it must stay a separate task from the framework migration so SDK-style conversion failures are not conflated with API breaking changes. Convert packages.config references to PackageReference as part of this conversion, and remove legacy project attributes and web project flavor GUIDs that no longer apply once the project is SDK-style. Preserve the project's content, view, and script file structure during this step, since file reorganization into wwwroot happens in the next task.

**Done when**: ContosoUniversity.csproj is SDK-style, still targets net48, uses PackageReference instead of packages.config, and the project loads and evaluates cleanly with dotnet build.

### 02-framework-to-core-migration: Retarget to net10.0 and migrate System.Web MVC to ASP.NET Core

Upgrade ContosoUniversity.csproj's TargetFramework to net10.0 and migrate the ASP.NET MVC 5 System.Web application to ASP.NET Core, following the migrating-aspnet-framework-to-core project-type skill in In-Place mode, since Project Approach is confirmed as In-place rewrite in scenario-instructions.md. This is the core task where API surface changes, package upgrades, and configuration migration all happen, after the SDK-style conversion completed in task one.

The project has 6 controllers including BaseController, CoursesController, DepartmentsController, HomeController, InstructorsController, NotificationsController, and StudentsController, Razor views, Global.asax and Global.asax.cs application startup code, and App_Start configuration classes for routing, bundling, and filters, all of which need to be inventoried during execution. NotificationsController returns JSON results consumed by the app's own Razor and JavaScript views as same origin AJAX calls rather than an externally published API contract, but the wire-contract compatibility check from the migrating-webapi-odata skill should still be run before changing it. About 26 NuGet packages need upgrading, with 2 incompatible packages needing special handling: Microsoft.AspNet.Web.Optimization needs its bundling and minification replaced with direct link and script tags or a modern bundler, and Microsoft.Data.SqlClient needs to be upgraded from 2.1.4 to 7.1.1 for net10.0 compatibility. Several Microsoft.AspNet and System packages are now included with the framework reference and should be removed rather than referenced directly, and the Microsoft.EntityFrameworkCore packages need to be upgraded from 3.1.32 to 10.0.13.

Services/NotificationService.cs uses System.Messaging for MSMQ access through a connection string read from Web.config appSettings, and since System.Messaging is not available on modern .NET, it needs to migrate to the MSMQ.Messaging package following the migrating-to-msmq-messaging skill, with the queue path configuration moved to appsettings.json and IConfiguration. Web.config appSettings and connectionStrings need to move to appsettings.json, the entire runtime assemblyBinding section with its binding redirects should be removed since it is not needed under net10.0, and the system.web and system.webServer sections should be replaced with their ASP.NET Core equivalents. Static assets in Content and Scripts need to move to wwwroot, and Scripts.Render and Styles.Render bundling calls need to be replaced with direct static file references plus app.UseStaticFiles(). No third-party dependency injection container, no OWIN or Katana middleware, no forms, windows, or membership authentication, no custom HttpModule or HttpHandler, and no HttpContext.Current usages were detected in the assessment or code scan, so dependency injection, middleware, and authentication migration are expected to be straightforward, but this should be verified during baseline capture rather than assumed. Leave Nullable disabled per the confirmed option.

**Done when**: ContosoUniversity.csproj targets net10.0, is a working ASP.NET Core MVC application using Program.cs and Microsoft.AspNetCore.Mvc, all controllers, views, routes, and static assets are migrated and reachable, Web.config content has been migrated to appsettings.json and IConfiguration with no ConfigurationManager references remaining, the MSMQ notification queue works through MSMQ.Messaging, no System.Web references remain in the project, and the solution builds successfully.

### 03-final-validation: Build, test, and document deferred items

Run a full solution build and fix all remaining compilation errors and warnings. Run the existing test suite if any test projects exist in the solution, confirming via project discovery, and fix any failing tests caused by the migration. Document deferred recommendations such as further UI or bundling modernization, nullable reference types adoption, or further EF Core provider tuning as notes rather than code changes.

**Done when**: The solution builds with 0 errors and 0 warnings in the projects touched by this upgrade, all tests pass or no test projects exist, and deferred recommendations are documented.

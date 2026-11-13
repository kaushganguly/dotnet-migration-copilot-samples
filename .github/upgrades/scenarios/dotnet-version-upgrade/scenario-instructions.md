# Scenario Instructions — dotnet-version-upgrade

## Goal

Upgrade ContosoUniversity from .NET Framework 4.8 (ASP.NET MVC 5 / System.Web) to .NET 10.0
(ASP.NET Core MVC), including SDK-style project conversion, NuGet package updates, configuration
migration (Web.config -> appsettings.json), and API compatibility fixes.

## Preferences

### Flow Mode

**Automatic** - do not pause for user review at any stage. Surface assessment/plan/progress
inline and keep moving. Only stop if genuinely blocked.

### Execution Style

- Fully autonomous, non-interactive execution (CI-style run). No user available to answer
  questions - apply best-practice defaults and proceed.
- `generateNewUnitTests = false` - do not author new test projects/tests.
- Success criteria: solution builds (`passBuild`), existing tests pass (`passUnitTests` - no
  test projects exist in the solution, so this is trivially satisfied), and no known-vulnerable
  packages remain (`securityComplianceCheck`).

## Upgrade Options (confirmed)

- **Target Framework**: net10.0 (LTS)
- **Project Approach**: In-place rewrite (single project in solution, no side-by-side host
  requested)
- **Package Management**: PackageReference (SDK-style), per-project (single project, Central
  Package Management not warranted)
- **Project Type**: ASP.NET Framework MVC 5 (System.Web) -> ASP.NET Core MVC
- **Test Coverage**: Skip (no existing test projects; `generateNewUnitTests=false` per task
  requirements)

## Decisions

- Environment is Linux (no Visual Studio / Microsoft.WebApplication.targets available). A
  minimal stub `Microsoft.WebApplication.targets` was placed at the path MSBuild resolves
  (`$(MSBuildExtensionsPath32)/Microsoft/VisualStudio/v18.0/WebApplications/` under the
  installed SDK) purely to unblock MCP tool MSBuild evaluation of the legacy project. This is a
  tooling workaround only - the real fix is converting the project to SDK-style and removing the
  import entirely, which happens as the very first execution step.
- `System.Messaging` (MSMQ) usage in `Services/NotificationService.cs` is migrated to the
  `MSMQ.Messaging` NuGet package (same API, .NET Core-compatible namespace) per the
  `migrating-to-msmq-messaging` skill. Runtime MSMQ behavior cannot be exercised on Linux CI;
  compilation/API-compatibility is the goal here.
- No authentication, session, HttpModules/HttpHandlers, OWIN, or custom DI container are present
  - default ASP.NET Core minimal hosting model is used with constructor DI for `SchoolContext`
  and `NotificationService` (previously `SchoolContextFactory.Create()` / `new
  NotificationService()` in `BaseController`).
- Routing: single default conventional route (`{controller}/{action}/{id}`) - migrated to
  `app.MapControllerRoute` in `Program.cs`.
- Bundling (`BundleConfig`/WebGrease) is dropped (no ASP.NET Core built-in equivalent); static
  assets (`Content/`, `Scripts/`) moved to `wwwroot/` and referenced directly via `<link>`/
  `<script>` tags.
- File upload (`CoursesController`) migrated from `HttpPostedFileBase` + `Server.MapPath` to
  `IFormFile` + injected `IWebHostEnvironment`.

# Upgrade Plan: ContosoUniversity → .NET 10.0

### Selected Strategy
**All-At-Once** — All work for this single-project solution is executed in one pass.
**Rationale**: 1 project total (`ContosoUniversity.csproj`), targeting .NET Framework 4.8. The
planning edge case for single-project solutions mandates All-at-Once regardless of Framework
status.

## Upgrade Options

| Option | Selected | Rationale |
|--------|----------|-----------|
| Upgrade Strategy | All-at-Once | Single project solution. |
| Project Approach | In-place rewrite | Small web project (6 controllers, ~2k LOC), no CI-green constraint. |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | No `HttpContext.Current` usage found; in-place approach confirmed. |
| Windows Native APIs | No Compatibility Pack | Single isolated MSMQ usage, migrated via dedicated skill rather than papered over. |
| Binding Redirects | Remove Binding Redirects | Standard NuGet-generated boilerplate; SDK-style auto-manages resolution. |
| Nullable Reference Types | Leave Disabled | Deliberate scope control — see scenario-instructions.md Decisions. |
| Package Management (CPM) | Not applicable | Single project. |
| Entity Framework (EF6→Core) | Not applicable | Already on EF Core 3.1. |
| Test Coverage | Skip | `generateNewUnitTests=false`; no existing test project. |

## Project List

| Project | Current TFM | Format | Type |
|---------|-------------|--------|------|
| ContosoUniversity (`ContosoUniversity.csproj`) | net48 | Legacy (non-SDK-style), `packages.config` | ASP.NET MVC 5 web application (`System.Web`) |

## Scope Summary

- ASP.NET MVC 5 web app (System.Web), 6 controllers (Base, Home, Students, Courses, Instructors,
  Departments, Notifications), ~25 Razor views, no authentication/authorization, no third-party DI
  container, no third-party logging framework.
- Data access already on EF Core 3.1 (`SchoolContext`) — not EF6/EDMX, simplifying migration.
- Notable risk: `Services/NotificationService.cs` uses `System.Messaging` (MSMQ), a Windows-only
  API with no direct .NET Core equivalent — requires the `migrating-to-msmq-messaging` skill to
  replace with the `MSMQ.Messaging` NuGet package.
- `packages.config` with 40+ legacy package entries (many transitive EF Core 3.1 dependency
  shims that become unnecessary once converted to SDK-style/PackageReference on net10.0).
- Bundling/minification via `System.Web.Optimization` (`BundleConfig.cs`) must be replaced
  (static files served from `wwwroot/`, bundling approach TBD at execution time — direct
  `<script>`/`<link>` references are sufficient given only 2 small bundles).
- No test project exists in the solution.

## Tasks

### 01-sdk-style-conversion: Convert ContosoUniversity project to SDK-style format

Convert `ContosoUniversity.csproj` from the legacy (non-SDK-style, `ToolsVersion="15.0"`) project
format to the modern SDK-style format, while remaining on the current target framework (net48) for
this task. Migrate `packages.config` to `<PackageReference>` items. Remove web-specific MSBuild
imports (`Microsoft.WebApplication.targets`) that only apply to the legacy format. This is a
structural change only — no TFM change, no ASP.NET Core migration yet. The project currently
targets `net48`, uses `packages.config` with ~40 package entries (many are legacy EF Core 3.1
transitive dependencies), and has `ProjectTypeGuids` marking it as an MVC web application project.

**Done when**: `ContosoUniversity.csproj` is SDK-style (`<Project Sdk="...">`), `packages.config`
is removed, all packages are `<PackageReference>` items, and the project still targets `net48`
(no TFM change in this task).

### 02-aspnetcore-migration: Migrate to ASP.NET Core MVC on .NET 10.0

Retarget `ContosoUniversity.csproj` to `net10.0` and migrate the ASP.NET MVC 5 (`System.Web`)
application to ASP.NET Core MVC, following the `migrating-aspnet-framework-to-core` orchestration
skill phase order (baseline capture → entry point/host → configuration → DI container →
controllers → middleware → views/static assets → cleanup). Scope includes:

- Replace `Global.asax`/`Global.asax.cs` with a minimal-hosting `Program.cs`.
- Migrate `Web.config` `<appSettings>`/`<connectionStrings>` to `appsettings.json`; replace all
  `ConfigurationManager` usage with `IConfiguration`.
- Register `SchoolContext` (EF Core 3.1 → updated EF Core version compatible with net10.0),
  `NotificationService`, and `LoggingService` via built-in DI in `Program.cs` (no third-party
  container in use).
- Migrate all 6 controllers (`BaseController`, `HomeController`, `StudentsController`,
  `CoursesController`, `InstructorsController`, `DepartmentsController`,
  `NotificationsController`) from `System.Web.Mvc` to `Microsoft.AspNetCore.Mvc`.
- Migrate `RouteConfig.cs` conventional routing to `MapControllerRoute` in `Program.cs`
  (consult `migrating-mvc-routing` skill).
- Migrate `FilterConfig.cs` (`HandleErrorAttribute`) to ASP.NET Core exception handling
  middleware.
- Migrate `Services/NotificationService.cs` off `System.Messaging` (MSMQ) — Windows-only API with
  no .NET Core equivalent — using the `migrating-to-msmq-messaging` skill (replace with
  `MSMQ.Messaging` NuGet package), preserving send/receive/mark-as-read behavior used by
  `NotificationsController` and `BaseController`.
- Migrate all ~25 Razor views: remove `@Scripts.Render`/`@Styles.Render` (replace with direct
  references or a lightweight bundling approach), convert `Html.ActionLink` and other
  `HtmlHelper` usage that doesn't carry over unchanged, move `Content/`/`Scripts/` static assets
  to `wwwroot/`, add `_ViewImports.cshtml` with Tag Helper imports, verify `_ViewStart.cshtml`.
- Update all NuGet packages to versions compatible with net10.0 (EF Core, Newtonsoft.Json,
  Microsoft.Data.SqlClient, etc.) — resolve any packages with no compatible version (e.g.
  `Antlr`, `WebGrease`, `Microsoft.AspNet.*` web optimization packages tied to System.Web) by
  removing them if their functionality is being replaced, or finding modern equivalents.
- Remove `AreaRegistration`/area-registration boilerplate if present (none detected currently).

**Done when**: Solution builds with 0 errors against `net10.0`, all endpoints/views function
equivalently to the pre-migration baseline (manual verification per route), no `System.Web`
reference remains anywhere in the project, and `Web.config`/`Global.asax`/`App_Start/*` legacy
files are removed.

### 03-final-validation: Build, verify, and clean up

Run a full solution build and fix all remaining compilation errors and warnings. Verify no
`System.Web`, `packages.config`, or other legacy-Framework-only references remain anywhere in the
project. Confirm the app starts (`dotnet run`) and key routes respond successfully (Home, Students,
Courses, Instructors, Departments, Notifications). Since the solution has no existing test
project, document that `passUnitTests` success criterion is trivially satisfied (no tests exist)
per the `generateNewUnitTests=false` constraint. Record any deferred/manual-review items (e.g.
notification queue behavior requiring a real MSMQ broker, which may not be available in all
deployment environments) in `progress-details.md`.

**Done when**: `dotnet build` succeeds solution-wide with 0 errors and 0 warnings, `dotnet run`
starts the app successfully, and all legacy artifacts (`Web.config`, `Global.asax(.cs)`,
`App_Start/`, `packages.config`) have been removed or confirmed unnecessary.

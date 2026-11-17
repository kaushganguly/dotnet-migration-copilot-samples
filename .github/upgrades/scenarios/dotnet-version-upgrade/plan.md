# Upgrade Plan — ContosoUniversity to .NET 10.0

## Selected Strategy
**All-At-Once** — All-at-once, single operation.
**Rationale**: Single project in the solution (ContosoUniversity.csproj), currently on .NET Framework 4.8, non-SDK-style. No dependency graph to manage — the whole project is upgraded and migrated to ASP.NET Core MVC in one coordinated effort.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph to manage |
| Project Approach | In-place rewrite | Small web project (6 controllers, ~2,000 LOC), low-complexity assessment, fully autonomous run accepts downtime |
| Unsupported Packages | Resolve Inline | Only 1 package (Microsoft.AspNet.Web.Optimization) has no known replacement package |
| System.Web Adapters | Direct Migration | No `HttpContext.Current` usage found; small in-place project |
| Assembly Binding Redirects | Document and Review Before Removing | 12 redirects present (>10 threshold); reviewed during SDK-style conversion, then removed |
| Nullable Reference Types | Leave Disabled | No explicit request to enable from day one; can be adopted as a follow-on effort |
| Entity Framework | N/A | Already on EF Core 3.1.32 — version bump only, no EF6→EF Core migration needed |
| Configuration Migration | N/A (auto) | Trivial config (5 appSettings keys, no custom section handlers) — auto-migrated as part of execution |
| Test Coverage | N/A | Not recommended by assessment; user explicitly set generateNewUnitTests=false |

## Projects

| Project | Current TFM | Target TFM | SDK-style | Project Kind |
|---------|------------|-----------|-----------|--------------|
| ContosoUniversity.csproj | net48 | net10.0 | No → Yes | ASP.NET MVC 5 web application (System.Web) |

## Tasks

### 01-sdk-style-conversion: Convert ContosoUniversity.csproj to SDK-style format

Convert the legacy non-SDK-style `ContosoUniversity.csproj` (ToolsVersion="15.0", `packages.config`, explicit `Reference` items, `Microsoft.WebApplication.targets` import) to the modern SDK-style project format, while remaining on `net48` for this task (structural change only — no TFM change, no ASP.NET Core migration yet). This keeps the structural change isolated from the upcoming API-surface change in task 02.

Scope: the single project in the solution. Assessment signals: `Project.0001` (project file needs SDK-style conversion), 45 NuGet packages currently referenced via `packages.config`/old-style `<Reference>` (migrate to `PackageReference`), and 12 binding-redirect issues (6 mandatory conflicts with auto-generated versions, 6 potential downgrade issues) recorded in `web.config`'s `<runtime><assemblyBinding>` section — per the confirmed "Document and Review Before Removing" option, review these before removal: confirm each redirect is superseded by the PackageReference-resolved version and has no residual multi-version conflict, then remove the manual `<assemblyBinding>` entries (SDK-style/PackageReference-based projects manage assembly binding automatically; ASP.NET Core does not use binding redirects at all).

Research starting points: use the `convert_project_to_sdk_style` tool; inspect `packages.config` and the `<ItemGroup>` `<Reference>` entries in `ContosoUniversity.csproj`; inspect `web.config`'s binding redirect section before removal; confirm the project still builds and runs as a classic ASP.NET Framework / System.Web project immediately after conversion (no ASP.NET Core changes yet).

**Done when**: `ContosoUniversity.csproj` is SDK-style (`Microsoft.NET.Sdk.Web` or equivalent interim SDK), still targets `net48`, `packages.config` is removed in favor of `PackageReference`, binding redirects have been reviewed and removed from `web.config`, and the project builds successfully on `net48` with 0 errors and 0 warnings.

### 02-aspnetcore-migration: Migrate ContosoUniversity to ASP.NET Core MVC on .NET 10.0

Migrate the SDK-style `ContosoUniversity` project from ASP.NET MVC 5 (`System.Web`) to ASP.NET Core MVC targeting `net10.0`. This is a full rewrite of the hosting model, not a TFM bump: `Global.asax`/`Global.asax.cs` application startup must become `Program.cs` / `Startup`-style composition; `System.Web.Routing` (`RouteConfig.cs`) must become ASP.NET Core endpoint routing (`MapControllerRoute`); `System.Web.Mvc` controllers/filters/`ActionResult` types must become `Microsoft.AspNetCore.Mvc` equivalents; `System.Web.Optimization` bundling (`BundleConfig.cs`, assessment `Feature.0001`, package `Microsoft.AspNet.Web.Optimization` — flagged incompatible, `NuGet.0001`) must be replaced with direct `<link>`/`<script>` tags or a modern bundler, since there is no supported ASP.NET Core equivalent package; and `web.config` (`<appSettings>`, `<connectionStrings>`) must be migrated to `appsettings.json` / `IConfiguration` (trivial config — 5 keys, no custom section handlers, auto-migration).

Scope: the single project, including its `Controllers/` (6 controllers: Base, Courses, Departments, Home, Instructors, Notifications, Students), `Views/`, `App_Start/` (`RouteConfig`, `BundleConfig`, `FilterConfig`), `Global.asax.cs`, `Data/` (EF Core `DbContext` — already EF Core 3.1.32, bump version only, no EF6 migration needed), `Services/` (including the MSMQ-based notification queue referenced by `NotificationQueuePath` in `web.config`), and `Models/`. Package updates: 26 of 45 packages need upgrading to .NET 10-compatible versions — notably `Microsoft.EntityFrameworkCore*` 3.1.32 → 10.0.15, `Microsoft.Extensions.*` 3.1.32 → 10.0.15, and the security-vulnerable `Microsoft.Data.SqlClient` 2.1.4 → 7.1.2 (`NuGet.0004`). `Antlr` 3.4.1.9004 has a known replacement (`Antlr4` 4.6.6) — apply it directly. `Microsoft.AspNet.Web.Optimization`, `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.Web.Infrastructure`, and other Framework-only packages whose functionality is now part of the ASP.NET Core shared framework should be removed entirely rather than upgraded. `Microsoft.Identity.Client` 4.21.1 is deprecated — evaluate whether it is still in use post-migration and update/remove accordingly.

Known risks: System.Web APIs have no direct ASP.NET Core equivalent in several places (bundling, `HttpContext.Current` is not used in code per assessment scan, so direct migration of any remaining `HttpContext` access is low-risk); the project uses a private MSMQ queue (`NotificationQueuePath`) via `System.Messaging`-style APIs that is not available in .NET 10 and needs a compatible replacement or cross-platform guard; client-side validation (`jQuery.Validation`, `Microsoft.jQuery.Unobtrusive.Validation`) needs to keep working with ASP.NET Core's unobtrusive validation scripts. This task should load `#skill:migrating-aspnet-framework-to-core` (orchestrates the phase sequence: project file → host → config → DI → controllers → middleware → auth → views → cleanup) plus satellite skills as needed during execution/breakdown: `#skill:migrating-mvc-routing`, `#skill:migrating-mvc-validation`, `#skill:migrating-mvc-model-binding`, `#skill:migrating-mvc-http-pipeline` (for `Global.asax` events), and `#skill:modernizing-csharp-version` if the C# language version increases with the TFM bump. Because no cookie authentication, Autofac/Unity/other third-party DI container, or third-party logging framework (log4net/NLog) was detected in the assessment, the dedicated auth/DI/logging satellite skills are not expected to be needed, but confirm this during research before skipping them.

This is the last implementation task for this upgrade: its Done-when criteria includes a full solution build and the existing test suite, so no separate validation task is added.

**Done when**: `ContosoUniversity.csproj` targets `net10.0` using `Microsoft.NET.Sdk.Web`; the app starts via `Program.cs` with no `Global.asax`/`System.Web` dependency remaining; all 6 controllers and their views render via ASP.NET Core MVC endpoint routing; `web.config` settings have been migrated to `appsettings.json`/`IConfiguration`; all 26 flagged packages are updated to .NET 10-compatible versions (including the `Microsoft.Data.SqlClient` security fix) and all Framework-only/superseded packages are removed; the whole solution builds with 0 errors and 0 warnings; and all existing unit tests pass.

# Upgrade Plan — ContosoUniversity to .NET 10.0

## Selected Strategy
**All-At-Once** — All work performed in a single atomic operation.
**Rationale**: 1 project in the solution (ContosoUniversity.csproj), clear dependency structure (no project references), single-project edge case applies regardless of source being .NET Framework.

## Upgrade Options

- **Upgrade Strategy**: All-at-Once — single project in solution.
- **Project Approach (Web Projects)**: In-place rewrite — small web surface (7 controllers, Low difficulty assessment), no `HttpContext.Current` usage found, no continuous-deployment requirement.
- **Unsupported Packages**: Resolve Inline — 2 incompatible packages (`Antlr` → `Antlr4`; `Microsoft.AspNet.Web.Optimization` → replace bundling with static `<link>`/`<script>` tags), both with known replacements.
- **System.Web Adapters**: Direct Migration to ASP.NET Core APIs — in-place approach confirmed, 0 occurrences of `HttpContext.Current`.
- **Assembly Binding Redirects**: Document and Review Before Removing (informational; no plan impact) — 12 redirects present, all become moot once `Web.config` is replaced.
- **Nullable Reference Types**: Enable — small codebase (Low difficulty, 1 project).
- Not applicable / skipped: Package Management/CPM (single project), Configuration Migration (trivial — 5 `appSettings` keys, standard connection strings), Windows Native APIs, Cross-App Cookie Authentication (no auth signals found), Logging Framework (none detected — custom `LoggingService` only), Dependency Injection container (none detected — already uses `Microsoft.Extensions.DependencyInjection`), Entity Framework option (already on EF Core 3.1.32, not EF6), Test Coverage (not recommended by assessment), Unsupported API Handling (0 breaking API issues found).

## Project List

Single group — all-at-once, no tiers/phases:

| Project | Current TFM | Format | Type |
|---------|-------------|--------|------|
| ContosoUniversity.csproj | net48 | Legacy (non-SDK) | ASP.NET MVC 5 web application (System.Web), EF Core 3.1.32 data access |

## Tasks

### 01-prerequisites: Verify toolchain and establish baseline

Confirm the .NET 10 SDK is installed and compatible, and capture a baseline understanding of the project before structural changes begin: current target framework (net48), project format (legacy/non-SDK `packages.config`-based), web stack (ASP.NET MVC 5 / System.Web, 7 controllers, Razor views, `BundleConfig`/`FilterConfig`/`RouteConfig` in `App_Start`), and data layer (already EF Core 3.1.32 — a significant head start since EF Core is cross-platform and mostly carries forward with a version bump). No `global.json` is present, so no SDK pinning conflict is expected. Note the 2 NuGet packages with no direct compatible net10.0 version (`Antlr` 3.4.1.9004, `Microsoft.AspNet.Web.Optimization` 1.1.3) and the known replacement path for each, for use in task 03.

**Done when**: .NET 10 SDK presence confirmed; no `global.json` conflicts; baseline inventory of project structure, web stack, and package risks recorded in this task's notes.

### 02-sdk-style-conversion: Convert ContosoUniversity.csproj to SDK-style format

Convert the legacy (non-SDK) `ContosoUniversity.csproj` to SDK-style project format while remaining on the current target framework (net48) — a structural change performed separately from the TFM/framework upgrade per .NET Framework migration planning rules (different failure modes: structural vs API-surface). This includes migrating `packages.config` package references to `PackageReference` as part of the conversion. The project currently has `packages.config` and classic MSBuild imports; convert using the standard SDK-style conversion tooling.

**Done when**: `ContosoUniversity.csproj` is SDK-style, still targets net48, builds without errors, and `packages.config` has been removed in favor of `PackageReference` entries.

### 03-aspnet-core-migration: Migrate to ASP.NET Core MVC on .NET 10.0

Retarget the now-SDK-style project to `net10.0` and migrate the web stack from ASP.NET MVC 5/System.Web to ASP.NET Core MVC, in-place (single pass, no side-by-side host, no `Microsoft.AspNetCore.SystemWebAdapters` shim layer — direct migration to native ASP.NET Core APIs per the confirmed Project Approach and System.Web Adapters options). Scope covers: `Global.asax`/`Global.asax.cs` → `Program.cs` minimal hosting + middleware pipeline; `App_Start/RouteConfig.cs` → ASP.NET Core endpoint routing; `App_Start/FilterConfig.cs` → ASP.NET Core MVC filter registration; `App_Start/BundleConfig.cs` and all `@Scripts.Render`/`@Styles.Render` calls in `Views/**/*.cshtml` → static `<script>`/`<link>` tags (bundling/minification via `System.Web.Optimization` is not supported on .NET Core, per assessment Feature.0001); `Web.config` → `appsettings.json` + `IConfiguration` (5 `appSettings` keys, standard `connectionStrings` — trivial auto-migration); all 7 controllers (`BaseController`, `CoursesController`, `DepartmentsController`, `HomeController`, `InstructorsController`, `NotificationsController`, `StudentsController`) and their Razor views migrated to ASP.NET Core MVC conventions; `Services/LoggingService.cs` and `Services/NotificationService.cs` reviewed for `System.Web`/HttpContext dependencies and adapted to ASP.NET Core DI if needed. Also in scope: bump all 26 packages flagged "upgrade recommended" in the assessment (including the EF Core family 3.1.32 → 10.0.15, and the security-flagged `Microsoft.Data.SqlClient` 2.1.4 → 7.1.2) to their target-net10.0-compatible versions; resolve inline the 2 incompatible packages (`Antlr` → `Antlr4`, `Microsoft.AspNet.Web.Optimization` → removed/replaced by static tags); drop packages whose functionality is now part of the framework reference (`Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.Web.Infrastructure`, `NETStandard.Library`, and the `System.*` BCL polyfill packages); remove all 12 assembly binding redirects (moot once `Web.config` / old-style binding model is gone); enable `<Nullable>enable</Nullable>` on the project. Use the `migrating-aspnet-framework-to-core` skill (In-Place mode) to drive phase ordering (project file → host → config → DI → controllers → middleware → views → cleanup) and dispatch its satellite skills as needed for bundling/views/routing. No cookie-based authentication, custom DI container, or third-party logging framework was detected, so no satellite skill is required for those areas.

**Done when**: Project targets `net10.0`, builds with 0 errors and 0 warnings; the application is a working ASP.NET Core MVC app (no `System.Web` references remain); all identified NuGet packages are upgraded/replaced/removed per the assessment; no assembly binding redirects remain; the whole solution builds successfully and any existing automated tests pass.

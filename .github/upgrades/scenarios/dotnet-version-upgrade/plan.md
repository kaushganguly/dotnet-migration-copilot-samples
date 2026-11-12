# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 to .NET 10.0

## Selected Strategy

### Selected Strategy
**All-At-Once** — Single project solution (ContosoUniversity.csproj only), upgraded in one atomic operation. .NET Framework solution-shape rule mandates All-at-Once for a single-project solution.
**Rationale**: 1 project, net48 -> net10.0, Low assessed difficulty, 0 binary/source-incompatible APIs. Small solution size (1-2 projects) means prerequisites + SDK-style conversion + TFM/framework migration are merged into a single task per the All-at-Once strategy's small-solution adaptation rule.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single-project .NET Framework solution |
| Project Approach (Web Projects) | In-place rewrite | 6 controllers, ~2000 LOC, Low complexity, no live-deployment constraint |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages, both with a known resolution path (Antlr -> Antlr4; Web.Optimization -> static files) |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | 0 `HttpContext.Current` usages found; small in-place web project |
| Assembly Binding Redirects | Document and Review Before Removing | 20 redirects present (> 10 threshold); several conflict with auto-generation (MSB3836) |
| Nullable Reference Types | Enable | Small codebase, 1 project, Low complexity |
| Test Coverage | Skip | Assessment recommends coverage for 0 projects; matches `generateNewUnitTests=false` |

## Projects

| Project | Current | Target | Kind |
|---------|---------|--------|------|
| ContosoUniversity.csproj | net48 (non-SDK-style, packages.config) | net10.0 (SDK-style, ASP.NET Core MVC) | Web (ASP.NET Framework MVC) |

## Tasks

### 01-contosouniversity-upgrade: Upgrade ContosoUniversity from ASP.NET Framework MVC (net48) to ASP.NET Core MVC (net10.0)

ContosoUniversity.csproj is the sole project in the solution: a non-SDK-style ASP.NET Framework MVC web application (net48, `packages.config`, `ProjectTypeGuids` web flavor, imports `Microsoft.WebApplication.targets`). It must be converted to SDK-style format, retargeted to `net10.0`, and migrated from `System.Web`/ASP.NET MVC 5 to ASP.NET Core MVC, in a single atomic pass (All-at-Once, merged with prerequisites and SDK-style conversion per the small-solution rule).

Key assessment signals: 45 NuGet packages (24 need upgrade, 2 incompatible — `Antlr` and `Microsoft.AspNet.Web.Optimization`), 1 security vulnerability (`Microsoft.Data.SqlClient` 2.1.4 → 7.1.1), 1 deprecated package (`Microsoft.Identity.Client`), 20 assembly binding redirects (several conflicting with auto-generation, MSB3836), `System.Web.Optimization` bundling in use (`BundleConfig.cs`) which has no .NET Core equivalent and must become static `<link>`/`<script>` tags, `Global.asax.cs` application startup logic that must move to `Program.cs`/minimal hosting, and `RouteConfig.cs` conventional MVC routes that must become `MapControllerRoute` endpoint routing. The project already uses EF Core 3.1.32 (not EF6) for data access via `SchoolContext`, so no EDMX/EF6-to-EF-Core migration is needed — only an EF Core package version bump. No third-party DI container, no third-party logging framework, no cookie/Forms authentication, and no custom/encrypted config sections were found, simplifying configuration migration to a straightforward Web.config → appsettings.json move (connection string, `NotificationQueuePath`, validation flags).

Research starting points for the executor: inventory `Global.asax.cs` events and `App_Start/*.cs` (RouteConfig, BundleConfig, FilterConfig), inventory all `Controllers/*.cs` (6 concrete controllers + `BaseController`) and their `View()`/`Content/Scripts` references, inventory `Views/*.cshtml` for `@Html.*`/`@Scripts.Render`/`@Styles.Render` usage tied to bundling, inventory `Services/` and `Data/` folders for any `System.Web` or `HttpContext` dependencies, confirm the `NotificationQueuePath` (MSMQ-backed `.\Private$\...`) feature's target API on .NET Core, and check `Uploads/TeachingMaterials` file-upload handling for `Server.MapPath` usage that needs `IWebHostEnvironment`.

**Done when**:
- `ContosoUniversity.csproj` is SDK-style, targets `net10.0`, `<Nullable>enable</Nullable>` is set, and `packages.config` is removed
- Project is an ASP.NET Core MVC app with `Program.cs` minimal-hosting entry point replacing `Global.asax.cs`
- Routing migrated to `MapControllerRoute` endpoint routing; areas (if any) preserved
- Bundling/minification (`System.Web.Optimization`) replaced with static asset references (`wwwroot`), `Antlr` replaced with `Antlr4` only if still required after the optimization removal (otherwise removed entirely)
- Web.config settings migrated to `appsettings.json` / ASP.NET Core configuration (connection string, queue path, validation flags); binding redirects documented then removed (not applicable to SDK-style `net10.0`)
- All NuGet packages updated to compatible/recommended versions, including the `Microsoft.Data.SqlClient` security fix (-> 7.1.1) and EF Core bump to 10.0.15-line (or latest compatible with net10.0)
- Deprecated `Microsoft.Identity.Client` reference reviewed and removed if unused (project has no visible auth code using it)
- Whole-project build succeeds with 0 errors and 0 warnings
- Any existing automated tests in the repository still pass (no new tests generated, per `generateNewUnitTests=false`)
- No remaining `System.Web` / ASP.NET MVC 5 / `HttpContext.Current` references anywhere in source

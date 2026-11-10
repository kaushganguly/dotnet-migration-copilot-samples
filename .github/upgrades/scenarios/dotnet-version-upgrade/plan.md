# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

## Selected Strategy
**All-At-Once** — Single project (ContosoUniversity), no dependency graph to manage. The
Framework-detection override confirms All-at-Once for single-project .NET Framework solutions.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project, no dependency graph to manage |
| Project Approach | In-place rewrite | Small web project (7 controllers, ~2k LOC), low complexity, user requested full migration |
| Unsupported Packages | Resolve Inline | Only 2 incompatible packages (Antlr, Microsoft.AspNet.Web.Optimization) |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small, in-place project, low `HttpContext.Current` usage |
| Assembly Binding Redirects | Document and Review Before Removing | 12 redirects present, above the 10-redirect review threshold |
| Nullable Reference Types | Enable Nullable Reference Types | Small, low-complexity codebase |
| Test Coverage | Skip | User requirement: generateNewUnitTests=false; existing tests must still pass |

## Projects

| Project | Current | Target | Type |
|---------|---------|--------|------|
| ContosoUniversity.csproj | net48, old-style, packages.config | net10.0, SDK-style, PackageReference | ASP.NET MVC 5 web app → ASP.NET Core MVC |

## Tasks

### 01-sdk-style-conversion: Convert ContosoUniversity to SDK-style project format

Convert `ContosoUniversity.csproj` from the legacy (non-SDK-style) project format to
modern SDK-style, while remaining on `net48` for this task (structural change only — no
TFM change yet). This includes migrating `packages.config` to `PackageReference` entries
and removing legacy MSBuild scaffolding (ToolsVersion, Web Application targets import,
AssemblyInfo duplication, etc.) that SDK-style projects no longer need. The assessment
flagged `Project.0001` (project file needs SDK-style conversion) and reports 45 NuGet
packages currently declared via `packages.config`.

Research starting points: inventory current `<ItemGroup>` content/compile includes that
SDK-style implicit globbing would otherwise duplicate (Views, Content, Scripts), and the
Web.config / Global.asax files that remain referenced by the legacy web project system
until the next task replaces them.

**Done when**: `ContosoUniversity.csproj` is SDK-style (`Sdk="Microsoft.NET.Sdk.Web"` or
equivalent), `packages.config` is removed, all packages are `PackageReference` entries,
project still targets `net48`, and the project continues to build on the current
framework (or is verified to no longer meaningfully build as a legacy web project once
SDK-style — in which case this is validated together with task 02's build, noted here for
traceability).

### 02-upgrade-to-net10-aspnetcore: Upgrade ContosoUniversity to net10.0 and migrate ASP.NET MVC to ASP.NET Core MVC

Retarget `ContosoUniversity.csproj` to `net10.0` and migrate the ASP.NET MVC 5
(`System.Web`) application to ASP.NET Core MVC, in-place (no side-by-side host). This is
the core, single-pass upgrade task for the only project in this solution.

Scope includes:
- **Package upgrades**: `Microsoft.EntityFrameworkCore*` 3.1.32 → 10.0.14 (all EF Core
  packages), `Microsoft.Data.SqlClient` 2.1.4 → 7.1.1 (addresses a known security
  vulnerability — `NuGet.0004`), `Newtonsoft.Json` 13.0.3 → 13.0.4,
  `Microsoft.Extensions.*` 3.1.32 → 10.0.14, `System.Collections.Immutable`,
  `System.Diagnostics.DiagnosticSource`, `System.Runtime.CompilerServices.Unsafe`,
  `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.HashCode` per assessment-recommended
  versions. Several packages (`Microsoft.AspNet.Mvc`, `.Razor`, `.WebPages`,
  `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.Web.Infrastructure`,
  `NETStandard.Library`, and BCL shim packages already covered by the framework) are
  removed entirely — their functionality is included with the net10.0 framework
  reference.
- **Incompatible packages (Resolve Inline)**: replace `Antlr` 3.4.1.9004 with
  `Antlr4` 4.6.6 if still needed by generated code (verify actual usage first — it may be
  an indirect MVC view-engine dependency no longer needed under Razor on ASP.NET Core);
  replace `Microsoft.AspNet.Web.Optimization` bundling/minification (`Feature.0001`) with
  static `<link>`/`<script>` tags or a modern bundler, removing `BundleConfig.cs` usage.
  `Microsoft.Identity.Client` 4.21.1 is deprecated — evaluate whether it is still used and
  upgrade to a supported `Microsoft.Identity.Client` version if so.
- **Host/startup migration**: convert `Global.asax.cs` application startup
  (`Feature.1000`) to ASP.NET Core's `Program.cs` minimal hosting model; replace
  `System.Web` HTTP pipeline concepts with ASP.NET Core middleware.
- **MVC migration**: convert controllers/views from `System.Web.Mvc` to
  `Microsoft.AspNetCore.Mvc` namespaces and conventions (routing, action results, model
  binding, validation, filters) — Direct Migration to ASP.NET Core APIs (no
  System.Web Adapters shim layer), per the confirmed Project Approach / System.Web
  Adapters options. 7 controllers in `Controllers/`.
- **Configuration migration**: replace `Web.config` (`<connectionStrings>`,
  `<appSettings>`) with `appsettings.json` + `IConfiguration`/options pattern. Config is
  low-complexity (5 appSettings keys, 1 connection string, no custom sections) — no
  special transform/encryption handling needed.
- **Binding redirects**: 12 binding redirect issues were flagged (`Binding.0006`,
  `Binding.0007` — manual redirects conflicting with/forcing a downgrade of
  auto-generated versions). Per the confirmed option (Document and Review), produce a
  short note in this task's progress record listing the prior redirects and confirming
  each is superseded by net10.0's own dependency resolution (SDK-style projects do not
  hand-author binding redirects) before removing `Web.config`'s `<runtime>` section.
- **Nullable reference types**: enable `<Nullable>enable</Nullable>` on the project and
  address resulting warnings (small codebase, ~2k LOC, low complexity).
- **Data access**: `Models/`, `Data/` (EF Core `DbContext`, migrations if present) should
  continue to work against EF Core 10 with minimal changes; verify `DbContext` options
  configuration moves from `Web.config` connection string to the new configuration model.

Research starting points: review `assessment/projects/ContosoUniversity.md` for the full
per-project package table; check `Global.asax.cs` for `Application_Start` logic that must
move to `Program.cs`; check `App_Start/` for `RouteConfig`, `BundleConfig`, `FilterConfig`
registration that need ASP.NET Core equivalents; confirm `Services/` folder (if present)
for any `System.Web`-coupled service implementations.

**Done when**: `ContosoUniversity.csproj` targets `net10.0`; project builds with 0 errors
and 0 warnings; all NuGet packages listed above are updated/replaced with no remaining
`NuGet.0001`/`NuGet.0004` incompatibilities or vulnerabilities; the application starts via
the ASP.NET Core hosting model (`Program.cs`) with no remaining `System.Web` references;
`Web.config` application settings/connection strings are migrated to
`appsettings.json`/configuration; binding redirect review note is recorded; nullable
reference types are enabled; existing test suite (if any) passes; full solution build is
green.

# Upgrade Plan — ContosoUniversity: .NET Framework 4.8 → .NET 10.0

### Selected Strategy
**All-At-Once** — All project changes (SDK-style conversion handled as a separate
prerequisite task; TFM + hosting-model upgrade as the core task) land as two
sequential tasks, not phased by tier.
**Rationale**: 1 project total (ContosoUniversity.csproj) — the Framework-migration
planning override mandates All-at-Once for single-project .NET Framework solutions.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project solution — Framework-migration override mandates All-at-Once for single-project solutions. |
| Project Approach (Web) | In-place rewrite | Small web project (7 controllers, ~1,979 LOC, 🟢 Low difficulty per assessment); no continuous-deployment constraint stated. |
| Unsupported Packages | Resolve Inline | Only 1 package with no direct replacement (Microsoft.AspNet.Web.Optimization) — small enough to resolve inline. |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | In-place approach confirmed; 0 occurrences of `HttpContext.Current`; small web project. |
| Assembly Binding Redirects | Document and Review Before Removing | 22 dependentAssembly entries in Web.config — volume warrants review before removal. |
| Nullable Reference Types | Leave Disabled | Default when applicable; not explicitly requested by the user. |
| Test Coverage | Skip | User-specified success criteria: generateNewUnitTests=false. |

## Project List

- **ContosoUniversity.csproj** — ASP.NET MVC 5 web application (System.Web), net48, non-SDK-style,
  `packages.config`. Only project in the solution.

## Tasks

### 01-sdk-style-conversion: Convert ContosoUniversity.csproj to SDK-style format

ContosoUniversity.csproj is a legacy (non-SDK-style) ASP.NET MVC 5 web application project using
`packages.config` for package management, `ProjectTypeGuids` marking it as a Web Application, and
an MSBuild import of `Microsoft.WebApplication.targets`. Convert it to SDK-style format while
remaining on the current target framework (net48) — this is a structural change only, performed
before any TFM or hosting-model change so that conversion failures and upgrade failures are never
conflated. Convert `packages.config` entries to `<PackageReference>` items as part of this
conversion. Remove the legacy web-application-specific MSBuild imports and `ProjectTypeGuids` that
no longer apply once SDK-style. The assessment counts 45 NuGet packages currently declared via
`packages.config`.

Research starting points: review the current `.csproj` for custom MSBuild targets
(`MvcBuildViews`, `CopySQLClientNativeBinaries`) that copy native SNI binaries for
`Microsoft.Data.SqlClient.SNI.runtime` — these exist to work around .NET Framework's lack of
automatic native asset deployment and should be reassessed once SDK-style package restore handles
native runtime assets automatically.

**Done when**: `ContosoUniversity.csproj` is SDK-style (`<Project Sdk="...">`), `packages.config`
is removed and all package references converted to `<PackageReference>`, the project still targets
`net48`, and the project continues to load/evaluate cleanly via `dotnet` tooling (no build required
yet — TFM change and package version bumps happen in the next task).

---

### 02-upgrade-to-net10: Upgrade ContosoUniversity to net10.0 and migrate to the ASP.NET Core hosting model

Retarget `ContosoUniversity.csproj` to `net10.0` and migrate the application in-place from the
System.Web-based ASP.NET MVC 5 hosting model (`Global.asax`, `HttpApplication`, `RouteConfig`,
`web.config`) to the ASP.NET Core hosting model (`Program.cs`, minimal hosting, endpoint routing,
`appsettings.json`). This is an in-place rewrite — no side-by-side scaffold, no
`Microsoft.AspNetCore.SystemWebAdapters` compatibility shims — since the project is small (7
controllers, ~1,979 LOC) with 0 `HttpContext.Current` usages found in assessment.

Scope covers:
- Update NuGet dependencies to net10-compatible versions: `Microsoft.EntityFrameworkCore*` family
  (3.1.32 → 10.0.15 per assessment), `Microsoft.Data.SqlClient` (2.1.4 → 7.1.2 per assessment —
  also flagged with a security vulnerability and as deprecated at the old version),
  `Microsoft.Identity.Client` (4.21.1, flagged deprecated — evaluate current supported replacement
  if still referenced after migration), and all `Microsoft.Extensions.*` packages (3.1.32 → 10.0.15
  per assessment: Caching, Configuration, DependencyInjection, Logging, Options, Primitives, etc.).
  Drop packages the assessment marks "functionality is included with framework reference"
  (`Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`,
  `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.Web.Infrastructure`,
  `NETStandard.Library`, `System.Buffers`, `System.ComponentModel.Annotations`, `System.Memory`,
  `System.Numerics.Vectors`, `System.Threading.Tasks.Extensions`) — these ship as part of the
  net10.0 framework reference and must not be kept as explicit package references.
- Replace `Microsoft.AspNet.Web.Optimization` bundling/minification (flagged incompatible and
  deprecated in assessment — Feature.0001) with static `<link>`/`<script>` tags or a modern
  bundling approach; remove `BundleConfig`/`BundleTable` usage.
- Convert `Global.asax.cs` application startup (`Application_Start`, `AreaRegistration`,
  `FilterConfig`, `RouteConfig`, `BundleConfig`, EF Core database initialization) into
  `Program.cs` minimal-hosting equivalents; remove `Global.asax`/`Global.asax.cs` once migrated
  (Feature.1000).
- Convert `RouteConfig`/`RouteTable.Routes` MVC routing to ASP.NET Core endpoint routing
  (`MapControllerRoute` in `Program.cs`).
- Migrate `Web.config` `<appSettings>` and `<connectionStrings>` (currently holding the
  `DefaultConnection` LocalDB connection string and a `NotificationQueuePath` setting for MSMQ)
  to `appsettings.json`, wiring `IConfiguration` and replacing any `ConfigurationManager` usage.
  `maxRequestLength`/`executionTimeout` `httpRuntime` settings and the `requestFiltering
  maxAllowedContentLength` setting need ASP.NET Core equivalents (`Kestrel`/`IIS` request size
  limits) if still required.
- Resolve the 6 mandatory + 6 potential binding-redirect findings in assessment by removing
  `<assemblyBinding>`/`<dependentAssembly>` entries entirely once the project is SDK-style and
  on net10.0 (.NET Core does not use assembly binding redirects) — review the list first per the
  confirmed "Document and Review" option, since 22 entries exist, then remove them as part of the
  `Web.config` → `appsettings.json` migration (`Web.config` itself is retired under ASP.NET Core).
- Controllers/Views: convert MVC controllers and Razor views to ASP.NET Core MVC equivalents;
  check for `HtmlHelper`/`UrlHelper` usages, child actions, and any Web Forms-era helpers that
  need ASP.NET Core Tag Helper or partial-view equivalents.
- Resolve any remaining API compatibility issues surfaced during the build (assessment reports 0
  pre-identified API issues, but build errors are expected once packages and TFM change — this is
  normal for a System.Web → ASP.NET Core in-place rewrite and is not a sign the assessment was
  wrong).
- Address the `Microsoft.Data.SqlClient.SNI.runtime` native-binary copy target
  (`CopySQLClientNativeBinaries`) from task 01 — current `Microsoft.Data.SqlClient` 7.x versions
  handle native SNI assets via NuGet runtime asset selection automatically on SDK-style/net10.0
  projects; remove the custom copy target once confirmed unnecessary.
- Review the Antlr/Antlr4 and other upgrade-recommended packages (`Newtonsoft.Json`,
  `System.Collections.Immutable`, `System.Diagnostics.DiagnosticSource`,
  `System.Runtime.CompilerServices.Unsafe`) called out in assessment and bump to the suggested
  compatible versions.

**Done when**: `ContosoUniversity.csproj` targets `net10.0`; the application boots on the ASP.NET
Core hosting model (`Program.cs`, no `Global.asax`/`System.Web` references remaining); all NuGet
packages listed above are updated to net10-compatible versions with no remaining incompatible or
"deprecated — included in framework" packages; `Web.config` is retired in favor of
`appsettings.json`; the solution builds with 0 errors and 0 warnings; any existing automated tests
pass (none currently exist in the solution, so this is trivially satisfied — record that
explicitly rather than silently skipping it).

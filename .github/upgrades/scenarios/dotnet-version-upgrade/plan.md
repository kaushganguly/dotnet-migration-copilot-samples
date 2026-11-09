# Upgrade Plan: ContosoUniversity .NET Framework 4.8 -> .NET 10.0

## Selected Strategy
**All-At-Once** - Single project (ContosoUniversity.csproj), no dependency graph to manage.
All work happens in one pass rather than tiered/phased rollout.

## Upgrade Options

| Option | Selected | Why |
|--------|----------|-----|
| Upgrade Strategy | All-at-Once | Single project total - no dependency graph to manage |
| Project Approach (Web Projects) | In-place rewrite | 7 controllers, ~2k LOC, low complexity assessment, no HttpContext.Current/HttpModule usage |
| Unsupported Packages | Resolve Inline | Only 1 package without a direct compatible version (Microsoft.AspNet.Web.Optimization), with a known replacement (static asset references) |
| System.Web Adapters | Direct Migration to ASP.NET Core APIs | Small web project, in-place approach, zero HttpContext.Current usage found |
| Assembly Binding Redirects | Remove Binding Redirects | Minor version mismatches only (no major version jumps); SDK-style/.NET 10 resolves assemblies without hand-authored redirects |
| Nullable Reference Types | Enable Nullable Reference Types | Small codebase (~2k LOC), single project, low complexity assessment |

## Projects

| Project | Current TFM | Target TFM | SDK-style | Kind |
|---------|-------------|-----------|-----------|------|
| ContosoUniversity.csproj | net48 | net10.0 | No -> Yes | ASP.NET MVC 5 web application (System.Web) |

## Tasks

### 01-sdk-style-conversion: Convert ContosoUniversity to SDK-style project format

Convert `ContosoUniversity.csproj` from the legacy (non-SDK) project format to modern
SDK-style format, while remaining on `net48` for this task. This is a structural change
only - it must not change the target framework or attempt ASP.NET Core API migration.
Convert `packages.config` (45 packages) to `<PackageReference>` items as part of this
conversion. Remove legacy project-type GUIDs / web-application-specific MSBuild
constructs that are superseded by SDK-style conventions where safe to do so without
altering behavior on net48. #skill:converting-to-sdk-style

**Known risks**: project uses `ProjectTypeGuids` identifying it as an ASP.NET MVC web
application; legacy web projects commonly depend on `Microsoft.WebApplication.targets`
import behavior that SDK-style conversion must reconcile. Validate the project still
builds on `net48` after conversion before moving to the next task.

**Done when**: `ContosoUniversity.csproj` is SDK-style, uses `<PackageReference>` for all
45 former `packages.config` entries, still targets `net48`, and the project builds
successfully with zero errors on the original target framework.

### 02-dotnet10-migration: Upgrade ContosoUniversity to .NET 10.0 (ASP.NET Core migration)

Retarget `ContosoUniversity.csproj` to `net10.0` and migrate the ASP.NET MVC 5 /
`System.Web` application to ASP.NET Core in place (single pass - Project Approach =
In-place rewrite, System.Web Adapters = Direct Migration, no compatibility shim layer).
This covers: project file TFM update, `Program.cs`/minimal hosting setup, configuration
migration (`Web.config` -> `appsettings.json`), routing (`RouteConfig.cs` ->
`MapControllerRoute`), the 7 MVC controllers and their Razor views, `Global.asax.cs`
startup logic (EF Core `DbContext` initialization, area registration, filters), removal
of `System.Web.Optimization` bundling in favor of direct static asset references
(Feature.0001), removal of now-conflicting assembly binding redirects, and enabling
`<Nullable>enable</Nullable>`. #skill:migrating-aspnet-framework-to-core

Package work: bump the 24 packages flagged "upgrade recommended" (including
`Microsoft.EntityFrameworkCore*` 3.1.32 -> 10.0.x, `Microsoft.Data.SqlClient` 2.1.4 -> 7.1.1
which also resolves a flagged CVE, `Newtonsoft.Json` 13.0.3 -> 13.0.4) to their latest
net10.0-compatible versions; drop the 19 packages whose functionality is now included in
the shared framework (`System.Buffers`, `System.Memory`, `NETStandard.Library`, etc.) and
the obsolete `Microsoft.AspNet.*` / `Microsoft.Web.Infrastructure` packages entirely as part
of the ASP.NET Core migration; resolve `Microsoft.AspNet.Web.Optimization` (the one
package assessment flagged as incompatible with no compatible version) per Feature.0001
(replace bundling with direct `<link>`/`<script>` tags to content files).

**Known risks**: `Services/NotificationService.cs` uses `System.Messaging` (MSMQ) directly -
this namespace does not exist on modern .NET the same way and is Windows-only; it must be
migrated to the `MSMQ.Messaging` NuGet package (or an equivalent cross-platform-safe
approach) as part of this task. #skill:migrating-to-msmq-messaging . The assessment's
automated API-compatibility scan reported 0 API issues and only 68 analyzed LOC against a
~2,000 LOC project, which is almost certainly an undercount caused by this environment
missing Visual Studio MSBuild web-application tooling during assessment (a stub
`Microsoft.WebApplication.targets` was required just to load the project) - treat the
assessment's API-issue count as unreliable for this project and rely on the actual build
and manual code inspection during execution to find every `System.Web`/`System.Web.Mvc`
API that needs replacing, rather than trusting "API Issues: 0" as evidence there is
nothing to fix. No test project exists in the repository today - validation relies on a
full solution build plus manual verification of EF Core `SchoolContext` wiring,
notification queue behavior, and the EF Core database initializer invoked from
`Global.asax.cs`.

**Done when**: `ContosoUniversity.csproj` targets `net10.0`; the solution builds with zero
errors and zero warnings; all controllers/views render through ASP.NET Core MVC with no
remaining `System.Web` references; `Web.config` settings are available through
`IConfiguration`; binding redirects are removed from the project; nullable reference types
are enabled; and any existing automated tests pass (none currently exist in this
repository - documented as a gap, not a blocking failure).

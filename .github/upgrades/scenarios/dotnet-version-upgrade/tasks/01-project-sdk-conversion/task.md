# 01-project-sdk-conversion

## Objective
Convert `ContosoUniversity.csproj` from legacy (non-SDK-style, `net48`, `packages.config`) to
SDK-style `Microsoft.NET.Sdk.Web` targeting `net10.0` with `PackageReference`.

## Research Findings

- **Tooling blocker (confirmed twice)**: `UpgradeDotNet-convert_project_to_sdk_style` and
  `UpgradeDotNet-get_dotnet_upgrade_options` both fail to evaluate this project with:
  `The imported project ".../Microsoft.WebApplication.targets" was not found` — this import
  (`$(VSToolsPath)/WebApplications/Microsoft.WebApplication.targets`) only ships with Visual
  Studio / Build Tools on Windows and is not present in this Linux execution environment, and
  never will be for this legacy web-application-typed project (`ProjectTypeGuids` include
  `{349c5851-65df-11da-9384-00065b846f21}`, the ASP.NET MVC web app flavor).
  Since the task requires a **full System.Web.Mvc → ASP.NET Core rewrite** (not a mechanical
  format-only SDK conversion — the `converting-to-sdk-style` skill's own hard constraint is
  "no target framework changes" which does not apply to this task), the new project file is
  written directly as a fresh `Microsoft.NET.Sdk.Web` SDK-style project. The old project's full
  content was read and inventoried below so nothing is lost.

- Old csproj inventory (349 lines):
  - `TargetFrameworkVersion=v4.8`, `OutputType=Library`, legacy web app project type GUIDs
  - ~45 `<Reference>` items: `System.Web.*` (Mvc, Optimization, Razor, WebPages, Helpers, etc. —
    all dropped, replaced by `Microsoft.NET.Sdk.Web`), `System.Messaging` (dropped — see task 03),
    EF Core 3.1.32 family + `Microsoft.Data.SqlClient` 2.1.4 + `Microsoft.Extensions.*` 3.1.32
    (all replaced by current PackageReference versions), `Newtonsoft.Json` 13.0.3 (kept, bumped
    to 13.0.4), `Microsoft.Identity.Client`, `Antlr3.Runtime`, `WebGrease` (transitive bundling
    deps — dropped, no longer needed once `System.Web.Optimization` is removed)
  - Explicit `<Compile>` list (SDK-style uses implicit globbing — no action needed except
    removing `Properties\AssemblyInfo.cs` since SDK-style generates assembly attributes;
    duplicate attributes would be a build error otherwise — must disable
    `GenerateAssemblyInfo` or delete the file; deleting is simpler, there's nothing
    custom in it besides the GUID which isn't required)
  - `<Content>`/`<None>` list of static assets (Content/Scripts/Views/Uploads) and config files
    (`Web.config`, `Web.Debug.config`, `Web.Release.config`, `Views/Web.config`,
    `packages.config`) — all handled by later tasks (02 config, 05 static assets, 06 cleanup)
  - Custom targets: `MvcBuildViews` (ASP.NET compiler precompile — not applicable to Core) and
    `CopySQLClientNativeBinaries` (copies win-x64/win-x86 SNI native DLLs — not needed;
    modern `Microsoft.Data.SqlClient` ships its own native assets via NuGet runtime folders)

- Confirmed via `curl` against `api.nuget.org` (2026-11-07) current stable versions for `net10.0`:
  - `Microsoft.EntityFrameworkCore.SqlServer` → **10.0.14** (also brings in
    `Microsoft.EntityFrameworkCore` + `.Relational` + `.Abstractions` transitively)
  - `Microsoft.Data.SqlClient` → **7.1.1** (not strictly needed as a direct PackageReference
    since `Microsoft.EntityFrameworkCore.SqlServer` already brings a compatible version
    transitively, but kept explicit since `SchoolContextFactory`/callers may reference its types
    directly as `UseSqlServer` extension — actually that extension lives in the EFCore.SqlServer
    package; `Microsoft.Data.SqlClient` types aren't referenced directly in app code, so no
    explicit PackageReference is required — EF Core brings it transitively. Will omit explicit
    reference unless a direct `Microsoft.Data.SqlClient` type is used in code.)
  - `Newtonsoft.Json` → **13.0.4** (latest stable; 14.0.1 is beta-only, skip)
  - `Microsoft.EntityFrameworkCore.Tools` not required (no `dotnet ef` migrations workflow used —
    `DbInitializer` uses `EnsureCreated()`, not EF migrations) — omit to keep deps minimal

## Steps
1. Write new SDK-style `ContosoUniversity.csproj` (`Microsoft.NET.Sdk.Web`, `TargetFramework=net10.0`,
   `Nullable=disable` to match original code's non-nullable-annotated style, `ImplicitUsings=disable`
   to keep explicit usings matching existing files, `InvariantGlobalization=false` default).
2. Delete `packages.config`.
3. Delete `Properties/AssemblyInfo.cs` (SDK-style auto-generates assembly info; keeping it would
   duplicate `AssemblyTitle`/`AssemblyVersion` attributes and fail the build).
4. Add `PackageReference`: `Microsoft.EntityFrameworkCore.SqlServer` 10.0.14, `Newtonsoft.Json` 13.0.4.
5. `dotnet restore` to verify package resolution.

## Files Modified
- `ContosoUniversity.csproj` (rewritten)
- `packages.config` (deleted)
- `Properties/AssemblyInfo.cs` (deleted)

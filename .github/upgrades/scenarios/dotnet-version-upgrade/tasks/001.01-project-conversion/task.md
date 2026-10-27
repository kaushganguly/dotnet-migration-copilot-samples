# 001.01-project-conversion: Convert csproj to SDK-style net10.0 + NuGet upgrade

## Objective
Convert `ContosoUniversity.csproj` from legacy (packages.config, ToolsVersion 15.0, Microsoft.WebApplication.targets) format to SDK-style `Microsoft.NET.Sdk.Web` targeting `net10.0`.

## Steps
1. Rewrite `ContosoUniversity.csproj` as SDK-style: `<Project Sdk="Microsoft.NET.Sdk.Web">`, `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>disable</Nullable>`, `<ImplicitUsings>disable</ImplicitUsings>` (keep explicit usings to minimize churn).
2. Remove `packages.config`; add `<PackageReference>` items for: Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Tools, Microsoft.EntityFrameworkCore.Design (net10.0-aligned 10.0.x), Microsoft.Data.SqlClient (latest), Newtonsoft.Json (keep, used by NotificationService), Antlr4 replacement not needed (Antlr was a transitive MVC dep, drop). Remove Microsoft.AspNet.* / Microsoft.Web.Infrastructure / Microsoft.CodeDom.Providers.* / NETStandard.Library / System.Buffers / System.Memory / System.Numerics.Vectors / System.Runtime.CompilerServices.Unsafe / System.Threading.Tasks.Extensions / System.ComponentModel.Annotations / System.Collections.Immutable — all now part of the shared framework.
3. Remove `packages.config`, `Properties/AssemblyInfo.cs` (SDK-style auto-generates assembly info), all legacy `<ItemGroup>`/`<Reference>`/`<Import>`/`<ProjectExtensions>` blocks, binding redirects (none needed in SDK-style).
4. Remove the `CopySQLClientNativeBinaries` custom MSBuild target (SNI native copy no longer needed — modern Microsoft.Data.SqlClient handles this via runtime assets).
5. Static content (Content/, Scripts/, Uploads/) moves under `wwwroot/` in task 001.02.

## Notes
- This sandbox has no Visual Studio msbuild.exe (Linux). A no-op stub `Microsoft.WebApplication.targets` was placed under the dotnet SDK path purely to unblock MCP assessment tooling; once this task removes the legacy project's import of that file, the stub is irrelevant for this project (leave it in place, it is outside the repo).
- After conversion use `dotnet build` (SDK-style + net10.0 = standard dotnet CLI build path per the building-projects skill).

## Research Findings (confirmed against repo)

- Current `ContosoUniversity.csproj`: legacy format (`ToolsVersion="15.0"`, no `Sdk` attribute),
  `TargetFrameworkVersion v4.8`, `OutputType Library` (web app via ProjectTypeGuids), imports
  `Microsoft.WebApplication.targets`, has a `CopySQLClientNativeBinaries` custom target copying
  `Microsoft.Data.SqlClient.SNI.*.dll` from `packages\...SNI.runtime.2.1.1\...` — dropped (no
  longer needed; modern `Microsoft.Data.SqlClient` ships its own native assets via NuGet runtime
  folders resolved by the SDK).
- `packages.config` has 45 packages (see assessment). Confirmed target versions via
  `get_supported_package_version` for net10.0:
  - `Microsoft.EntityFrameworkCore(.SqlServer/.Tools/.Design)` → **10.0.14**
  - `Microsoft.Data.SqlClient` → **7.1.1**
  - `Newtonsoft.Json` → **13.0.4** (kept — used explicitly by `Services/NotificationService.cs`
    via `JsonConvert.SerializeObject/DeserializeObject`)
  - All `Microsoft.AspNet.*`, `Microsoft.Web.Infrastructure`, `Microsoft.CodeDom.Providers.*`,
    `NETStandard.Library`, `System.Buffers`, `System.Memory`, `System.Numerics.Vectors`,
    `System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`,
    `System.ComponentModel.Annotations`, `System.Collections.Immutable`,
    `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.HashCode`, `System.Diagnostics.DiagnosticSource`,
    `Microsoft.Extensions.*` (Caching/Configuration/DependencyInjection/Logging/Options/Primitives)
    → dropped as direct references: these all ship as part of the net10.0 shared framework /
    come transitively with `Microsoft.NET.Sdk.Web` + EF Core 10 package graph.
  - `Microsoft.Identity.Client`, `Antlr`, `bootstrap`/`jQuery`/`jQuery.Validation`/
    `Microsoft.jQuery.Unobtrusive.Validation`/`Modernizr`/`WebGrease` (front-end/legacy-only
    packages, not referenced from C#) → dropped; the actual `.js`/`.css` files already exist
    under `Scripts/`/`Content/` and are moved to `wwwroot/` by task 001.02 instead of being
    pulled via NuGet front-end packages.
- `Properties/AssemblyInfo.cs` has standard `AssemblyTitle`/`AssemblyProduct`/etc attributes —
  removed; SDK-style projects set these via project properties and `GenerateAssemblyInfo`
  (default `true`), so keeping the old file would cause CS0579 duplicate-attribute errors.

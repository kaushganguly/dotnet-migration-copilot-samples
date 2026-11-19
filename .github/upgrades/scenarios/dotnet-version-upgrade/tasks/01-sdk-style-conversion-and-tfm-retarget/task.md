# Task 01: Convert to SDK-style and retarget to net10.0

## Scope
- ContosoUniversity.csproj (single project, ClassicClassLibrary/web-app kind, currently net48, non-SDK-style, packages.config)

## Research notes (from assessment)
- Project.0001 (Mandatory): needs SDK-style conversion
- Project.0002 (Mandatory): needs TFM change net48 -> net10.0
- Binding.0006/0007: 6 manual assemblyBinding redirects conflict with/downgrade auto-generated versions -> remove (SDK-style/.NET Core does not need them)
- NuGet.0003 (Mandatory, 11 packages): functionality included with framework reference -> remove explicit package refs: Microsoft.AspNet.Mvc, Microsoft.AspNet.Razor, Microsoft.AspNet.WebPages, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Web.Infrastructure, NETStandard.Library, System.Buffers, System.ComponentModel.Annotations, System.Memory, System.Numerics.Vectors, System.Threading.Tasks.Extensions (most of these are superseded in .NET 10 / replaced by ASP.NET Core MVC framework reference in Task 02; for Task 01 remove the BCL-shim ones that are purely framework-provided: System.Buffers, System.Memory, System.Numerics.Vectors, System.Threading.Tasks.Extensions, System.ComponentModel.Annotations, NETStandard.Library, Microsoft.Web.Infrastructure, Microsoft.CodeDom.Providers.DotNetCompilerPlatform)
- NuGet.0001 (Mandatory, 1 package): Microsoft.AspNet.Web.Optimization incompatible -> removed in Task 02 along with bundling rewrite (kept referenced until Task 02 replaces usages, OR remove now and let Task 02 fix compile errors — since TFM change alone breaks System.Web entirely regardless)
- NuGet.0004: Microsoft.Data.SqlClient 2.1.4 has a security vulnerability -> upgrade to 7.1.2 (compliance requirement)
- NuGet.0005: Microsoft.Data.SqlClient and Microsoft.Identity.Client flagged deprecated/vulnerable -> upgrade/evaluate
- NuGet.0002 (24 packages): version bumps recommended, notably Microsoft.EntityFrameworkCore* 3.1.32 -> compatible net10 version (use EF Core 9.x LTS-compatible with net10, or latest per `get_supported_package_version`)
- Antlr 3.4.1.9004 has no net10-compatible version; recommended replacement Antlr4 4.6.6 — used by the notification queue code path (verify usage) or can be removed if unused in app code (framework-only MVC bundling dependency, not app code)

## Approach
1. Call `convert_project_to_sdk_style` for ContosoUniversity.csproj (converts packages.config -> PackageReference, removes legacy imports).
2. Edit resulting .csproj: set `<TargetFramework>net10.0</TargetFramework>`, remove framework-provided package references, resolve package versions via `get_supported_package_version` for net10.0.
3. Remove manual `<assemblyBinding>` block from Web.config (not needed with SDK-style/.NET Core).
4. This task does NOT need to produce a clean net10.0 build yet (System.Web APIs don't exist in .NET Core) — Task 02 completes the migration. Validate only that the project file is well-formed SDK-style XML and package restore succeeds for the packages that remain valid for net10.0.

## Done when
- ContosoUniversity.csproj is SDK-style, targets net10.0, packages.config removed, framework-provided legacy packages removed, Microsoft.Data.SqlClient upgraded off the vulnerable version, EF Core packages bumped to net10-compatible versions.
- Manual assembly binding redirects removed from Web.config.

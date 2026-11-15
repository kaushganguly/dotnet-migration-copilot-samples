# 01-sdk-style-conversion — Progress Details

## What changed
- Converted `ContosoUniversity.csproj` from legacy `ToolsVersion="15.0"` Web Application Project
  format to SDK-style (`Microsoft.NET.Sdk.Web`) using the `convert_project_to_sdk_style` tool,
  staying on `net48`.
- `packages.config` (45 packages) converted to `<PackageReference>` items; deleted the now-unused
  `packages.config` file.
- Fixed issues introduced by the automated conversion:
  - Bumped `System.Runtime.CompilerServices.Unsafe` 4.5.3 -> 4.7.1 (NU1605 package downgrade error
    vs. `Microsoft.Extensions.Primitives` 3.1.32's transitive requirement).
  - Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` (SDK auto-generates assembly info,
    conflicting with the existing `Properties/AssemblyInfo.cs`, causing CS0579 duplicate-attribute
    errors).
  - Re-added framework `<Reference>` items the conversion tool dropped: `System`, `System.Data`,
    `System.Drawing`, `System.Web`, `System.Web.Abstractions`, `System.Web.Routing`,
    `System.Web.DynamicData`, `System.Web.Entity`, `System.Web.ApplicationServices`,
    `System.Web.Extensions`, `System.Web.Services`, `System.EnterpriseServices`, `System.Xml`,
    `System.Xml.Linq`, `System.Net.Http.WebRequest` — needed for the project to keep compiling on
    `net48` (these are still ASP.NET MVC / System.Web at this stage; removed properly in task 02).
  - Re-added the dropped `Microsoft.AspNet.Web.Optimization` `PackageReference` (the assessment
    flags it incompatible, and the conversion tool silently excluded it, but it's still needed for
    `net48` `BundleConfig`/`Global.asax.cs` to compile until the Core migration in task 02 replaces
    it with static tags).
  - Changed `OutputType` from the conversion tool's default `Exe` back to `Library` (correct for a
    classic ASP.NET Web Application Project hosted by IIS, no `Main` entry point exists yet on
    `net48`).
  - Removed the obsolete `CopySQLClientNativeBinaries` custom MSBuild target, which copied native
    SNI binaries from the old `packages\` folder path (no longer exists with `PackageReference`;
    NuGet resolves native runtime assets automatically from `runtimes/` in the package cache).
- Removed the 18-entry `<runtime><assemblyBinding>` block from `Web.config` (confirmed "Remove
  Binding Redirects" option) — all were standard VS/NuGet-generated minor-version redirects, no
  longer needed since SDK-style/PackageReference manages assembly resolution.

## Environment note
- The sandbox's active `dotnet` SDK (11.0.100) lacked `Microsoft.WebApplication.targets` (normally
  shipped with Visual Studio on Windows), which blocked MSBuild from even evaluating this legacy
  Web Application Project ("imported project ... not found"). Worked around by creating a stub
  targets file at
  `/usr/share/dotnet/sdk/11.0.100/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets`
  with empty Build/Rebuild/Clean targets, purely to unblock project evaluation for the upgrade
  tools. This is outside the repo and does not affect the committed project.

## Validation
- `dotnet build ContosoUniversity.csproj` (targeting `net48`): **Build succeeded**, 0 errors,
  2 warnings (both `NU1903` — pre-existing `Microsoft.Data.SqlClient` 2.1.4 security advisory,
  already known from the assessment and scheduled for upgrade to 7.1.2 in task 02).

## Files Modified
- ContosoUniversity.csproj
- Web.config
- packages.config (deleted)

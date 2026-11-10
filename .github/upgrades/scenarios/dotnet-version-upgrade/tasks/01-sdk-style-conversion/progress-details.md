# Progress — 01-sdk-style-conversion

## What changed
- Ran `convert_project_to_sdk_style` on `ContosoUniversity.csproj` (still targeting `net48`).
- Post-conversion fixes required to restore a clean build (all structural, no TFM/version
  drift beyond what's noted):
  - `System.Runtime.CompilerServices.Unsafe` bumped 4.5.3 → 4.7.1: PackageReference enforces
    strict version resolution (unlike packages.config), and EF Core 3.1.32's own dependency
    graph requires >= 4.7.1 — this was a latent conflict packages.config silently tolerated.
  - Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` — SDK-style auto-generates
    assembly attributes, which duplicated the existing `Properties/AssemblyInfo.cs`.
  - Added explicit `<Reference Include="System.Web" />` — the legacy project obtained this
    transitively via `Microsoft.WebApplication.targets`/`ProjectTypeGuids`; SDK-style needs it
    explicit while still net48.
  - Re-added `PackageReference Include="Microsoft.AspNet.Web.Optimization"` — the conversion
    tool dropped it along with `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`. The latter
    is unused in code (no-op removal); the former is still used by `Global.asax.cs` and
    `App_Start/BundleConfig.cs` at this stage (removed in task 02 per the Unsupported Packages
    option), so it had to come back for task 01's net48 build to succeed.
  - `<OutputType>` corrected from the conversion tool's default `Exe` back to `Library` — this
    project doesn't get a `Main` method until task 02 introduces `Program.cs` for the ASP.NET
    Core host.
  - Removed the legacy `CopySQLClientNativeBinaries` post-build target — it copied native SNI
    binaries from the old `packages\` folder path, which no longer exists under
    PackageReference (NuGet handles native runtime assets automatically now).
  - Removed `packages.config` from disk (already removed from the project by the tool).
  - Left `Services\LoggingService.cs` excluded from compilation (conversion tool flagged it as
    "now included by globbing that were not in the original project file") — the file is empty
    and was never part of the original build; excluding it preserves original behavior.

## Validation
- `dotnet build ContosoUniversity.csproj` (targeting `net48`): **Build succeeded, 0 errors**.
  Only warning: `NU1903` (Microsoft.Data.SqlClient 2.1.4 known vulnerability) — expected,
  addressed by the package upgrade in task 02.

## Status: Done

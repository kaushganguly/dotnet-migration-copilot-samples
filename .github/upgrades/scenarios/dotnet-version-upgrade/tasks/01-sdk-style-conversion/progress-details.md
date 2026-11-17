# Task 01 — SDK-style conversion — Progress Details

Status: ✅ Completed

## Changes made
- Converted `ContosoUniversity/ContosoUniversity.csproj` from legacy (non-SDK) format to SDK-style
  using `Sdk="Microsoft.NET.Sdk.Web"`, `TargetFramework=net48`, `PackageReference`-based dependencies
  (via the `convert_project_to_sdk_style` MCP tool).
- Set `OutputType` to `Library` (the auto-conversion defaulted to `Exe`, which is wrong for a
  classic IIS-hosted ASP.NET MVC app with no `Main` entry point — build failed with CS5001 until corrected).
- Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` to avoid CS0579 duplicate-attribute
  errors against the existing `Properties/AssemblyInfo.cs`.
- Bumped `System.Runtime.CompilerServices.Unsafe` 4.5.3 → 4.7.1 to resolve an NU1605 package
  downgrade conflict introduced by the PackageReference graph (transitive minimum from
  `Microsoft.Extensions.Primitives`).
- Re-added explicit Framework `<Reference>` items that the conversion tool dropped but that the
  code still requires on net48 (System.Web, System.Web.Routing, System.Web.Abstractions,
  System.Web.ApplicationServices, System.Web.DynamicData, System.Web.Entity, System.Web.Extensions,
  System.Web.Services, System.EnterpriseServices, System.Drawing, System.Xml, System.Xml.Linq,
  System.Net.Http.WebRequest, System, System.Data). `Microsoft.NET.Sdk.Web` does not implicitly
  supply these for a net48 TFM the way classic `Microsoft.WebApplication.targets`-based projects did.
- Re-added the missing `PackageReference Include="Microsoft.AspNet.Web.Optimization" Version="1.1.3"`
  — the conversion tool failed to map this package from its original `<Reference>`
  (it had a literal `HintPath`, unlike the sibling AspNet.Mvc/Razor/WebPages references, and was
  silently dropped), causing `System.Web.Optimization` (BundleCollection) build errors.
- Removed the obsolete `CopySQLClientNativeBinaries` custom post-build `<Target>` — it copied
  `Microsoft.Data.SqlClient.SNI` native binaries from a `packages\...` folder path that no longer
  exists under PackageReference-based restore. Verified the native SNI binaries
  (`Microsoft.Data.SqlClient.SNI.x64.dll` / `.x86.dll`) are now copied to the output directory
  automatically by the package's own asset resolution — no custom target needed.
- Reviewed and removed the `<runtime><assemblyBinding>...</assemblyBinding></runtime>` block from
  `Web.config` (binding redirects option = "Document and Review Before Removing"). All entries were
  stale pins from the legacy `packages.config`/manual-assembly-reference era (Microsoft.Web.Infrastructure,
  Antlr3.Runtime, Newtonsoft.Json, System.Web.Optimization/Mvc/WebPages/Helpers, WebGrease, several
  Microsoft.Extensions.* 3.1.32 redirects, Microsoft.Data.SqlClient, System.Memory, netstandard, etc.).
  None reflected a live multi-version conflict beyond what PackageReference-based restore now resolves
  automatically; safe to remove.

## Build validation
- `dotnet build ContosoUniversity.csproj` (net48, this Linux sandbox): **Build succeeded, 0 errors.**
- Remaining warning: `NU1903` — `Microsoft.Data.SqlClient` 2.1.4 has a known high-severity vulnerability.
  This is **intentionally deferred to Task 02** (full package upgrade — SqlClient 2.1.4 → 7.1.2 — is
  explicitly in that task's scope per `plan.md` and the assessment's security findings); fixing it here
  would pull net10-only package versions into a project still pinned at net48.

## Environment note
- Required a one-time, repo-external build-environment shim: a no-op stub
  `Microsoft.WebApplication.targets` file at
  `/usr/share/dotnet/sdk/11.0.100/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets`
  (created via `sudo`), because this Linux sandbox has no Visual Studio / MSBuild web-targets
  installation and `$(VSToolsPath)` still resolves to a non-empty default, causing the *original*
  non-SDK project's web-targets `<Import>` to fail evaluation before conversion could even run.
  This file is **not part of the repository** and is not tracked by git.

## Excluded file (no action needed)
- `Services/LoggingService.cs` was flagged by the conversion tool as newly globbed-in but not in the
  original project's explicit item list. Verified it is a 0-byte empty file, unreferenced anywhere in
  the codebase — left excluded from the project (harmless, no behavior impact).

# Progress — 01-project-sdk-conversion

## What changed
- Rewrote `ContosoUniversity.csproj` as SDK-style (`Microsoft.NET.Sdk.Web`), `TargetFramework=net10.0`.
  Replaced ~45 legacy `<Reference>` HintPath entries with 3 `PackageReference`s:
  - `Microsoft.EntityFrameworkCore.SqlServer` 10.0.14 (brings EF Core + Relational + Abstractions transitively)
  - `Newtonsoft.Json` 13.0.4
  - `System.Security.Cryptography.Pkcs` 10.0.14 (explicit pin — transitive 9.0.11 had NU1902/NU1903 moderate+high
    severity advisories; verified via `dotnet restore` that pinning to 10.0.14 clears both warnings)
- Deleted `packages.config` and `Properties/AssemblyInfo.cs` (SDK-style generates assembly attributes automatically).
- Removed legacy custom MSBuild targets (`MvcBuildViews` precompile, `CopySQLClientNativeBinaries` SNI DLL copy) —
  not applicable to `Microsoft.NET.Sdk.Web`/modern `Microsoft.Data.SqlClient`.

## Deviation from standard tooling
`UpgradeDotNet-convert_project_to_sdk_style` and `UpgradeDotNet-get_dotnet_upgrade_options` both fail with
`Microsoft.WebApplication.targets not found` — this Visual-Studio-only MSBuild import cannot be evaluated
on this Linux agent for the legacy web-application-typed project, and the task scope (full ASP.NET Core
rewrite) exceeds the `converting-to-sdk-style` skill's format-only hard constraint anyway. Project file was
written by hand from the fully-inventoried old `.csproj` (349 lines reviewed) instead.

## Validation
- `dotnet restore ContosoUniversity.csproj` → succeeded, 0 warnings, 0 errors.
- Full `dotnet build` is deferred to task 06 (intermediate build will fail until controllers/Global.asax/App_Start,
  which still reference `System.Web.Mvc`, are migrated in tasks 02–04 — expected or an in-place rewrite split
  across ordered tasks).

## Files Modified
- ContosoUniversity.csproj (rewritten)
- packages.config (deleted)
- Properties/AssemblyInfo.cs (deleted)

# 01 — SDK-style conversion

**Status:** Completed

## Changes
- Ran `convert_project_to_sdk_style` on `ContosoUniversity.csproj`, then manually rewrote it:
  - `Sdk="Microsoft.NET.Sdk.Web"`, `TargetFramework=net10.0`.
  - Removed `packages.config` and all `<Reference>`/`<PackageReference>` entries for legacy
    MVC5/System.Web/WebPages/Optimization/CodeDom/WebGrease/Antlr/bootstrap/jQuery NuGet packages
    (these are either obsolete, replaced by framework-provided assemblies, or replaced by static
    files already committed under `wwwroot`).
  - Removed stale `<Content Include="Content\...">` items (content moved to `wwwroot/` and is
    now included automatically by the Web SDK's static-asset globbing).
  - Removed the `CopySQLClientNativeBinaries` MSBuild target (referenced a `packages\` folder that
    was never restored).
  - Final package set: `Microsoft.Data.SqlClient 7.1.2`, `Microsoft.EntityFrameworkCore 10.0.15`,
    `Microsoft.EntityFrameworkCore.SqlServer 10.0.15` (versions confirmed via
    `get_supported_package_version`).
- Moved static assets with `git mv`: `Content/` → `wwwroot/Content/`, `Scripts/` → `wwwroot/Scripts/`,
  `Uploads/` → `wwwroot/Uploads/`.

## Environment note
This Linux sandbox lacked `Microsoft.WebApplication.targets` (Windows/VS-only MSBuild import
required to evaluate the *original* non-SDK web project format). A no-op stub was created at
`/usr/share/dotnet/sdk/11.0.100/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets`
(outside the repo) to unblock the assessment/options tools before conversion. Once the project
became SDK-style this was no longer required for building.

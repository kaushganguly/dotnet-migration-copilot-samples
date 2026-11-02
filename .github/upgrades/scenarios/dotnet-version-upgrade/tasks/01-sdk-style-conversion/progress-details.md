# Task 01-sdk-style-conversion — Progress

## What changed
- Ran `convert_project_to_sdk_style` on `ContosoUniversity.csproj`.
  - Converted to SDK-style (`Microsoft.NET.Sdk.Web`), still targeting `net48`.
  - `packages.config` migrated to `PackageReference` items; file removed from disk.
  - Legacy VS web-application imports/targets removed; custom `CopySQLClientNativeBinaries`
    target preserved.
  - `Microsoft.AspNet.Web.Optimization` (incompatible package per assessment) was dropped
    automatically by the conversion tool.
  - Tool correctly excluded `Services/LoggingService.cs` from the SDK-style glob to
    preserve original behavior (it was never compiled in the legacy project).
- Manual fixups to make the net48 intermediate state build as cleanly as possible on this
  Linux toolchain (no Visual Studio / System.Web reference assemblies auto-wired by old
  ASP.NET web-app project type):
  - Added explicit `<Reference Include="System.Web" />`.
  - Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` (project already has
    `Properties/AssemblyInfo.cs`, which caused CS0579 duplicate-attribute errors under
    SDK-style auto-generation).
  - Bumped `System.Runtime.CompilerServices.Unsafe` 4.5.3 → 4.7.1 to resolve a
    NU1605 package-downgrade error (transitive min version required by
    `Microsoft.Extensions.Primitives` 3.1.32).

## Remaining known-broken state (expected, addressed in Task 02)
- `BundleConfig.cs` / `Global.asax.cs` reference `System.Web.Optimization`, whose package
  was removed (incompatible, per assessment `NuGet.0001`). This is intentional — Task 02
  replaces `System.Web.Optimization` bundling with ASP.NET Core static files as part of the
  full MVC5 → ASP.NET Core migration. Not fixed here to avoid duplicate work.

## Validation
- `dotnet build` on net48: fails only on the expected `System.Web.Optimization` references
  (3 errors), all addressed in Task 02. No other errors.

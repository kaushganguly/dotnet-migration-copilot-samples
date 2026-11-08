# Task 01: Prerequisites and SDK-style conversion — Progress

Status: Completed

## What changed
- Converted `ContosoUniversity.csproj` from the legacy (non-SDK) project format to SDK-style
  (`Microsoft.NET.Sdk.Web`) using `convert_project_to_sdk_style`. Project stayed on `net48` for
  this task (deliberately — net10 retarget is Task 02).
- Migrated all 45 `packages.config` entries to `<PackageReference>` items; deleted `packages.config`.
- Verified original compile/content item semantics were preserved (including the intentional
  exclusion of the empty `Services/LoggingService.cs` file from implicit globbing, matching the
  original project's explicit item list).

## Notes / follow-ups carried into Task 02
- The converted csproj retained a legacy `CopySQLClientNativeBinaries` MSBuild target that
  references the old `packages\` folder layout — removed in Task 02 since it's obsolete with
  the modern Microsoft.Data.SqlClient package.

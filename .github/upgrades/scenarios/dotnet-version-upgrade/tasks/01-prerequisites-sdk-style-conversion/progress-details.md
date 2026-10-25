# Progress: 01-prerequisites-sdk-style-conversion

## What changed
- Verified .NET 10 SDK installed (`validate_dotnet_sdk_installation` → Compatible SDK found).
- Converted `ContosoUniversity.csproj` to SDK-style via `convert_project_to_sdk_style`
  (`Sdk="Microsoft.NET.Sdk.Web"`), staying on `net48`.
- `packages.config` (45 packages) converted to `<PackageReference>` items; removed the now-unused
  `packages.config` file.
- Removed legacy `ToolsVersion`, `ProjectTypeGuids`, and the `<ProjectExtensions><VisualStudio>` web
  flavor block — handled automatically by the conversion tool.
- Fixed a NuGet downgrade error introduced by proper PackageReference resolution: bumped
  `System.Runtime.CompilerServices.Unsafe` from the packages.config-pinned `4.5.3` to `4.7.1`
  (required transitively by `Microsoft.Extensions.Primitives 3.1.32`).
- Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` to avoid duplicate assembly attribute
  errors against the existing `Properties/AssemblyInfo.cs`.
- `Services/LoggingService.cs` (0 bytes, unused, not referenced anywhere) is excluded by the
  conversion tool's auto-generated `<Compile Remove>` since it was never part of the original
  project's compile list — left as-is (matches pre-existing behavior).

## Verification
- `dotnet restore` / `dotnet build ContosoUniversity.csproj` (still targeting `net48`): NuGet
  restore succeeds with 0 errors. Compilation shows only `System.Web`-surface errors (System.Web.Mvc,
  System.Web.Routing, System.Web.Optimization, HttpPostedFileBase, HttpApplication,
  RouteCollection, BundleCollection) — expected per plan.md task 01's accepted exception, since this
  task performs no TFM change and System.Web is removed in task 02.

## Deviations from tooling
- `start_task`/`complete_task` could not be used — the Upgrade MCP's plan-parser repeatedly failed
  with "plan.md exists but could not be parsed into tasks" across multiple retries, including after
  simplifying plan.md's prose (ruling out formatting as the cause). See scenario-instructions.md
  "Tool Incident" section. This file and `tasks.md` are updated manually to mirror what the tool
  would have recorded.

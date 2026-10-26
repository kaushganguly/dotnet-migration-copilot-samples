# 01-sdk-style-and-packages

## Objective

Convert `ContosoUniversity.csproj` from the legacy non-SDK-style format (`.NETFramework v4.8`,
`packages.config`) to a modern SDK-style project (`Microsoft.NET.Sdk.Web`) targeting `net10.0`,
with `PackageReference`-based dependencies updated to .NET-10-compatible versions.

## Research Findings (confirmed against repo contents)

- `ContosoUniversity.csproj` is legacy-style: `ToolsVersion="15.0"`, `ProjectTypeGuids` for
  web application, `TargetFrameworkVersion=v4.8`, imports `Microsoft.WebApplication.targets`
  (not available on Linux — confirmed causes `generate_dotnet_upgrade_assessment` to report 0
  projects; this is a tooling environment limitation, not a project problem).
- 46 `<Reference>` entries for `System.Web.*` assemblies and NuGet lib binaries resolved via
  `HintPath` into `packages\...` (no `packages\` folder currently materialized/restored).
- `packages.config` lists 44 packages. Relevant ones to replace with `PackageReference` at
  .NET-10-compatible versions (verified against nuget.org v3-flatcontainer index):
  | Package | Current | New |
  |---|---|---|
  | Microsoft.EntityFrameworkCore | 3.1.32 | 10.0.14 |
  | Microsoft.EntityFrameworkCore.SqlServer | 3.1.32 | 10.0.14 |
  | Microsoft.EntityFrameworkCore.Tools | 3.1.32 | 10.0.14 |
  | Microsoft.Data.SqlClient | 2.1.4 | 7.1.1 |
  | Newtonsoft.Json | 13.0.3 | 13.0.4 (stable) |
  | Microsoft.AspNet.Mvc / Razor / WebPages / Web.Optimization, WebGrease, Antlr, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Web.Infrastructure, Microsoft.jQuery.Unobtrusive.Validation, NETStandard.Library, Microsoft.Bcl.*, System.* compat shims, Microsoft.Identity.Client | n/a | **removed** — superseded by `Microsoft.NET.Sdk.Web` + `Microsoft.AspNetCore.*` which ship in the shared framework |
  | jQuery / jQuery.Validation / bootstrap / Modernizr | npm/libman content-only packages (just drop scripts in `Scripts/`/`Content/`) | **removed** — static files already vendored under `Scripts/`/`Content/`, moved to `wwwroot/` in task 02, no NuGet package needed |
  | System.Messaging (implicit via `System.Messaging` framework reference) | n/a | add `MSMQ.Messaging` 1.0.4 (netstandard2.0-compatible replacement, used by `Services/NotificationService.cs`) |
- No `Directory.Build.props`/`.targets`, no Central Package Management in use — plain
  per-project `PackageReference` is appropriate (single project in solution).
- Target: `Microsoft.NET.Sdk.Web`, `<TargetFramework>net10.0</TargetFramework>`,
  `<Nullable>disable</Nullable>` (keep existing nullable-oblivious code as-is to minimize
  diff — not part of this upgrade's scope), `<ImplicitUsings>disable</ImplicitUsings>` to
  avoid silently changing using-directive resolution across ~20 existing files in one pass.
- `Properties/AssemblyInfo.cs` has explicit `[assembly: AssemblyVersion]`/`[assembly: Guid]`
  attributes — SDK-style projects generate these by default
  (`GenerateAssemblyInfo`/`GenerateAssemblyVersionInfo`), so either delete the file or set
  `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`. Decision: delete `Properties/AssemblyInfo.cs`
  and let the SDK generate assembly info (standard SDK-style convention), since none of its
  values are load-bearing for this app.
- `Uploads/TeachingMaterials/.gitkeep` and the `Uploads` folder are user-content storage, not
  compiled content — must not be caught by any over-broad `<Content Include>`/glob; SDK-style
  default globbing includes `**/*` by content type, which is fine (it is already just static files).

## Planned Changes

1. Replace `ContosoUniversity.csproj` with SDK-style equivalent:
   - `Sdk="Microsoft.NET.Sdk.Web"`, `TargetFramework=net10.0`
   - `PackageReference` set per table above
   - No manual `<Compile Include>`/`<Content Include>` items — rely on SDK implicit globbing
   - Remove `App_Start`, `Global.asax(.cs)` from compile scope only in task 02 (left in place
     physically until task 02 replaces them with `Program.cs`, to keep this task buildable
     in isolation is not required — SDK-style still compiles `Global.asax.cs` as a `.cs` file,
     which will fail because `System.Web` types won't resolve; this is expected and resolved by
     task 02, not this task — see Gate note below)
2. Delete `packages.config`.
3. Delete `Properties/AssemblyInfo.cs` (superseded by SDK auto-generation).
4. Delete the legacy `Views/Web.config` and root `Web.config` `<system.web>`/`<system.webServer>`
   sections that are ASP.NET-Framework-only — full Web.config removal happens in task 02
   alongside `appsettings.json` introduction (kept out of scope here to keep this task
   focused purely on project-file/package format).

## Gate / Exit Criteria for this task

Per the `migrating-aspnet-framework-to-core` skill: "Gate: Project loads in IDE without errors.
No compilation required yet." — this task converts the project file and package set only.
Full compilation is expected to fail until task 02 (ASP.NET Core host/controllers/views) is
complete; that is normal and tracked as the task 02 gate, not this one. This task's own exit
check is: `dotnet restore` succeeds (SDK-style project file + packages resolve), confirming the
project format and package graph are valid for net10.0, even though `dotnet build` will still
fail on `System.Web` symbols until task 02.

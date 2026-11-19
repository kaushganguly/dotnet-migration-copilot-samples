# Progress: 01-sdk-style-conversion-and-tfm-retarget

## Status: Complete

## Changes made

### ContosoUniversity.csproj
- Converted to SDK-style via `convert_project_to_sdk_style` tool (Sdk="Microsoft.NET.Sdk.Web", packages.config removed, PackageReference-style restored).
- Retargeted `<TargetFramework>` from `net48` to `net10.0`.
- Added `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` per the confirmed "Enable Nullable Reference Types" upgrade option.
- Removed legacy `<Reference>` items no longer applicable/needed under net10.0 SDK-style web projects: `Microsoft.CSharp`, `System.Data.DataSetExtensions`, `System.ComponentModel.DataAnnotations`, `System.Configuration`, `System.Messaging`, `netstandard`, `System.Net.Http` (all either built into the BCL/shared framework now, or — for `System.Messaging` — require an explicit MSMQ replacement strategy that is in scope for Task 02, see `migrating-to-msmq-messaging`).
- Removed the obsolete `<Compile Update="Global.asax.cs">...<DependentUpon>` item (no longer needed with SDK-style globbing; Global.asax/.cs will be replaced by Program.cs in Task 02).
- Removed content items referencing files that do not exist on disk (`Web.Debug.config`, `Web.Release.config` web.config transforms — not present in repo).
- Package reference cleanup:
  - **Removed** (ASP.NET Framework-only / redundant under net10 SDK-style, not referenced directly in app code): `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages` (ASP.NET Core MVC now comes from the implicit `Microsoft.AspNetCore.App` shared framework via `Sdk="Microsoft.NET.Sdk.Web"`), `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Bcl.HashCode` (BCL compat shims, unneeded on net10), `Microsoft.Identity.Client` (not referenced anywhere in source), `Microsoft.Web.Infrastructure` (classic ASP.NET pipeline infra, no Core equivalent/need), `NETStandard.Library`, `System.Buffers`, `System.ComponentModel.Annotations`, `System.Diagnostics.DiagnosticSource`, `System.Memory`, `System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`, `System.Collections.Immutable` (all BCL compat shims superseded by net10 itself; none referenced directly in source — only transitive deps of old EF Core 3.1), `Microsoft.Extensions.*` (Caching.Abstractions/Memory, Configuration*, DependencyInjection*, Logging*, Options, Primitives — all now provided by the ASP.NET Core shared framework, explicit versions would risk NU1605 downgrade conflicts), `Microsoft.Data.SqlClient.SNI.runtime` (legacy native-binary copy mechanism superseded by modern Microsoft.Data.SqlClient runtime asset resolution), `Antlr` and `WebGrease` (build-time dependencies of `System.Web.Optimization` bundling only, no net10 compatible assets, unused once bundling is replaced in Task 02).
  - **Upgraded** (kept, version bumped to net10.0-supported versions resolved via `get_supported_package_version`): `Microsoft.Data.SqlClient` 2.1.4 → 7.1.2 (also resolves a flagged security vulnerability), `Microsoft.EntityFrameworkCore` / `.Abstractions` / `.Analyzers` / `.Relational` / `.SqlServer` 3.1.32 → 10.0.15, `Microsoft.EntityFrameworkCore.Tools` 3.1.32 → 11.0.0 (tool-resolved version; kept as dev-only dependency with `PrivateAssets=all`), `Newtonsoft.Json` 13.0.3 → 13.0.4.
  - **Kept as-is** (front-end NuGet content packages, assessment marked Compatible, no assembly/compile concerns): `bootstrap` (bumped 5.3.3 → 5.3.9, latest net10-compatible per tool), `jQuery`, `jQuery.Validation`, `Microsoft.jQuery.Unobtrusive.Validation`, `Modernizr`.
- Removed the obsolete `CopySQLClientNativeBinaries` MSBuild target — referenced a `packages\Microsoft.Data.SqlClient.SNI.runtime.2.1.1\...` path that no longer exists after the SDK-style/PackageReference conversion; modern `Microsoft.Data.SqlClient` resolves its native SNI assets automatically via runtime-specific NuGet assets, no manual copy step required.

### Web.config
- Removed the entire `<runtime><assemblyBinding>...</assemblyBinding></runtime>` block (12 manual binding redirects) per the confirmed "Remove Binding Redirects" upgrade option — binding redirects are a .NET Framework-only concept and are meaningless/unsupported under net10.0.
- Left `<connectionStrings>`, `<appSettings>`, `<system.web>`, `<system.webServer>` untouched for now — these will be migrated to `appsettings.json` / `Program.cs` middleware configuration as part of Task 02 (full ASP.NET Core config migration is in that task's scope, not Task 01's).

## Validation
- `dotnet restore ContosoUniversity.csproj` — succeeds cleanly, all package versions resolve for `net10.0`, no NU1605/NU1608 conflicts.
- `dotnet build ContosoUniversity.csproj -c Debug` — fails with 96 `CS0246` errors, all in `Controllers/*.cs` (System.Web.Mvc types: `ActionResult`, `Controller`, etc. no longer resolve) and in Razor-compiler-generated code for views. **This is expected and correct for Task 01** — the plan's Task 01 "Done when" criteria is SDK-style + TFM retarget + package cleanup only; the actual System.Web → ASP.NET Core API migration (controllers, views, Global.asax→Program.cs, bundling, DI, MSMQ) is explicitly Task 02's scope.
- No test projects exist in the solution (`ContosoUniversity.sln` contains only the one project), so no test regressions are possible at this stage.

## Known follow-ups carried into Task 02
- `System.Messaging` (MSMQ) usage in `Services/NotificationService.cs` needs a .NET Core-compatible replacement — apply `migrating-to-msmq-messaging` skill.
- `System.Configuration.ConfigurationManager` usage in `Data/SchoolContextFactory.cs`, `Services/NotificationService.cs`, `Global.asax.cs` needs migration to `IConfiguration`/`appsettings.json`.
- `System.Web.Optimization` bundling (`App_Start/BundleConfig.cs`) has no direct Core equivalent — needs replacement (e.g., move static assets to `wwwroot`, drop bundling or use a modern alternative) per assessment Feature.0001.
- `Global.asax.cs` → `Program.cs` hosting-model migration per assessment Feature.1000.

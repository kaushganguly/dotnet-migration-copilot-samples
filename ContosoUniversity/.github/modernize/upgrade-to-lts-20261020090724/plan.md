# .NET Upgrade Plan

## Overview

| Item | Value |
|------|-------|
| Source .NET version | .NET Framework 4.8 |
| Target .NET version | .NET 10 (`net10.0`) |
| Project type | ASP.NET MVC 5 Web Application (non-SDK-style `.csproj`) |
| Solution structure | Single project |

## Projects in Scope

| Project | Current TFM | Target TFM | SDK-Style Conversion Required |
|---------|------------|------------|:-----------------------------:|
| ContosoUniversity.csproj | `v4.8` (.NET Framework 4.8) | `net10.0` | Yes |

## Why Upgrade

ContosoUniversity targets .NET Framework 4.8, which is a legacy framework-dependent, non-SDK-style project using `System.Web`/ASP.NET MVC 5, `packages.config` for NuGet dependency management, and a mix of legacy and modern packages (e.g., `Microsoft.EntityFrameworkCore` 3.1.32, which is itself long out of support). .NET Framework does not receive new feature investment and modern Azure SDKs, tooling, and hosting models increasingly target current .NET. Upgrading to .NET 10 (LTS) brings long-term support, modern performance/security improvements, and full compatibility with current Azure SDKs and tooling.

## High-Level Upgrade Scope

1. **Project file modernization**: Convert `ContosoUniversity.csproj` from the legacy non-SDK-style format to the modern SDK-style format (`Microsoft.NET.Sdk.Web`), targeting `net10.0`.
2. **Dependency management migration**: Replace `packages.config` with `PackageReference` entries; update NuGet packages (e.g., `Microsoft.EntityFrameworkCore*`, `Microsoft.Data.SqlClient`, `Newtonsoft.Json`, `Microsoft.Identity.Client`, etc.) to versions compatible with .NET 10.
3. **Web framework migration**: Migrate from ASP.NET MVC 5 (`System.Web.Mvc`) to ASP.NET Core MVC, including:
   - Replacing `Global.asax`/`Global.asax.cs` startup with `Program.cs`/`Startup`-based hosting.
   - Migrating `App_Start` configuration (`BundleConfig`, `FilterConfig`, `RouteConfig`) to ASP.NET Core equivalents (endpoint routing, middleware pipeline, static file/bundling alternatives).
   - Reviewing `Web.config`/`Web.Debug.config`/`Web.Release.config` settings and migrating relevant configuration to `appsettings.json`.
4. **Data access layer updates**: Validate and update Entity Framework Core usage (`SchoolContext`, `SchoolContextFactory`, `DbInitializer`) for compatibility with the current EF Core version supported on .NET 10.
5. **API and library compatibility fixes**: Address any breaking API changes across Controllers, Models, Services, and supporting classes resulting from the `System.Web` to ASP.NET Core migration.
6. **Build and validation**: Ensure the project builds successfully on .NET 10 and existing unit tests continue to pass.

## Execution Note

This plan only defines *what* needs to change. The detailed step-by-step execution (exact package versions, code transformations, API replacements) will be carried out by the `modernize-dotnet-upgrade-engineer` agent when the corresponding task in `tasks.json` is executed.

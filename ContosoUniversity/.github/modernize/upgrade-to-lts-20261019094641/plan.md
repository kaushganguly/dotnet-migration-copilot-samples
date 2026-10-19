# .NET Upgrade Plan: ContosoUniversity

## Overview
- **Source**: .NET Framework 4.8 (classic non-SDK-style `.csproj`, ASP.NET MVC 5 / System.Web)
- **Target**: .NET 10.0 (`net10.0`, latest LTS)
- **Projects**: `ContosoUniversity` (ContosoUniversity.csproj, single web project in `ContosoUniversity.sln`)

## Rationale
Upgrade requested explicitly to .NET 10 (LTS). The app depends on legacy .NET Framework-only technologies that must be moved to modern equivalents.

## Scope
1. Convert the project to SDK-style and set the target framework to `net10.0`.
2. Migrate ASP.NET MVC 5 / System.Web (Global.asax, routing, bundling, Web.config) to ASP.NET Core MVC (Program.cs, appsettings.json, middleware).
3. Replace packages.config / assembly references with PackageReferences; update NuGet packages (EF Core, Microsoft.Data.SqlClient, Newtonsoft.Json, etc.) to .NET 10 compatible versions.
4. Replace unsupported APIs (e.g., System.Messaging, System.Web.*, Razor views helpers) with modern .NET alternatives.
5. Build, fix compatibility issues, and validate with existing tests.

## Task
Single upgrade task: `001-upgrade-dotnet-to-net10` (see `.metadata/tasks.json`).

# .NET Upgrade Plan: ContosoUniversity

## Overview
- **Source**: .NET Framework 4.8 (classic non-SDK-style project, ASP.NET MVC / System.Web, IIS Express)
- **Target**: .NET 10.0 (`net10.0`, latest LTS)
- **Projects**: `ContosoUniversity` (ContosoUniversity.csproj, in ContosoUniversity.sln)

## Scope
A single upgrade task covers:
1. Converting the project to SDK-style and retargeting to `net10.0`
2. Replacing System.Web / ASP.NET MVC hosting with ASP.NET Core (startup, routing, configuration, bundling/static files)
3. Updating NuGet packages to .NET 10 compatible versions (e.g. Entity Framework to EF Core if applicable)
4. Fixing API incompatibilities so the solution builds and existing tests pass
5. Security/compliance check of the upgraded dependencies

## Task
- `001-upgrade-dotnet-to-net10`: Upgrade ContosoUniversity from .NET Framework 4.8 to .NET 10.0

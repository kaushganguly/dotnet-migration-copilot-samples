# .NET Upgrade Plan: ContosoUniversity

## Overview
- **Source**: .NET Framework 4.8 (ASP.NET MVC 5, legacy non-SDK-style csproj, System.Web, packages.config)
- **Target**: .NET 10.0 (`net10.0`, latest LTS)
- **Reason**: Explicit user request to upgrade to .NET 10.

## Projects
| Project | Path | Current | Target |
|---------|------|---------|--------|
| ContosoUniversity | ContosoUniversity.csproj (solution: ContosoUniversity.sln) | .NET Framework 4.8 | net10.0 |

## Scope of Upgrade
Single upgrade task (`001-upgrade-dotnet-to-net10`) covering:
- Conversion from legacy csproj to SDK-style project and TFM update to `net10.0`
- Migration from ASP.NET MVC 5 / System.Web (Global.asax, bundling, web.config) to ASP.NET Core MVC
- NuGet updates: packages.config to PackageReference; EF Core 3.1 and Microsoft.Data.SqlClient to versions compatible with .NET 10; removal of obsolete System.Web-era packages
- API compatibility fixes for build, plus verification that build and existing tests pass

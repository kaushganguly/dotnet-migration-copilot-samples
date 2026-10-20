# .NET Upgrade Plan: ContosoUniversity

## Overview
- **Source**: .NET Framework 4.8 (legacy non-SDK-style project, ASP.NET MVC 5 / System.Web, `packages.config`)
- **Target**: .NET 10.0 (`net10.0`, current LTS)
- **Projects**: `ContosoUniversity` (`ContosoUniversity.csproj`, in `ContosoUniversity.sln`)

## Rationale
The upgrade to .NET 10 was explicitly requested. It moves the app from .NET Framework 4.8 (Windows-only, maintenance-only) to a modern, long-term-supported runtime.

## Scope
Single upgrade task (`001-upgrade-dotnet-to-net10`):
- Convert the project to SDK-style and target `net10.0`
- Migrate `packages.config` references to `PackageReference`, updating packages to .NET 10 compatible versions
- Migrate ASP.NET MVC 5 / System.Web hosting (Global.asax, Web.config, routing, bundling, filters) to ASP.NET Core MVC
- Update API usages incompatible with .NET 10 and fix build errors
- Validate with build and existing tests

## Success Criteria
- Project builds successfully on .NET 10
- Existing unit tests pass

# .NET Upgrade Plan: ContosoUniversity

- **Source**: .NET Framework 4.8 (ASP.NET MVC 5, non-SDK-style csproj, System.Web)
- **Target**: .NET 10.0 (`net10.0`, latest LTS)
- **Projects**: `ContosoUniversity` (ContosoUniversity.csproj, in ContosoUniversity.sln)

## Scope
A single upgrade task covers:
- Conversion to SDK-style project format and TFM update to `net10.0`
- NuGet package updates/replacements (e.g., EF6 / System.Web.* dependencies to modern equivalents)
- Migration from ASP.NET MVC 5 / System.Web to ASP.NET Core (startup, routing, bundling, config, views)
- API compatibility fixes, build verification, and unit tests/security checks

## Task
- `001-upgrade-dotnet-to-net10` — see `.metadata/tasks.json`

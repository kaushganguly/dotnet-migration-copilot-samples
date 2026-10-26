# Scenario Instructions: dotnet-version-upgrade

## Scope

- Repository root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Project: ContosoUniversity/ContosoUniversity.csproj
- Solution: ContosoUniversity/ContosoUniversity.sln
- Target Framework: net10.0
- Scope includes: SDK-style conversion, packages.config -> PackageReference, EF Core/SqlClient package
  updates, ASP.NET MVC 5 -> ASP.NET Core MVC migration, Web.config -> modern configuration system.

## User Preferences

### Flow Mode

Automatic — do not pause for user review/approval at any stage. Only pause if genuinely blocked
(missing information, ambiguous decision with major consequences). Surface assessment/plan/progress
inline and keep moving.

### Execution Style

- Fully autonomous, no user interaction expected.
- Fix all build warnings in modified projects.
- Success criteria: build passes, unit tests pass, security/compliance check, no new unit tests required.

### Source Control

- Git repo. Working branch: `upgrade/dotnet10-contosouniversity` (created from `main`, clean tree at start).

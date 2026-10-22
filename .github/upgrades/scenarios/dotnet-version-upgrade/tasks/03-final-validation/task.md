# 03-final-validation: Build, test, and document the completed upgrade

## Research / Confirmed Scope
- Single project solution: `ContosoUniversity/ContosoUniversity.csproj`, target `net10.0`,
  SDK-style (`Microsoft.NET.Sdk.Web`). No other projects/solutions in the repo.
- No test projects exist anywhere in the repo (confirmed via `find . -iname *.csproj` and a
  grep for `Microsoft.NET.Test.Sdk|xunit|nunit|mstest`): `passUnitTests` success criterion is
  satisfied vacuously, consistent with `generateNewUnitTests=false`.
- Build tool: `dotnet build` is the correct default per the `building-projects` skill (modern
  SDK-style project only, no .resx/WPF/WinForms/COM/multi-targeting-with-net48 conditions that
  would require `msbuild.exe`).

## Validation performed
1. `rm -rf bin obj && dotnet build ContosoUniversity.sln -c Release` -> Build succeeded,
   0 Warning(s), 0 Error(s).
2. `dotnet build` (Debug) -> 0 Warning(s), 0 Error(s) (verified earlier in Task 02 as well).
3. `dotnet list package --vulnerable --include-transitive` -> "has no vulnerable packages".
4. `dotnet list package --deprecated` -> "has no deprecated packages".
5. Runtime smoke test against a temporary Docker SQL Server 2022 container (removed after use):
   confirmed EF Core `DbInitializer` seeding + app startup succeed against a real SQL Server
   backend, and static assets under `wwwroot/` (`/Content/...`, `/Scripts/...`) serve 200.
6. Grepped the full repo for residual legacy references: no `System.Web`, `System.Messaging`,
   `System.Configuration`/`ConfigurationManager`, `@Scripts.Render`/`@Styles.Render`, or
   `HttpContext.Current` remain in `.cs`/`.cshtml` files.

## Outcome
- passBuild: true (0 errors / 0 warnings, Debug + Release)
- passUnitTests: true (vacuous — no test projects in repo, consistent with generateNewUnitTests=false)
- securityComplianceCheck: true (no vulnerable or deprecated packages reported)

# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Target project: ContosoUniversity/ContosoUniversity.csproj (solution: ContosoUniversity/ContosoUniversity.sln)
- Target framework: net10.0
- Migration: ASP.NET Framework 4.8 MVC5 (System.Web.Mvc) -> ASP.NET Core MVC on .NET 10.0
  - SDK-style project conversion
  - EF Core migration (from EF6 / ADO.NET as applicable)
  - System.Data.SqlClient -> Microsoft.Data.SqlClient
  - Web.config -> appsettings.json
  - PackageReference modernization

## User Preferences

### Execution Style
- Flow Mode: Automatic - do not pause for user review/approval at any stage. Only stop if genuinely blocked.
- Source control: working branch upgrade-to-lts-20261107075544 already created and checked out from main (clean tree at init time).
- Success criteria required: build passes, unit tests pass (no new tests need to be generated), security/compliance check passes.

### Technical Preferences
- Target: net10.0 (LTS)
- generateNewUnitTests: false - do not create new unit test projects/tests beyond what's needed to validate existing behavior, unless required to satisfy passUnitTests on already existing tests.

## Decisions
- Proceeding fully automatically per orchestrator instructions; no human-in-the-loop pauses.

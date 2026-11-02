# Task 03 — Final Validation

Status: ✅ Complete

## Build

- `dotnet build ContosoUniversity.csproj` (Debug, net10.0): **0 errors, 0 warnings**
- `dotnet build ContosoUniversity.sln` (Debug): **0 errors, 0 warnings**
- Clean rebuild (`rm -rf bin obj` then restore+build) in **Release** configuration:
  **0 errors, 0 warnings**
- Clean rebuild in **Debug** configuration (restored to leave the workspace in the normal
  dev state): **0 errors, 0 warnings**

## Tests

- `UpgradeDotNet-discover_test_projects` not usable (no candidate project paths to check —
  solution has a single project). Confirmed directly: `find ... -iname "*.csproj"` returns only
  `ContosoUniversity.csproj`; no project in the repository references
  `Microsoft.NET.Test.Sdk`, `xunit`, `NUnit`, or `MSTest`.
- `dotnet test ContosoUniversity.sln` completes with no test projects discovered and no failures.
- **`passUnitTests=true` is satisfied vacuously** — there are no unit tests in this repository
  to pass or fail, before or after the upgrade.

## Outstanding/leftover items reviewed

- `Antlr`/`WebGrease`/`Microsoft.Web.Infrastructure`/`Microsoft.AspNet.Web.Optimization`
  (assessment's 2 flagged-incompatible + other build-only legacy MVC5 packages): confirmed
  unused by any app code (`grep` across `**/*.cs`) before removal in Tasks 1–2. No `Antlr4`
  replacement was needed since the dependency was never consumed by application code — it was
  a transitive build-time artifact of the legacy `Microsoft.AspNet.Mvc` toolchain.
- 12 binding-redirect issues from the assessment: all resolved implicitly — `Web.config` (the
  only place `<assemblyBinding>`/`<bindingRedirect>` existed) was removed entirely in Task 2,
  and SDK-style/`.NET` apps do not use binding redirects (assemblies are resolved via the
  `.deps.json`/`AssemblyLoadContext` mechanism instead).
- `App_Data` folder (referenced via `<Folder Include="App_Data\" />` in the legacy `.csproj`):
  confirmed empty/non-existent on disk; SDK-style projects don't need explicit empty-folder
  declarations, nothing to migrate.
- Documentation files (`README.md`, `NOTIFICATION_SYSTEM_README.md`, `ROLE_SETUP_GUIDE.md`,
  `SETUP_TESTING_GUIDE.md`, `TEACHING_MATERIAL_UPLOAD.md`, `Project_Readme.txt`) left as-is —
  out of scope for a code/project upgrade; none contain build-affecting content.

## Known runtime limitations (environment, not code defects — see Task 2 progress-details.md)

- MSMQ (`MSMQ.Messaging`) is Windows-only; `NotificationService` throws
  `PlatformNotSupportedException` on this Linux sandbox. Identical limitation existed in the
  pre-upgrade .NET Framework 4.8 app (MSMQ was never cross-platform).
- SQL Server LocalDB is Windows-only; the default `appsettings.json` connection string
  (unchanged from the original `Web.config`, `(LocalDB)\MSSQLLocalDB`) cannot connect from
  Linux. Startup database initialization now logs and continues rather than crashing the host
  (improvement over the original, which would have failed startup identically on an unreachable
  database).
- Neither limitation is reachable by `dotnet build`/`dotnet test`, so they do not affect the
  stated success criteria.

## Success Criteria

| Criterion | Result |
|---|---|
| `passBuild=true` | ✅ `dotnet build` (Debug and Release, project and solution) — 0 errors, 0 warnings |
| `passUnitTests=true` | ✅ No test projects in the repository; vacuously satisfied |

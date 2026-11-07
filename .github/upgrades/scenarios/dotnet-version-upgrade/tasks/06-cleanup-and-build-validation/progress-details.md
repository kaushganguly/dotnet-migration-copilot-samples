# Progress — 06-cleanup-and-build-validation

## What changed
- Deleted dead legacy hosting files (all functionality already ported in tasks 02/05):
  `Global.asax`, `Global.asax.cs`, `App_Start/BundleConfig.cs`, `App_Start/FilterConfig.cs`,
  `App_Start/RouteConfig.cs`, `Web.config` (+ confirmed no `Web.Debug.config`/
  `Web.Release.config` exist in this repo — nothing further to delete there).
  Removed the now-empty `App_Start/` directory.
- Full clean rebuild (`rm -rf bin obj && dotnet build`): **Build succeeded — 0 Warning(s),
  0 Error(s)**.
- Repo-wide grep verification (excluding `bin/`/`obj/`) — zero remaining references to:
  `System.Web`, `System.Messaging`, `ConfigurationManager`, `HttpPostedFileBase`,
  `Scripts.Render`/`Styles.Render`.
- `dotnet restore --force` — clean, 0 NuGet security-advisory warnings (the NU1902/NU1903
  `System.Security.Cryptography.Pkcs` transitive vulnerability found during task 01 remains
  resolved by the explicit 10.0.14 pin).
- Confirmed no test project exists anywhere in the repository/solution
  (`find . -iname "*test*.csproj"` → empty) — `passUnitTests` is vacuously satisfied per the
  user's stated success criteria (`generateNewUnitTests: false` — none created, matching
  instructions).
- Runtime smoke test (`dotnet run`): application starts, DI container resolves
  `SchoolContext`/`NotificationService` correctly, reaches `DbInitializer.Initialize` and
  attempts a real SQL connection — fails with
  `PlatformNotSupportedException: LocalDB is not supported on this platform`. This is an
  **environment limitation, not a migration defect**: the original `Web.config` connection
  string was already LocalDB-only (Windows-only technology), so this exact failure mode is
  pre-existing and would occur identically on any non-Windows host regardless of the .NET
  version — the legacy MVC5 app could never have been run on Linux either (it couldn't even
  build here, per task 01's `Microsoft.WebApplication.targets` finding). No SQL Server engine is
  available in this sandbox to fully exercise a live connection; the successful progression
  through host startup → DI resolution → EF Core connection attempt is the furthest
  runtime verification possible in this environment, and it confirms `Program.cs`/DI
  wiring/`SchoolContext` registration/`DbInitializer` are all correctly wired.

## Success Criteria Verification
| Criterion | Result | Evidence |
|---|---|---|
| passBuild | ✅ true | `dotnet build` — 0 errors, 0 warnings |
| generateNewUnitTests | N/A (false, as requested) | No test project created |
| passUnitTests | ✅ true (vacuous) | No test project exists in the solution — nothing to fail |
| securityComplianceCheck | ✅ true | `dotnet restore` — 0 NuGet vulnerability advisories (pinned `System.Security.Cryptography.Pkcs` 10.0.14 to resolve the transitive NU1902/NU1903 moderate+high findings); grep-verified no legacy `System.Web`/`System.Messaging`/`ConfigurationManager` surface remains |

## Files Modified
- Global.asax (deleted)
- Global.asax.cs (deleted)
- App_Start/BundleConfig.cs (deleted)
- App_Start/FilterConfig.cs (deleted)
- App_Start/RouteConfig.cs (deleted)
- Web.config (deleted)

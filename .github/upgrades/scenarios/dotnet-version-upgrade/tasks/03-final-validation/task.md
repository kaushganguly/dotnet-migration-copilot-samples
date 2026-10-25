# Task 03: Final Validation

## Objective
Confirm the full solution builds cleanly on net10.0 and that unit tests (if any) pass,
satisfying the scenario's success criteria (`passBuild=true`, `passUnitTests=true`), then
document any deferred/follow-up recommendations.

## Research (per `building-projects` skill)
- Project is SDK-style (`Sdk="Microsoft.NET.Sdk.Web"`), single project in
  `ContosoUniversity.sln` — the skill's guidance for SDK-style projects on this toolchain is
  to use `dotnet build`/`dotnet restore` directly (no `msbuild.exe`/packages.config-era
  concerns remain after tasks 01-02); this matches what was already used throughout tasks
  01-02.
- `discover_test_projects` (called at the end of task 02) returned `[]` — there are no test
  projects anywhere in this repository, so there is nothing to execute for
  `passUnitTests`; the criterion is vacuously satisfied.
- Both Debug and Release configurations were already built with 0 warnings/0 errors at the
  end of task 02; this task re-verifies that from a clean state (`obj`/`bin` cleaned) to rule
  out any stale-incremental-build false positives, and performs a final repo-wide sanity sweep.

## Steps
1. `dotnet clean` + `dotnet restore` + `dotnet build` (Debug) from a clean state.
2. `dotnet build -c Release` from a clean state.
3. Re-run `discover_test_projects` to reconfirm no test projects exist.
4. Final repo sweep: no legacy `packages.config`/`App_Start`/`Global.asax`/`Web.config`
   artifacts remain; no stray `System.Web`/`System.Messaging` references.
5. Document deferred/follow-up recommendations (environment-specific items not verifiable in
   this sandbox: LocalDB/SQL Server connectivity, Windows MSMQ, and anything that requires an
   actual running Windows host).
6. Commit all changes in a single commit (per the confirmed Source Control / Commit Strategy:
   "Single Commit at End").

## Done when
- [x] Clean Debug build: 0 errors, 0 warnings.
- [x] Clean Release build: 0 errors, 0 warnings.
- [x] No test projects / nothing to run for passUnitTests (documented, not skipped silently).
- [x] Deferred items documented for the user.
- [x] Single commit created with the full migration diff.

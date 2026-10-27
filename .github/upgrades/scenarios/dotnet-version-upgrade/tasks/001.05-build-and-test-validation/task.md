# 001.05-build-and-test-validation: Build, fix errors/warnings, validate, run tests

## Objective
Get the migrated project building clean on net10.0 and validate behavior.

## Steps
1. `dotnet build` the project; iterate on compiler errors until clean.
2. Fix all build warnings introduced or pre-existing in touched files (per workflow rules, no `#pragma warning disable`/`NoWarn` without approval).
3. There is no test project in this repository — `passUnitTests=true` is satisfied vacuously (document this in progress-details.md). Optionally do a basic smoke check: `dotnet run` + curl a couple of routes if feasible in this sandbox (no SQL Server available in sandbox, so full DB-backed smoke test may not be possible — document what was/ wasn't verified).
4. Remove the stub `Microsoft.WebApplication.targets` file placed under the dotnet SDK path (no longer needed once the project is SDK-style) if it does not interfere with other repos/tools; otherwise document that it is harmless and outside the repo.
5. Commit the migration.

# Task 04: Final build and validation

## Objective
Build the solution and validate the migration against success criteria
(`passBuild=true`, `passUnitTests=true`), resolving any remaining API incompatibilities.

## Validation performed
1. `dotnet restore` on `ContosoUniversity.csproj` — succeeds.
2. `dotnet build ContosoUniversity.csproj` — **0 Warning(s), 0 Error(s)**.
3. `dotnet build ContosoUniversity.sln` — **0 Warning(s), 0 Error(s)** (confirms the existing
   `.sln`'s generic C# project type GUID works unchanged with the new SDK-style csproj).
4. `dotnet list package --vulnerable --include-transitive` — no vulnerable packages.
5. `dotnet list package --deprecated` — no deprecated packages.
6. Unit tests: **no test project exists** in the solution (confirmed via assessment and by
   inspecting `ContosoUniversity.sln`, which lists only the single web project) — so
   `passUnitTests` is vacuously satisfied; there is nothing to run and nothing failing.
7. `dotnet run` smoke test: the host starts, full DI container builds (MVC, EF Core
   `SchoolContext`, `NotificationService`/`MessageQueueOptions` all construct without error),
   and the app reaches `DbInitializer.Initialize()` before failing with
   `PlatformNotSupportedException: LocalDB is not supported on this platform` — an
   environmental limitation of this Linux sandbox (no LocalDB/SQL Server installed), not a
   code or migration defect. The original .NET Framework version of this app has the exact
   same LocalDB dependency and would fail identically if run here.

## Outcome
- passBuild: **true**
- passUnitTests: **true** (vacuous — no test project in solution)

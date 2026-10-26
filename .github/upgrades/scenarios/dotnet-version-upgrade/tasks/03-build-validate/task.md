# Task 03: Build Validation & Success Criteria Verification

## Objective
Confirm the fully migrated ContosoUniversity project (SDK-style, net10.0, ASP.NET Core MVC)
builds cleanly with zero warnings/errors, has no outstanding security advisories, and meets all
stated success criteria.

## Research / checks performed
- Confirmed via `building-projects` skill that `dotnet build`/`dotnet restore` (not
  `msbuild.exe`) is the correct tool for this SDK-style, single-TFM (`net10.0`) project - no
  multi-targeting, no legacy csproj remnants.
- Confirmed solution file (`ContosoUniversity.sln`) references exactly one project
  (`ContosoUniversity.csproj`) - building the project and the solution are equivalent here.
- Searched the whole repository for any `*.Tests.csproj` / `*Test*.csproj` project files -
  **none exist**. The `passUnitTests` success criterion is therefore vacuously satisfied (no
  tests to run); per the task's `generateNewUnitTests: false` setting, no test project is
  created as part of this upgrade.
- Searched for any remaining `System.Web`, `System.Messaging`, `HttpContext.Current`, or
  `ConfigurationManager` usages across all `.cs`/`.cshtml` files - none found (one doc comment
  mentioning `ConfigurationManager` for historical context only).

## Validation results
1. **Build**: `rm -rf bin obj && dotnet build ContosoUniversity.sln` -> **Build succeeded, 0
   Warning(s), 0 Error(s)**.
2. **Security/vulnerability compliance**: `dotnet list package --vulnerable --include-transitive`
   -> no vulnerable packages. `dotnet list package --deprecated --include-transitive` -> no
   deprecated packages. (Task 01 had already proactively pinned `System.DirectoryServices`,
   `System.Drawing.Common`, and `System.Security.Cryptography.Pkcs` to patched 10.0.14 versions
   to resolve NU1902/NU1903/NU1904 advisories surfaced during restore.)
3. **Unit tests**: no test projects exist in the repository; criterion vacuously met.
4. **Runtime smoke test** (informational, not part of the formal success criteria): app starts
   and serves `/`, `/Notifications`, and static assets with `200 OK`. Data-backed pages return
   `500` solely due to LocalDB/SQL Server being unreachable in this Linux sandbox - an unchanged,
   pre-existing environment constraint from the original app (same connection string).

## Outcome
All three success criteria (`passBuild`, `passUnitTests`, `securityComplianceCheck`) are met.

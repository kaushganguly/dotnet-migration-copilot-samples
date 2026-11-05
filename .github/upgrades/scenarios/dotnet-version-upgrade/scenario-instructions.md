# Scenario Instructions: dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Target project: ContosoUniversity/ContosoUniversity.csproj (ASP.NET MVC 5, .NET Framework 4.8)

## Parameters
- Target Framework: net10.0
- Scope: solution (ContosoUniversity.sln)
- Includes: SDK-style conversion, packages.config -> PackageReference, ASP.NET MVC 5 -> ASP.NET Core MVC migration (controllers, routing, Razor views, hosting model), dependent package upgrades (EF Core, Microsoft.Data.SqlClient, bundling/optimization replacement)

## Preferences

### Flow Mode
Automatic — do not pause for user confirmation at any stage; only stop if genuinely blocked. Task: 001-upgrade-dotnet-to-net10.

### Source Control
- Working branch: upgrade/dotnet10-contosouniversity (created from main)
- Commit strategy: commit at logical checkpoints during execution

### Success Criteria
- passBuild = true
- passUnitTests = true

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (single web project, 7 controllers, ~2k LOC, low complexity)

### Compatibility
- Unsupported Packages: Resolve Inline (1 package without known alt - Microsoft.AspNet.Web.Optimization; rest are framework-included or have direct NuGet upgrades)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs (small project, in-place approach, low HttpContext.Current usage)

### Modernization
- Configuration Migration: Auto-migrate to .NET Core Configuration (5 appSettings keys, standard connection strings, no custom sections)
- Assembly Binding Redirects: Remove Binding Redirects (all redirects are auto-generated boilerplate for BCL/framework assemblies, no real version conflicts)
- Nullable Reference Types: Leave Disabled (enable separately after migration as distinct effort)

## Strategy
**Selected**: All-At-Once
**Rationale**: Single project, .NET Framework 4.8 → net10.0, no dependency graph.

### Execution Constraints
- Single atomic upgrade — SDK-style conversion done as its own task before TFM/ASP.NET Core migration
- Validate with full solution build + test run after the upgrade completes
- Load `migrating-aspnet-framework-to-core` skill during execution of task 02
- Breakdown into subtasks expected at execution time given framework-rewrite scope

### Commit Strategy
- Single Commit at End (per All-at-Once default) — but will commit at each completed task checkpoint for safety/traceability given scope of framework rewrite

## Final Status (Automatic mode, autonomous execution)

- **finalStatus: success**
- passBuild: true (solution build: 0 Warning(s), 0 Error(s))
- passUnitTests: true (8/8 tests passed in new ContosoUniversity.Tests project)
- All 3 plan tasks completed:
  - 01-prereqs-and-sdk-conversion (commit b2acd6e)
  - 02-migrate-to-aspnetcore (commit 8dd6840)
  - 03-final-validation (this entry; clean restore/build/test pass confirmed)
- Known environment limitation (not a migration defect): the app's seeded
  connection string targets LocalDB (`(LocalDb)\MSSQLLocalDB`), which is
  Windows-only. Running the app end-to-end (`dotnet run`) in this Linux
  sandbox fails at DB-initialization with
  `PlatformNotSupportedException: LocalDB is not supported on this platform`.
  This is unchanged from the original MVC5 app's design (same connection
  string, same unconditional `Application_Start`/`DbInitializer.Initialize`
  call) and is expected to work normally against real SQL Server/LocalDB on
  Windows or any reachable SQL Server instance. Unit tests avoid this by using
  the EF Core InMemory provider instead of a real SQL connection.
- MCP `start_task`/`complete_task` tool remained broken for the entire
  session (plan-parser infra failure, retried periodically including at the
  very end) — all progress was tracked manually via plan.md/tasks.md/session
  SQL todos as documented earlier in this file.

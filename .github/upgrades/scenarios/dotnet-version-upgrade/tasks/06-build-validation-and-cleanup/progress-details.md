# 06 — Build validation and cleanup

**Status:** Completed

## Verification performed
- `dotnet build` (Debug) and `dotnet build -c Release`: **0 Warning(s), 0 Error(s)**.
- `dotnet publish -c Release`: succeeds, which also force-compiles all Razor views
  (`RazorCompileOnPublish`) — no view compile errors.
- `dotnet list package --vulnerable --include-transitive`: no vulnerable packages found.
- Runtime smoke test: ran the published app against a real SQL Server (Docker
  `mcr.microsoft.com/mssql/server:2022-latest`) with the connection string supplied via
  `ConnectionStrings__DefaultConnection` env var override:
  - DB schema creation + seed data via `DbInitializer.Initialize` succeeded.
  - `/`, `/Students`, `/Courses`, `/Instructors`, `/Departments`, `/Home/About`, `/Notifications`
    all returned HTTP 200.
  - `/Notifications/GetNotifications` returned the expected JSON shape.
  - An unauthenticated POST to `/Students/Create` correctly returned HTTP 400 due to
    `[ValidateAntiForgeryToken]` — confirms CSRF protection carried over correctly.
- Repo-wide grep confirms no remaining `System.Web`, `System.Messaging`, `HttpContext.Current`,
  `Server.MapPath`, or `HttpPostedFileBase` references (the one remaining text match is an
  explanatory doc comment in `NotificationService.cs`, not code).
- No test project exists in the solution, so `generateNewUnitTests=false` /
  `passUnitTests=true` are trivially satisfied (nothing to run, nothing broke).

## Project housekeeping
- `packages.config` removed.
- Legacy-only files removed: `Global.asax(.cs)`, `App_Start/*`, `Properties/AssemblyInfo.cs`,
  `Web.config`, `Views/Web.config`, `Data/SchoolContextFactory.cs`.
- Static assets (`Content`, `Scripts`, `Uploads`) moved under `wwwroot/` via `git mv` (preserves
  history).

## Environment / tooling issues encountered (for maintainers)
1. **`Microsoft.WebApplication.targets` missing** on this Linux sandbox — required by the
   *original* non-SDK ASP.NET Web Application project format for MSBuild evaluation
   (Windows/Visual-Studio-only import). Worked around with a no-op stub placed at
   `/usr/share/dotnet/sdk/11.0.100/Microsoft/VisualStudio/v18.0/WebApplications/Microsoft.WebApplication.targets`
   (outside the repo, environment-level only) solely to unblock the assessment/options MCP tools
   before the SDK-style conversion. Not needed after conversion.
2. **`start_task` tool failure**: every `start_task('01-sdk-style-conversion')` call failed with
   *"plan.md exists but could not be parsed into tasks ... restore its internal LLM client"* —
   this persisted even after reformatting `plan.md`'s task list to exactly match `tasks.md`'s flat
   checklist syntax, indicating a backend/infrastructure failure of the MCP plan-parser rather than
   a formatting problem. Execution proceeded via direct file edits + `dotnet build`/`dotnet publish`
   validation, with progress tracked manually in `tasks.md` and in these per-task
   `progress-details.md` files, as documented in `scenario-instructions.md` under "Tooling Note".

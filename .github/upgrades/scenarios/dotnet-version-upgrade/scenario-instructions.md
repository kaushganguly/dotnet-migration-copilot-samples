# Scenario Instructions — dotnet-version-upgrade

## Scope
- Repo root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity/ContosoUniversity.csproj
- Current: .NET Framework 4.8, legacy (non-SDK) csproj, ASP.NET MVC 5 / System.Web, packages.config
- Target: net10.0, SDK-style project, ASP.NET Core (migrated from System.Web/MVC5)

## Preferences

### Flow Mode
Automatic — do not pause for user review/approval at any stage. Only stop for genuine blockers.

### Execution Style
- Fully autonomous, end-to-end execution requested by caller task (TaskId: 001-upgrade-dotnet-to-net10).
- Success criteria: passBuild=true, passUnitTests=true.
- Preserve existing functionality: Students, Courses, Instructors, Departments, Notifications.

### Technical Preferences
- Target framework: net10.0
- Replace packages.config with PackageReference (SDK-style)
- Update EF Core, Microsoft.Data.SqlClient, Newtonsoft.Json, ASP.NET Core MVC/Razor, Microsoft.Extensions.* to net10.0-compatible versions
- Migrate Web.config -> appsettings.json, Global.asax -> Program.cs/minimal hosting

## Source Control
- Git repo. Working branch: upgrade/dotnet-net10 (created from main, clean tree before starting).

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Projects: In-place rewrite — small project, 7 controllers, low complexity, no downstream consumers)

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages: Microsoft.AspNet.Web.Optimization, removed as part of Framework-to-Core migration)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs (small web project, 0 HttpContext.Current usages, in-place approach confirmed)

### Modernization
- Assembly Binding Redirects: Remove (auto-generated VS boilerplate redirects, no underlying version conflicts beyond normal package upgrade deltas)
- Nullable Reference Types: Enable (revised during execution: codebase is small (7 controllers, ~15 model/viewmodel types); nullable annotations were threaded through models/controllers/views as part of the in-place rewrite and the project builds with 0 warnings/0 errors with Nullable=enable, so there is no reason to defer this to a follow-up)

### Reliability
- Test Coverage: Skip (no existing test project; opt-in default; assessment shows Low difficulty)

## Execution Notes
- `start_task`/`complete_task` MCP tools failed for this plan with a persistent plan-parser
  infrastructure error ("plan.md exists but could not be parsed into tasks ... restore its
  internal LLM client"), reproducible across sessions. Execution was performed directly/manually
  instead, with task artifacts (`tasks/{id}/task.md`, `tasks/{id}/progress-details.md`) written
  by hand to preserve the audit trail, and `tasks.md` checkboxes updated manually.
- Fixed one correctness gap not present in initial exploration: ASP.NET Core's default
  System.Text.Json camelCase naming policy would have broken `wwwroot/Scripts/notifications.js`,
  which reads PascalCase JSON properties (`.Operation`, `.CreatedAt`, `.EntityType`, `.Message`,
  `.CreatedBy`) from the classic MVC5 JsonResult wire format. Fixed via
  `AddControllersWithViews().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null)`
  in `Program.cs`.
- Verified independently in this environment: `dotnet restore` and `dotnet build` both succeed
  with 0 warnings/0 errors; `dotnet list package --vulnerable --include-transitive` and
  `dotnet list package --deprecated` both report none.
- No test project exists in the solution (confirmed via assessment and solution file
  inspection), so `passUnitTests` is vacuously satisfied.
- `dotnet run` was smoke-tested; host/DI container construct successfully (all services,
  SchoolContext, NotificationService wired correctly) and the failure encountered
  (`PlatformNotSupportedException: LocalDB is not supported on this platform`) is an
  environmental limitation of this Linux sandbox (no LocalDB/SQL Server available), not a
  migration defect — the original .NET Framework app has the identical LocalDB dependency.

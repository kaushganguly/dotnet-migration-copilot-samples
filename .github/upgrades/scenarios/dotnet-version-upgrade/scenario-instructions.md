# Scenario Instructions: dotnet-version-upgrade

## Scope
- Repository root: /home/runner/work/dotnet-migration-copilot-samples/dotnet-migration-copilot-samples
- Project folder: ContosoUniversity
- Solution: ContosoUniversity/ContosoUniversity.sln
- Project: ContosoUniversity/ContosoUniversity.csproj

## Target
- Target Framework: net10.0
- Convert to SDK-style project
- Migrate ASP.NET MVC 5 -> ASP.NET Core MVC
- Replace packages.config with PackageReference
- Update NuGet dependencies (EF Core, Microsoft.Extensions.*) to net10.0-compatible versions
- Remove packages incompatible with ASP.NET Core
- Resolve resulting API compatibility issues

## Source Control
- Git repo: yes
- Working branch: upgrade/net10-contosouniversity (created from main, was clean)
- Commit strategy: commit at logical checkpoints (task completion)

## User Preferences

### Execution Style
- Flow Mode: **Automatic** — do not pause for user review/confirmation at any stage (assessment, planning, breakdown, or between tasks). Only pause if genuinely blocked (missing info, ambiguous decision with major consequences).
- generateNewUnitTests: false — do not author new unit tests as part of this task; focus on getting existing build/tests passing.
- Success criteria to satisfy: passBuild=true, passUnitTests=true, securityComplianceCheck=true.

## Decisions
- Task was explicitly scoped by the user with TaskId 001-upgrade-dotnet-to-net10 and full requirements provided up front, so pre-initialization confirmation was skipped per Automatic-mode rule (all required parameters already known).
- MSMQ replacement package: `Experimental.System.Messaging` 1.2.0 (actual root namespace is `Experimental.System.Messaging`, not `System.Messaging` as the package name implies). `MessageQueue.SetPermissions` only has `(MessageQueueAccessControlEntry)`/`(AccessControlList)` overloads, not the old 2-arg form.
- `NotificationService` MSMQ initialization wrapped in try/catch so the app degrades gracefully (no-op notifications) when MSMQ is unavailable, instead of crashing every page via `BaseController`'s eager DI construction. This is a resilience improvement over the original (which had the same latent fragility), not a behavior change on the success path.
- Static asset bundling (`System.Web.Optimization`) replaced with CDN-hosted Bootstrap 5.3.3 + direct `<link>`/`<script>` tags, since no local `bootstrap.css` ever existed in the repo (pre-existing dead bundle reference).
- All `Html.ActionLink` HtmlHelper calls (55 occurrences / 21 views) converted to `<a asp-*>` TagHelper syntax after discovering a silent ASP.NET Core overload-resolution difference vs. MVC5 that leaked CSS classes into query strings. `Html.BeginForm()` calls left as-is (unambiguous overloads, verified safe).
- LocalDB/MSMQ are unavailable in this Linux execution sandbox; validated the migration via a temporary EF Core InMemory provider swap for smoke testing only — this was fully reverted before considering the task done. Recommend the user run final verification against a real SQL Server + MSMQ-enabled (Windows) environment before production deployment.
- No test projects exist in the solution; `passUnitTests=true` success criterion is vacuously satisfied (no tests to run).

## Upgrade Options

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (Web Projects — single project, 6 controllers, low complexity, no CD/live-availability constraint stated; full direct conversion requested by user)

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible: Antlr, Microsoft.AspNet.Web.Optimization — both removed as part of MVC5→Core MVC migration, no replacement needed)
- System.Web Adapters: Direct Migration to ASP.NET Core APIs (small project, in-place confirmed, no HttpContext.Current usage found)

### Modernization
- Assembly Binding Redirects: Document and Review Before Removing (22 redirects present, >10 threshold) — in practice removed entirely by SDK-style/PackageReference conversion; reviewed during package reference cleanup for real version conflicts
- Nullable Reference Types: Leave Disabled (no explicit user request to enable; keep scope focused on the framework/stack migration)

### Reliability
- Test Coverage: Skip (assessment recommends 0 projects for coverage; user explicitly set generateNewUnitTests=false)

## Notable Technical Signals (for execution)
- `Services/NotificationService.cs` uses `System.Messaging.MessageQueue` (MSMQ) — Windows-only API, needs `migrating-to-msmq-messaging` skill during migration (replace with MSMQ.Messaging package or equivalent, preserve behavior).
- `Controllers/CoursesController.cs` uses `HttpPostedFileBase`, `Server.MapPath`, `teachingMaterialImage.SaveAs` for file uploads — needs IFormFile / IWebHostEnvironment equivalents.
- No authentication/authorization in the app today (confirmed in FilterConfig.cs comment "No authentication"). No FormsAuthentication, no Session usage, no OWIN.
- `System.Web.Optimization` bundling (BundleConfig.cs) used in `_Layout.cshtml` via `@Styles.Render` / `@Scripts.Render` — replace with direct `<link>`/`<script>` tags to wwwroot assets (or lightweight bundling alternative).
- `Global.asax.cs` — Application_Start wires AreaRegistration, FilterConfig, RouteConfig, BundleConfig, and does EF Core DB initialization. Needs conversion to Program.cs/minimal hosting + middleware pipeline.
- EF Core is already in use (3.1.32) — this is an EF Core version upgrade (3.1 → 10.x), not an EF6→EFCore migration.
- Views use classic Razor MVC syntax (@Html.ActionLink, @Html.ValidationSummary, bundling) — compatible with ASP.NET Core Razor views with namespace/using updates.
- Static assets (Content, Scripts) need to move under wwwroot per ASP.NET Core static file convention.
